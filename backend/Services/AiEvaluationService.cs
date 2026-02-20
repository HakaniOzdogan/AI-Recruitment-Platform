using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IkOtomasyon.Api.Data;
using IkOtomasyon.Api.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;

namespace IkOtomasyon.Api.Services;

public class AiEvaluationService
{
    private static readonly HashSet<string> AllowedRecommendations = new(StringComparer.OrdinalIgnoreCase)
    {
        "shortlist", "hold", "reject"
    };

    private static readonly string[] SensitiveKeywords =
    [
        "health", "religion", "race", "ethnicity", "politics", "political", "sexual orientation", "disability",
        "sağlık", "din", "ırk", "siyaset", "engellilik"
    ];

    private readonly AppDbContext _db;
    private readonly ILLMClient _llmClient;
    private readonly MatchService _matchService;
    private readonly LlmOptions _llmOptions;
    private readonly IMemoryCache _cache;

    public AiEvaluationService(
        AppDbContext db,
        ILLMClient llmClient,
        MatchService matchService,
        Microsoft.Extensions.Options.IOptions<LlmOptions> llmOptions,
        IMemoryCache cache)
    {
        _db = db;
        _llmClient = llmClient;
        _matchService = matchService;
        _llmOptions = llmOptions.Value;
        _cache = cache;
    }

    public async Task<AiEvaluationReport> EvaluateAsync(Guid jobId, Guid candidateId, Guid actorUserId, bool force, CancellationToken ct = default)
    {
        var job = await _db.JobPostings.FirstOrDefaultAsync(x => x.Id == jobId, ct)
            ?? throw new NotFoundException("Job not found.");

        _ = await _db.Candidates.FirstOrDefaultAsync(x => x.Id == candidateId, ct)
            ?? throw new NotFoundException("Candidate not found.");

        var consent = await _db.CandidateConsents.FirstOrDefaultAsync(x => x.CandidateId == candidateId, ct);
        if (consent is null || !consent.ConsentGiven)
        {
            throw new ConflictException("Consent required");
        }

        var profile = await _db.CandidateProfiles.FirstOrDefaultAsync(x => x.CandidateId == candidateId, ct)
            ?? throw new ConflictException("Profile missing (CV parse required)");

        var latest = await _db.AiEvaluationReports
            .Where(x => x.JobId == jobId && x.CandidateId == candidateId)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(ct);

        var appId = await _db.Applications
            .Where(x => x.JobId == jobId && x.CandidateId == candidateId)
            .OrderByDescending(x => x.AppliedAt)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct);

        var requiredSkills = ParseStringArray(job.RequiredSkillsJson).Select(SkillNormalization.Normalize).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var niceSkills = ParseStringArray(job.NiceToHaveSkillsJson).Select(SkillNormalization.Normalize).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var profileSkills = ParseStringArray(profile.SkillsJson).Select(SkillNormalization.Normalize).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var competencyWeights = ParseWeights(job.CompetencyWeightsJson);
        var skillWeights = await _db.JobSkillWeights
            .AsNoTracking()
            .Where(x => x.JobId == jobId)
            .OrderByDescending(x => x.IsRequired)
            .ThenByDescending(x => x.Weight)
            .ToListAsync(ct);

        var matching = await _matchService.GetOrComputeMatchAsync(jobId, candidateId, actorUserId, force: false, ct);
        var snapshotCore = new
        {
            job = new
            {
                job.Id,
                job.Title,
                requiredSkills,
                niceSkills,
                job.MinExperienceMonths,
                competencyWeights,
                skillWeights = skillWeights.Select(x => new { x.SkillNameNormalized, x.Weight, x.IsRequired })
            },
            profile = new
            {
                profile.CandidateId,
                profile.FullNameSnapshot,
                profile.EmailSnapshot,
                profile.Summary,
                profileSkills,
                profile.TotalExperienceMonths,
                links = ParseStringArray(profile.LinksJson)
            },
            matching = new
            {
                matching.Score,
                reasons = MatchService.ParseReasonJson(matching.ReasonsJson),
                gaps = MatchService.ParseGapJson(matching.GapsJson)
            }
        };

