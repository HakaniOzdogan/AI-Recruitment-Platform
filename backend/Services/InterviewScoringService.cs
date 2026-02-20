using System.Text.Json;
using System.Globalization;
using System.Text;
using IkOtomasyon.Api.Contracts;
using IkOtomasyon.Api.Data;
using IkOtomasyon.Api.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IkOtomasyon.Api.Services;

public class InterviewScoringService
{
    private const string InsufficientEvidenceReason = "Insufficient evidence in transcript";

    private static readonly Dictionary<string, string[]> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        [RubricKeys.Technical] = ["c#", "dotnet", ".net", "java", "sql", "api", "microservice", "docker", "kubernetes", "react", "node", "architecture", "cache", "redis", "ci/cd"],
        [RubricKeys.ProblemSolving] = ["\u00E7\u00F6z\u00FCm", "analiz", "trade-off", "optimize", "optimizasyon", "root cause", "problem", "approach", "karar", "iyile\u015Ftir", "metric", "\u00F6l\u00E7\u00FCm"],
        [RubricKeys.Communication] = ["\u00F6nce", "sonra", "ad\u0131m", "net", "a\u00E7\u0131k", "anlat", "summary", "ileti\u015Fim", "stakeholder", "dok\u00FCmante"],
        [RubricKeys.CultureFit] = ["tak\u0131m", "ownership", "sorumluluk", "geri bildirim", "i\u015F birli\u011Fi", "de\u011Fer", "mentor", "yard\u0131m", "initiative"],
        [RubricKeys.DomainKnowledge] = ["domain", "\u00FCr\u00FCn", "m\u00FC\u015Fteri", "i\u015F", "kpi", "compliance", "fintech", "e-commerce", "saas", "hr", "recruitment"]
    };

    private readonly AppDbContext _db;
    private readonly RubricService _rubricService;
    private readonly InterviewScoringEnrichmentService _enrichmentService;
    private readonly LlmOptions _llmOptions;
    private readonly ScoringOptions _options;
    private readonly IMemoryCache _cache;

    public InterviewScoringService(
        AppDbContext db,
        RubricService rubricService,
        InterviewScoringEnrichmentService enrichmentService,
        IOptions<ScoringOptions> options,
        IOptions<LlmOptions> llmOptions,
        IMemoryCache cache)
    {
        _db = db;
        _rubricService = rubricService;
        _enrichmentService = enrichmentService;
        _options = options.Value;
        _llmOptions = llmOptions.Value;
        _cache = cache;
    }

    public async Task<InterviewScoreResponse> AutoScoreAsync(Guid sessionId, Guid actorUserId, bool force, CancellationToken ct = default)
    {
        var session = await _db.InterviewSessions
            .Include(x => x.Application)
            .ThenInclude(x => x!.Job)
            .FirstOrDefaultAsync(x => x.Id == sessionId, ct)
            ?? throw new NotFoundException("Interview session not found.");

        if (session.Status != InterviewSessionStatus.Completed)
        {
            if (!force)
            {
                throw new ConflictException("Interview session must be Completed.");
            }

            var candidateMessageCount = await _db.InterviewMessages
                .CountAsync(x => x.SessionId == sessionId && x.Role == "candidate", ct);
            if (candidateMessageCount == 0)
            {
                throw new ConflictException("Interview session has no candidate responses yet.");
            }

            session.Status = InterviewSessionStatus.Completed;
            session.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        if (!force && _cache.TryGetValue<InterviewScoreResponse>(GetSessionCacheKey(sessionId), out var cachedResponse))
        {
            return cachedResponse!;
        }

        var jobId = session.Application?.JobId ?? throw new ConflictException("Session application is missing.");
        var rubric = await _rubricService.GetJobRubricOrDefaultAsync(jobId, ct);

        var scorecard = await GetOrCreateScorecardAsync(sessionId, rubric.Id, ct);
        var existingAi = await _db.InterviewCriterionScores
            .Where(x => x.ScorecardId == scorecard.Id && x.EvaluatorType == "AI")
            .AnyAsync(ct);

        if (existingAi && !force)
        {
            var cached = await BuildScoreResponseAsync(scorecard.Id, "cached", Array.Empty<string>(), ct);
            _cache.Set(GetSessionCacheKey(sessionId), cached, TimeSpan.FromMinutes(10));
            return cached;
        }

        var messages = await _db.InterviewMessages
            .AsNoTracking()
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);

        var candidateMessages = messages
            .Where(x => x.Role.Equals("candidate", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Content)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        if (candidateMessages.Count == 0)
        {
            candidateMessages = messages.Select(x => x.Content).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        }

        var latestByCriterion = await GetLatestByCriterionAsync(scorecard.Id, ct);
        var criteria = rubric.Criteria.OrderBy(x => x.Order).ToList();
        var autoScores = new List<InterviewCriterionScore>();
        var enrichmentScores = new List<InterviewCriterionScore>();
        var enrichmentEvents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var llmEnrichment = "deterministic";

        foreach (var criterion in criteria)
        {
            var evidence = ExtractEvidence(candidateMessages, criterion.Key, session.Application?.Job);

            var score = new InterviewCriterionScore
            {
                Id = Guid.NewGuid(),
                ScorecardId = scorecard.Id,
                CriterionKey = criterion.Key,
                EvaluatorType = "AI",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = actorUserId,
                ReplacesScoreId = latestByCriterion.TryGetValue(criterion.Key, out var previous) ? previous.Id : null,
                EnrichmentStatus = InterviewScoreEnrichmentStatus.None
            };

            if (evidence.Count == 0)
            {
                score.Status = InterviewCriterionScoreStatus.InsufficientEvidence;
                score.Score = null;
                score.Rationale = InsufficientEvidenceReason;
                score.EvidenceQuotesJson = "[]";
            }
            else
            {
                var scoreTuple = CalculateScore(evidence);
                score.Status = InterviewCriterionScoreStatus.Scored;
                score.Score = scoreTuple.Score;
                score.Rationale = scoreTuple.Rationale;
                score.EvidenceQuotesJson = JsonSerializer.Serialize(evidence);
            }

            autoScores.Add(score);
        }

        if (_options.Mode.Equals("llm_assisted", StringComparison.OrdinalIgnoreCase))
        {
            if (!_llmOptions.Enabled || !_options.LlmScoringEnabled)
            {
                llmEnrichment = "skipped_disabled";
                enrichmentEvents.Add(AuditActions.LlmScoreEnrichSkip);

                foreach (var score in autoScores.Where(x => x.Status == InterviewCriterionScoreStatus.Scored))
                {
                    score.EnrichmentStatus = InterviewScoreEnrichmentStatus.Skipped;
                    score.EnrichmentErrorsJson = JsonSerializer.Serialize(new[] { "LLM scoring enrichment is disabled." });
                }
            }
            else
            {
                llmEnrichment = "failed";
                foreach (var score in autoScores.Where(x => x.Status == InterviewCriterionScoreStatus.Scored))
                {
                    var deterministicEvidence = ParseEvidence(score.EvidenceQuotesJson);
                    var request = new ScoreEnrichmentRequest(
                        score.CriterionKey,
                        BuildJobSummary(session.Application?.Job),
                        candidateMessages.Take(8).ToList(),
                        deterministicEvidence,
                        score.Score ?? 0,
                        score.Rationale);

                    var result = await _enrichmentService.EnrichAsync(request, ct);
                    if (result.IsEnriched)
                    {
                        var mergedEvidence = deterministicEvidence
                            .Concat(result.AdditionalEvidence)
                            .DistinctBy(x => $"{x.RelatedTo}|{x.Quote}", StringComparer.OrdinalIgnoreCase)
                            .Take(3)
                            .ToList();

                        enrichmentScores.Add(new InterviewCriterionScore
                        {
                            Id = Guid.NewGuid(),
                            ScorecardId = scorecard.Id,
                            CriterionKey = score.CriterionKey,
                            Status = InterviewCriterionScoreStatus.Scored,
                            Score = result.SuggestedScore,
                            Rationale = result.Rationale,
                            EvidenceQuotesJson = JsonSerializer.Serialize(mergedEvidence),
                            EvaluatorType = "AI",
                            EnrichmentStatus = InterviewScoreEnrichmentStatus.Enriched,
                            EnrichedByModel = result.ModelName,
                            CreatedAt = DateTime.UtcNow,
                            CreatedByUserId = actorUserId,
                            ReplacesScoreId = score.Id
                        });

                        enrichmentEvents.Add(AuditActions.LlmScoreEnriched);
                        llmEnrichment = "enriched";
                    }
                    else if (result.GuardrailBlocked)
                    {
                        score.EnrichmentStatus = InterviewScoreEnrichmentStatus.Failed;
                        score.EnrichmentErrorsJson = JsonSerializer.Serialize(result.Errors);
                        score.EnrichedByModel = _llmOptions.Model;
                        enrichmentEvents.Add(AuditActions.LlmScoreEnrichGuardrailBlock);
                        llmEnrichment = llmEnrichment == "enriched" ? "partial_enriched" : "failed";
                    }
                    else
                    {
                        score.EnrichmentStatus = InterviewScoreEnrichmentStatus.Failed;
                        score.EnrichmentErrorsJson = JsonSerializer.Serialize(result.Errors);
                        score.EnrichedByModel = _llmOptions.Model;
                        enrichmentEvents.Add(AuditActions.LlmScoreEnrichFail);
                        llmEnrichment = llmEnrichment == "enriched" ? "partial_enriched" : "failed";
                    }
                }
            }
        }

        _db.InterviewCriterionScores.AddRange(autoScores);
        if (enrichmentScores.Count > 0)
        {
            _db.InterviewCriterionScores.AddRange(enrichmentScores);
        }

        await _db.SaveChangesAsync(ct);

        scorecard.OverallScore = await RecalculateOverallScoreAsync(scorecard.Id, ct);
        await _db.SaveChangesAsync(ct);

        var response = await BuildScoreResponseAsync(scorecard.Id, llmEnrichment, enrichmentEvents, ct);
        _cache.Set(GetSessionCacheKey(sessionId), response, TimeSpan.FromMinutes(10));
        return response;
    }

    public async Task<InterviewScoreResponse> AddHumanOverrideAsync(Guid sessionId, Guid actorUserId, HumanScoreOverrideRequest request, CancellationToken ct = default)
    {
        var session = await _db.InterviewSessions
            .Include(x => x.Application)
            .FirstOrDefaultAsync(x => x.Id == sessionId, ct)
            ?? throw new NotFoundException("Interview session not found.");

        var jobId = session.Application?.JobId ?? throw new ConflictException("Session application is missing.");
        var rubric = await _rubricService.GetJobRubricOrDefaultAsync(jobId, ct);

        var scorecard = await GetOrCreateScorecardAsync(session.Id, rubric.Id, ct);
        var key = request.CriterionKey.Trim().ToLowerInvariant();
        if (!rubric.Criteria.Any(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ConflictException("Criterion is not part of active rubric.");
        }

        var latest = await _db.InterviewCriterionScores
            .Where(x => x.ScorecardId == scorecard.Id && x.CriterionKey == key)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var evidence = request.EvidenceQuotes
            .Select(x => new EvidenceQuoteResponse(SanitizeQuote(x.Quote), SanitizeRelatedTo(x.RelatedTo)))
            .Where(x => !string.IsNullOrWhiteSpace(x.Quote))
            .ToList();

        if (evidence.Count == 0)
        {
            throw new RequestValidationException("evidenceQuotes", "At least one non-empty evidence quote is required.");
        }

        var entity = new InterviewCriterionScore
        {
            Id = Guid.NewGuid(),
            ScorecardId = scorecard.Id,
            CriterionKey = key,
            Status = InterviewCriterionScoreStatus.Scored,
            Score = Math.Clamp(request.Score, 0, 5),
            Rationale = request.Rationale.Trim(),
            EvidenceQuotesJson = JsonSerializer.Serialize(evidence),
            EvaluatorType = "HUMAN",
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = actorUserId,
            ReplacesScoreId = latest?.Id
        };

        _db.InterviewCriterionScores.Add(entity);
        await _db.SaveChangesAsync(ct);

        scorecard.OverallScore = await RecalculateOverallScoreAsync(scorecard.Id, ct);
        await _db.SaveChangesAsync(ct);

        var response = await BuildScoreResponseAsync(scorecard.Id, "not_applicable", Array.Empty<string>(), ct);
        _cache.Set(GetSessionCacheKey(sessionId), response, TimeSpan.FromMinutes(10));
        return response;
    }

    public async Task<InterviewScoreResponse> GetScoreAsync(Guid sessionId, CancellationToken ct = default)
    {
        var scorecard = await _db.InterviewScorecards
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(x => x.SessionId == sessionId, ct)
            ?? throw new NotFoundException("Interview scorecard not found.");

        if (_cache.TryGetValue<InterviewScoreResponse>(GetSessionCacheKey(sessionId), out var cached) && cached is not null)
        {
            return cached;
        }

        var response = await BuildScoreResponseAsync(scorecard.Id, "not_applicable", Array.Empty<string>(), ct);
        _cache.Set(GetSessionCacheKey(sessionId), response, TimeSpan.FromMinutes(10));
        return response;
    }

    private async Task<InterviewScorecard> GetOrCreateScorecardAsync(Guid sessionId, Guid rubricTemplateId, CancellationToken ct)
    {
        var scorecard = await _db.InterviewScorecards
            .FirstOrDefaultAsync(x => x.SessionId == sessionId && x.RubricTemplateId == rubricTemplateId, ct);

        if (scorecard is not null)
        {
            return scorecard;
        }

        scorecard = new InterviewScorecard
        {
            Id = Guid.NewGuid(),
            TenantId = await _db.InterviewSessions
                .AsNoTracking()
                .Where(x => x.Id == sessionId)
                .Select(x => x.TenantId)
                .FirstOrDefaultAsync(ct),
            SessionId = sessionId,
            RubricTemplateId = rubricTemplateId,
            CreatedAt = DateTime.UtcNow
        };
        _db.InterviewScorecards.Add(scorecard);
        await _db.SaveChangesAsync(ct);
        return scorecard;
    }

    private async Task<Dictionary<string, InterviewCriterionScore>> GetLatestByCriterionAsync(Guid scorecardId, CancellationToken ct)
    {
        var all = await _db.InterviewCriterionScores
            .Where(x => x.ScorecardId == scorecardId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        return all.GroupBy(x => x.CriterionKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
    }

    private async Task<double?> RecalculateOverallScoreAsync(Guid scorecardId, CancellationToken ct)
    {
        var scorecard = await _db.InterviewScorecards
            .Include(x => x.RubricTemplate)
            .ThenInclude(x => x!.Criteria)
            .FirstAsync(x => x.Id == scorecardId, ct);

        var latest = await GetLatestByCriterionAsync(scorecardId, ct);
        var weightedSum = 0.0;
        var effectiveWeight = 0.0;

        foreach (var criterion in scorecard.RubricTemplate!.Criteria)
        {
            if (!latest.TryGetValue(criterion.Key, out var item))
            {
                continue;
            }

            if (!item.Status.Equals(InterviewCriterionScoreStatus.Scored, StringComparison.OrdinalIgnoreCase) || item.Score is null)
            {
                continue;
            }

            weightedSum += item.Score.Value * criterion.Weight;
            effectiveWeight += criterion.Weight;
        }

        if (effectiveWeight <= 0)
        {
            return null;
        }

        return Math.Round(weightedSum / effectiveWeight, 2, MidpointRounding.AwayFromZero);
    }

    private async Task<InterviewScoreResponse> BuildScoreResponseAsync(
        Guid scorecardId,
        string llmEnrichment,
        IEnumerable<string> llmEnrichmentEvents,
        CancellationToken ct)
    {
        var scorecard = await _db.InterviewScorecards
            .AsNoTracking()
            .FirstAsync(x => x.Id == scorecardId, ct);

        var scores = await _db.InterviewCriterionScores
            .AsNoTracking()
            .Where(x => x.ScorecardId == scorecardId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        var latest = scores
            .GroupBy(x => x.CriterionKey, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(x => x.CriterionKey, StringComparer.OrdinalIgnoreCase)
            .Select(ToLatestResponse)
            .ToList();

        var insufficientAll = latest.Count > 0 && latest.All(x => !x.Status.Equals(InterviewCriterionScoreStatus.Scored, StringComparison.OrdinalIgnoreCase));

        var history = scores
            .OrderByDescending(x => x.CreatedAt)
            .Select(ToHistoryResponse)
            .ToList();

        return new InterviewScoreResponse(
            scorecard.Id,
            scorecard.SessionId,
            scorecard.RubricTemplateId,
            scorecard.OverallScore,
            insufficientAll,
            llmEnrichment,
            llmEnrichmentEvents.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            scorecard.CreatedAt,
            latest,
            history);
    }

    private static InterviewCriterionLatestResponse ToLatestResponse(InterviewCriterionScore entity)
    {
        return new InterviewCriterionLatestResponse(
            entity.CriterionKey,
            entity.Status,
            entity.Score,
            entity.Rationale,
            ParseEvidence(entity.EvidenceQuotesJson),
            entity.EvaluatorType,
            entity.EnrichmentStatus,
            entity.EnrichedByModel,
            ParseErrors(entity.EnrichmentErrorsJson),
            entity.CreatedAt,
            entity.CreatedByUserId);
    }

    private static InterviewCriterionHistoryResponse ToHistoryResponse(InterviewCriterionScore entity)
    {
        return new InterviewCriterionHistoryResponse(
            entity.Id,
            entity.CriterionKey,
            entity.Status,
            entity.Score,
            entity.Rationale,
            ParseEvidence(entity.EvidenceQuotesJson),
            entity.EvaluatorType,
            entity.EnrichmentStatus,
            entity.EnrichedByModel,
            ParseErrors(entity.EnrichmentErrorsJson),
            entity.CreatedAt,
            entity.CreatedByUserId,
            entity.ReplacesScoreId);
    }

    private static List<EvidenceQuoteResponse> ExtractEvidence(IReadOnlyCollection<string> messages, string criterionKey, JobPosting? job)
    {
        var keywords = BuildKeywords(criterionKey, job)
            .Select(NormalizeForMatch)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var hits = new List<(int Score, string Quote)>();

        foreach (var message in messages)
        {
            var content = message.Trim();
            if (content.Length == 0)
            {
                continue;
            }

            var normalizedContent = NormalizeForMatch(content);
            var score = 0;
            foreach (var keyword in keywords)
            {
                if (normalizedContent.Contains(keyword, StringComparison.Ordinal))
                {
                    score++;
                }
            }

            if (score > 0)
            {
                hits.Add((score, SanitizeQuote(content)));
            }
        }

        return hits
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Quote.Length)
            .Take(3)
            .Select(x => new EvidenceQuoteResponse(x.Quote, criterionKey))
            .ToList();
    }

    private static string NormalizeForMatch(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var lower = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(lower.Length);

        foreach (var ch in lower)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            sb.Append(ch switch
            {
                '\u00E7' => 'c',
                '\u011F' => 'g',
                '\u0131' => 'i',
                '\u00F6' => 'o',
                '\u015F' => 's',
                '\u00FC' => 'u',
                _ => ch
            });
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private static (double Score, string Rationale) CalculateScore(IReadOnlyCollection<EvidenceQuoteResponse> evidence)
    {
        var longest = evidence.Max(x => x.Quote.Length);
        var score = evidence.Count switch
        {
            1 when longest < 90 => 1,
            1 => 2,
            2 => 3,
            3 when longest < 140 => 4,
            _ => 5
        };

        var rationale = score switch
        {
            <= 1 => "General statements with limited specificity.",
            2 => "Some relevant detail exists but depth is limited.",
            3 => "Moderate evidence with a concrete example.",
            4 => "Strong evidence with clear examples and context.",
            _ => "Rich, specific evidence including measurable outcomes."
        };

        return (score, rationale);
    }

    private static string BuildJobSummary(JobPosting? job)
    {
        if (job is null)
        {
            return "Job context is missing.";
        }

        var required = ParseSkills(job.RequiredSkillsJson).Take(12).ToList();
        var nice = ParseSkills(job.NiceToHaveSkillsJson).Take(12).ToList();
        return $"title={job.Title}; required=[{string.Join(",", required)}]; nice=[{string.Join(",", nice)}]; min_exp={job.MinExperienceMonths?.ToString() ?? "n/a"}";
    }

    private static IEnumerable<string> BuildKeywords(string criterionKey, JobPosting? job)
    {
        var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (Keywords.TryGetValue(criterionKey, out var predefined))
        {
            foreach (var item in predefined)
            {
                values.Add(item);
            }
        }

        if (criterionKey.Equals(RubricKeys.DomainKnowledge, StringComparison.OrdinalIgnoreCase) && job is not null)
        {
            foreach (var skill in ParseSkills(job.RequiredSkillsJson))
            {
                values.Add(skill);
            }
        }

        return values;
    }

    private static IEnumerable<string> ParseSkills(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static List<EvidenceQuoteResponse> ParseEvidence(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<EvidenceQuoteResponse>>(json, JsonOptions)
                ?.Where(x => !string.IsNullOrWhiteSpace(x.Quote))
                .Select(x => new EvidenceQuoteResponse(SanitizeQuote(x.Quote), SanitizeRelatedTo(x.RelatedTo)))
                .ToList()
                ?? new List<EvidenceQuoteResponse>();
        }
        catch
        {
            return new List<EvidenceQuoteResponse>();
        }
    }

    private static IReadOnlyCollection<string> ParseErrors(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<string>();
        }

        try
        {
            var items = JsonSerializer.Deserialize<List<string>>(json, JsonOptions)
                ?.Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList()
                ?? new List<string>();
            return items;
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static string SanitizeQuote(string quote)
    {
        var clean = quote.Replace('\n', ' ').Replace('\r', ' ').Trim();
        if (clean.Length > 200)
        {
            clean = clean[..200];
        }

        return clean;
    }

    private static string SanitizeRelatedTo(string? relatedTo)
    {
        var value = string.IsNullOrWhiteSpace(relatedTo) ? "general" : relatedTo.Trim();
        return value.Length > 120 ? value[..120] : value;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static string GetSessionCacheKey(Guid sessionId) => $"interview-score:{sessionId:N}";
}