        var snapshotRaw = JsonSerializer.Serialize(snapshotCore);
        var snapshotHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshotRaw)));
        var snapshotJson = JsonSerializer.Serialize(new { hash = snapshotHash, data = snapshotCore });
        var cacheKey = GetCacheKey(jobId, candidateId);

        if (!force)
        {
            if (_cache.TryGetValue<AiEvaluationReport>(cacheKey, out var cached)
                && cached is not null
                && cached.InputSnapshotHash.Equals(snapshotHash, StringComparison.OrdinalIgnoreCase))
            {
                return cached;
            }

            if (latest is not null && latest.InputSnapshotHash.Equals(snapshotHash, StringComparison.OrdinalIgnoreCase))
            {
                _cache.Set(cacheKey, latest, TimeSpan.FromMinutes(10));
                return latest;
            }
        }

        var systemPrompt = BuildSystemPrompt();
        var userPrompt = JsonSerializer.Serialize(new
        {
            job = new
            {
                job.Id,
                job.Title,
                requiredSkills,
                niceSkills,
                job.MinExperienceMonths
            },
            competencyWeights,
            skillWeights = skillWeights.Select(x => new
            {
                name = x.SkillNameNormalized,
                x.Weight,
                x.IsRequired
            }),
            profile = new
            {
                profile.Summary,
                profile.TotalExperienceMonths,
                skills = profileSkills,
                links = ParseStringArray(profile.LinksJson)
            },
            matching = new
            {
                matching.Score,
                reasons = MatchService.ParseReasonJson(matching.ReasonsJson),
                gaps = MatchService.ParseGapJson(matching.GapsJson)
            }
        });

        var rawJson = await _llmClient.GenerateStructuredAsync(systemPrompt, userPrompt, BuildSchema(), ct);
        var output = ParseAndSanitizeOutput(rawJson, skillWeights);

        var report = new AiEvaluationReport
        {
            Id = Guid.NewGuid(),
            TenantId = job.TenantId,
            JobId = jobId,
            CandidateId = candidateId,
            ApplicationId = appId,
            ModelName = _llmOptions.Model,
            InputSnapshotJson = snapshotJson,
            InputSnapshotHash = snapshotHash,
            OverallRecommendation = output.OverallRecommendation,
            StrengthsJson = JsonSerializer.Serialize(output.Strengths),
            RisksJson = JsonSerializer.Serialize(output.Risks),
            VerificationQuestionsJson = JsonSerializer.Serialize(output.VerificationQuestions),
            EvidenceQuotesJson = JsonSerializer.Serialize(output.EvidenceQuotes),
            CompetencyAssessmentJson = JsonSerializer.Serialize(output.CompetencyAssessment),
            SkillAssessmentJson = JsonSerializer.Serialize(output.SkillAssessment),
            Confidence = output.Confidence,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = actorUserId,
            Version = (latest?.Version ?? 0) + 1
        };

        _db.AiEvaluationReports.Add(report);
        await _db.SaveChangesAsync(ct);
        _cache.Set(cacheKey, report, TimeSpan.FromMinutes(10));
        return report;
    }

    public async Task<AiEvaluationReport> GetLatestAsync(Guid jobId, Guid candidateId, CancellationToken ct = default)
    {
        var report = await _db.AiEvaluationReports
            .Where(x => x.JobId == jobId && x.CandidateId == candidateId)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(ct);

        return report ?? throw new NotFoundException("AI evaluation report not found.");
    }

    public async Task<IReadOnlyCollection<AiEvaluationReport>> GetHistoryAsync(Guid jobId, Guid candidateId, int limit, int offset, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 200);
        offset = Math.Max(0, offset);

        return await _db.AiEvaluationReports
            .AsNoTracking()
            .Where(x => x.JobId == jobId && x.CandidateId == candidateId)
            .OrderByDescending(x => x.Version)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<AiEvaluationReport> GetLatestByApplicationAsync(Guid applicationId, CancellationToken ct = default)
    {
        var app = await _db.Applications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == applicationId, ct)
            ?? throw new NotFoundException("Application not found.");

        return await GetLatestAsync(app.JobId, app.CandidateId, ct);
    }

    private static string GetCacheKey(Guid jobId, Guid candidateId) => $"ai-eval:{jobId:N}:{candidateId:N}";

    private static string BuildSystemPrompt()
    {
        return """
You are an internal recruiting evaluator.
Use only provided job, weights, profile, and deterministic matching data.
Do not use external data and do not infer sensitive attributes.
If evidence is missing, return empty/null fields.
Every strength/risk must include at least one evidence quote.
Return strictly valid JSON matching schema.
""";
    }

    private static string BuildSchema()
    {
        return """
{
  "overall_recommendation": "shortlist|hold|reject",
  "confidence": 0.0,
  "competency_assessment": [
    {
      "key": "technical|problem_solving|communication|culture_fit|domain_knowledge",
      "score": 0,
      "rationale": "string",
      "evidence_quotes": [{"quote":"string","related_to":"string"}]
    }
  ],
  "skill_assessment": {
    "weighted_coverage": 0.0,
    "matched": [{"skill":"string","weight":0.0,"is_required":true}],
    "missing_required": ["string"],
    "missing_nice": ["string"]
  },
  "strengths": [{"title":"string","detail":"string","evidence_quotes":[{"quote":"string","related_to":"string"}]}],
  "risks": [{"title":"string","detail":"string","severity":1,"evidence_quotes":[{"quote":"string","related_to":"string"}]}],
  "verification_questions": [{"category":"technical|behavioral|case","question":"string","why":"string"}],
  "evidence_quotes": [{"quote":"string","source":"cv_profile","related_to":"string"}]
}
""";
    }

    private static AiEvaluationOutput ParseAndSanitizeOutput(string rawJson, IReadOnlyCollection<JobSkillWeight> skillWeights)
    {
        AiEvaluationOutput output;
        try
        {
            output = JsonSerializer.Deserialize<AiEvaluationOutput>(rawJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new AiEvaluationOutput();
        }
        catch
        {
            output = new AiEvaluationOutput();
        }

        output.OverallRecommendation = AllowedRecommendations.Contains(output.OverallRecommendation)
            ? output.OverallRecommendation.ToLowerInvariant()
            : "hold";

        output.EvidenceQuotes = SanitizeEvidenceQuotes(output.EvidenceQuotes);

        output.CompetencyAssessment = output.CompetencyAssessment
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .Select(x =>
            {
                x.Key = x.Key.Trim();
                x.Score = Math.Clamp(x.Score, 0, 5);
                x.Rationale = StripSensitive(x.Rationale);
                x.EvidenceQuotes = SanitizeEvidenceSnippets(x.EvidenceQuotes);
                return x;
            })
            .Where(x => x.EvidenceQuotes.Count > 0)
            .ToList();

        output.SkillAssessment ??= new AiSkillAssessment();
        output.SkillAssessment.WeightedCoverage = Math.Clamp(output.SkillAssessment.WeightedCoverage, 0, 1);
        output.SkillAssessment.Matched = output.SkillAssessment.Matched
            .Where(x => !string.IsNullOrWhiteSpace(x.Skill))
            .Select(x => new AiMatchedSkill
            {
                Skill = SkillNormalization.Normalize(x.Skill),
                Weight = Math.Clamp(x.Weight, 0, 1),
                IsRequired = x.IsRequired
            })
            .DistinctBy(x => x.Skill, StringComparer.OrdinalIgnoreCase)
            .ToList();

        output.SkillAssessment.MissingRequired = output.SkillAssessment.MissingRequired
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(SkillNormalization.Normalize)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        output.SkillAssessment.MissingNice = output.SkillAssessment.MissingNice
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(SkillNormalization.Normalize)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (output.SkillAssessment.Matched.Count == 0 && skillWeights.Count > 0)
        {
            output.SkillAssessment.WeightedCoverage = 0;
        }

        output.Strengths = output.Strengths
            .Where(x => !string.IsNullOrWhiteSpace(x.Title) && !string.IsNullOrWhiteSpace(x.Detail))
            .Select(x => new AiInsight
            {
                Title = StripSensitive(x.Title),
                Detail = StripSensitive(x.Detail),
                EvidenceQuotes = SanitizeEvidenceSnippets(x.EvidenceQuotes)
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Title) && !string.IsNullOrWhiteSpace(x.Detail))
            .Where(x => x.EvidenceQuotes.Count > 0)
            .ToList();

        output.Risks = output.Risks
            .Where(x => !string.IsNullOrWhiteSpace(x.Title) && !string.IsNullOrWhiteSpace(x.Detail))
            .Select(x => new AiRiskInsight
            {
                Title = StripSensitive(x.Title),
                Detail = StripSensitive(x.Detail),
                Severity = Math.Clamp(x.Severity, 1, 5),
                EvidenceQuotes = SanitizeEvidenceSnippets(x.EvidenceQuotes)
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Title) && !string.IsNullOrWhiteSpace(x.Detail))
            .Where(x => x.EvidenceQuotes.Count > 0)
            .ToList();

        output.VerificationQuestions = output.VerificationQuestions
            .Where(x => !string.IsNullOrWhiteSpace(x.Question))
            .Where(x => !ContainsSensitiveContent(x.Question) && !ContainsSensitiveContent(x.Why))
            .Select(x => new AiVerificationQuestion
            {
                Category = x.Category,
                Question = x.Question.Trim(),
                Why = x.Why?.Trim() ?? string.Empty
            })
            .ToList();

        output.Confidence = Math.Clamp(output.Confidence, 0.0, 1.0);
        return output;
    }

    private static List<AiEvidenceQuote> SanitizeEvidenceQuotes(IEnumerable<AiEvidenceQuote>? quotes)
    {
        return (quotes ?? Array.Empty<AiEvidenceQuote>())
            .Where(x => !string.IsNullOrWhiteSpace(x.Quote))
            .Select(x => new AiEvidenceQuote
            {
                Quote = StripSensitive(x.Quote),
                Source = "cv_profile",
                RelatedTo = string.IsNullOrWhiteSpace(x.RelatedTo) ? "general" : StripSensitive(x.RelatedTo)
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Quote))
            .ToList();
    }

    private static List<AiEvidenceSnippet> SanitizeEvidenceSnippets(IEnumerable<AiEvidenceSnippet>? quotes)
    {
        return (quotes ?? Array.Empty<AiEvidenceSnippet>())
            .Where(x => !string.IsNullOrWhiteSpace(x.Quote))
            .Select(x => new AiEvidenceSnippet
            {
                Quote = StripSensitive(x.Quote),
                RelatedTo = string.IsNullOrWhiteSpace(x.RelatedTo) ? "general" : StripSensitive(x.RelatedTo)
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Quote))
            .ToList();
    }

    private static string StripSensitive(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return ContainsSensitiveContent(text) ? string.Empty : text.Trim();
    }

    private static bool ContainsSensitiveContent(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return SensitiveKeywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    private static Dictionary<string, double> ParseWeights(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, double>>(json)
                ?.Where(x => !string.IsNullOrWhiteSpace(x.Key))
                .ToDictionary(x => x.Key.Trim(), x => x.Value, StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static List<string> ParseStringArray(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json)?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList() ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
