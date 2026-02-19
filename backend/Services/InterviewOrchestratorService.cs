using System.Text.Json;
using System.Diagnostics;
using System.Text.RegularExpressions;
using IkOtomasyon.Api.Contracts;
using IkOtomasyon.Api.Data;
using IkOtomasyon.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IkOtomasyon.Api.Services;

public class InterviewOrchestratorService
{
    private static readonly HashSet<string> AllowedCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "technical", "behavioral", "case", "culture"
    };

    private static readonly HashSet<string> TopicAllowlist = new(StringComparer.OrdinalIgnoreCase)
    {
        "sql_indexes", "transactions", "cqrs", "microservices", "docker", "kubernetes", "cloud_aws", "cloud_azure",
        "rest_api", "system_design", "testing", "git", "oop", "datastructures", "algorithms", "team_conflict",
        "ownership", "communication",
        "csharp", "dotnet", "sql", "postgresql", "mysql", "nosql", "redis", "cqrs", "event_sourcing",
        "docker", "kubernetes", "azure", "aws", "gcp", "microservices", "api_design", "rest", "grpc",
        "javascript", "typescript", "react", "angular", "node.js", "system_design", "architecture",
        "testing", "unit_testing", "integration_testing", "ci_cd", "devops", "communication",
        "problem_solving", "decision_making", "culture", "teamwork", "leadership", "domain_knowledge"
    };

    private static readonly HashSet<string> SensitiveKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "health", "religion", "politics", "race", "sexual", "disability",
        "sağlık", "din", "siyaset", "ırk", "cinsel", "mezhep", "engellilik", "etnik", "sendika"
    };

    private static readonly Regex[] SensitivePatterns =
    [
        new Regex(@"\b(race|ethnicity|ethnic|sexual\s+life|sexual\s+orientation|religion|politics|union)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"\b(ırk|etnik|cinsel\s+hayat|cinsel\s+yönelim|din|siyaset|sendika)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    ];

    private readonly AppDbContext _db;
    private readonly ILLMClient _llmClient;
    private readonly LlmOptions _llmOptions;
    private readonly AdaptiveInterviewOptions _adaptiveOptions;

    public InterviewOrchestratorService(
        AppDbContext db,
        ILLMClient llmClient,
        IOptions<LlmOptions> llmOptions,
        IOptions<AdaptiveInterviewOptions> adaptiveOptions)
    {
        _db = db;
        _llmClient = llmClient;
        _llmOptions = llmOptions.Value;
        _adaptiveOptions = adaptiveOptions.Value;
    }

    public async Task<InterviewSession> UpdateModeAsync(Guid sessionId, string aiMode, CancellationToken ct = default)
    {
        var session = await _db.InterviewSessions.FirstOrDefaultAsync(x => x.Id == sessionId, ct)
            ?? throw new NotFoundException("Interview session not found.");

        var normalizedMode = NormalizeMode(aiMode);
        if (normalizedMode == "ADAPTIVE" && (!_llmOptions.Enabled || !_adaptiveOptions.Enabled))
        {
            throw new ConflictException("Adaptive interview not enabled");
        }

        session.AiMode = normalizedMode;
        if (session.AiMode == "OFF")
        {
            session.AiModelName = null;
            session.AiLastPlanAt = null;
        }
        else
        {
            session.AiModelName = _llmOptions.Model;
        }

        await _db.SaveChangesAsync(ct);
        return session;
    }

    public async Task<InterviewSession?> GetSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        return await _db.InterviewSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == sessionId, ct);
    }

    public async Task<List<InterviewMessage>?> GetMessagesAsync(Guid sessionId, CancellationToken ct = default)
    {
        var exists = await _db.InterviewSessions.AnyAsync(x => x.Id == sessionId, ct);
        if (!exists)
        {
            return null;
        }

        return await _db.InterviewMessages
            .AsNoTracking()
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<InterviewAiStateResponse> GetAiStateAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _db.InterviewSessions.FirstOrDefaultAsync(x => x.Id == sessionId, ct)
            ?? throw new NotFoundException("Interview session not found.");

        var latestInsight = await _db.InterviewInsights
            .AsNoTracking()
            .Where(x => x.SessionId == sessionId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var latestPlan = await _db.InterviewPlans
            .AsNoTracking()
            .Where(x => x.SessionId == sessionId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var rationale = ParseRationale(latestPlan?.PlanRationaleJson);

        return new InterviewAiStateResponse(
            session.Id,
            session.AiMode,
            session.AiModelName,
            _llmOptions.Provider,
            session.AiLastPlanAt,
            rationale.AnalyzeLatencyMs,
            rationale.PlanLatencyMs,
            latestInsight is null ? null : JsonSerializer.Serialize(new
            {
                latestInsight.TurnIndex,
                latestInsight.SignalsJson,
                latestInsight.CompetencyJson,
                latestInsight.DepthJson,
                latestInsight.RiskFlagsJson,
                latestInsight.EvidenceSnippetsJson,
                latestInsight.CreatedAt
            }),
            latestPlan is null ? null : JsonSerializer.Serialize(new
            {
                latestPlan.FromTurnIndex,
                latestPlan.PlannedQuestionsJson,
                latestPlan.PlanRationaleJson,
                latestPlan.CreatedAt
            }),
            rationale.FallbackUsed,
            rationale.SchemaValid,
            rationale.GuardrailBlocked);
    }

    public async Task<InterviewTurnResponse> AddCandidateMessageAsync(Guid sessionId, string content, CancellationToken ct = default)
    {
        var session = await _db.InterviewSessions
            .Include(x => x.Application)
            .ThenInclude(x => x!.Job)
            .FirstOrDefaultAsync(x => x.Id == sessionId, ct)
            ?? throw new NotFoundException("Interview session not found.");

        if (session.Status == InterviewSessionStatus.Completed)
        {
            throw new ConflictException("Interview session already completed.");
        }

        var candidateMessage = new InterviewMessage
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Role = "candidate",
            Content = content.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        _db.InterviewMessages.Add(candidateMessage);
        await _db.SaveChangesAsync(ct);

        var mode = NormalizeMode(session.AiMode);
        if (mode == "OFF" || mode == "ASSIST")
        {
            return await AskTemplateQuestionAsync(session, candidateMessage.Id, usedFallback: false, planGenerated: false, guardrailBlocked: false, ct);
        }

        if (!_llmOptions.Enabled || !_adaptiveOptions.Enabled)
        {
            return await AskTemplateQuestionAsync(session, candidateMessage.Id, usedFallback: true, planGenerated: false, guardrailBlocked: false, ct);
        }

        var orchestrated = await RunAdaptiveAsync(session, candidateMessage.Id, ct);
        if (orchestrated.SystemMessageId is not null)
        {
            return orchestrated;
        }

        return await AskTemplateQuestionAsync(
            session,
            candidateMessage.Id,
            usedFallback: orchestrated.UsedFallback,
            planGenerated: false,
            guardrailBlocked: orchestrated.GuardrailBlocked,
            ct);
    }

    public async Task<InterviewMessage> AddInterviewerQuestionAsync(Guid sessionId, string content, CancellationToken ct = default)
    {
        var session = await _db.InterviewSessions.FirstOrDefaultAsync(x => x.Id == sessionId, ct)
            ?? throw new NotFoundException("Interview session not found.");

        if (session.Status == InterviewSessionStatus.Completed)
        {
            throw new ConflictException("Interview session already completed.");
        }

        var message = new InterviewMessage
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Role = "hiring",
            Content = TruncateAndSanitize(content, 350),
            CreatedAt = DateTime.UtcNow
        };

        _db.InterviewMessages.Add(message);
        await _db.SaveChangesAsync(ct);
        return message;
    }

    public async Task<InterviewMessage> AddCandidateAnswerAsync(Guid sessionId, string content, CancellationToken ct = default)
    {
        var session = await _db.InterviewSessions.FirstOrDefaultAsync(x => x.Id == sessionId, ct)
            ?? throw new NotFoundException("Interview session not found.");

        if (session.Status == InterviewSessionStatus.Completed)
        {
            throw new ConflictException("Interview session already completed.");
        }

        var message = new InterviewMessage
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Role = "candidate",
            Content = TruncateAndSanitize(content, 350),
            CreatedAt = DateTime.UtcNow
        };

        _db.InterviewMessages.Add(message);
        await _db.SaveChangesAsync(ct);
        return message;
    }

    private async Task<InterviewTurnResponse> RunAdaptiveAsync(InterviewSession session, Guid candidateMessageId, CancellationToken ct)
    {
        var sessionId = session.Id;
        var messages = await _db.InterviewMessages
            .AsNoTracking()
            .Where(x => x.SessionId == sessionId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(Math.Max(2, _adaptiveOptions.LastMessageWindow))
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);

        var turnIndex = await _db.InterviewMessages.CountAsync(x => x.SessionId == sessionId, ct);
        var job = session.Application?.Job;
        if (job is null)
        {
            return new InterviewTurnResponse(sessionId, candidateMessageId, null, null, session.AiMode, true, false, false);
        }

        var allowedTopics = BuildAllowedTopics(job);
        var (analyze, analyzeSchemaValid, analyzeGuardrailBlocked, analyzeLatencyMs) = await AnalyzeAsync(job, messages, allowedTopics, ct);
        var insight = new InterviewInsight
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            TurnIndex = turnIndex,
            SignalsJson = JsonSerializer.Serialize(analyze.Signals),
            CompetencyJson = JsonSerializer.Serialize(analyze.CompetencyEstimates),
            DepthJson = JsonSerializer.Serialize(analyze.Depth),
            RiskFlagsJson = JsonSerializer.Serialize(analyze.RiskFlags),
            EvidenceSnippetsJson = JsonSerializer.Serialize(analyze.EvidenceSnippets.Select(x => new
            {
                quote = TruncateAndSanitize(x.Quote, 200),
                related_to = x.RelatedTo
            })),
            CreatedAt = DateTime.UtcNow
        };
        _db.InterviewInsights.Add(insight);

        var (plan, planSchemaValid, planFallbackUsed, planGuardrailBlocked, planLatencyMs) = await BuildPlanAsync(job, analyze, messages, allowedTopics, ct);
        var fallbackUsed = planFallbackUsed || !analyzeSchemaValid || !planSchemaValid;
        var guardrailBlocked = analyzeGuardrailBlocked || planGuardrailBlocked;
        var planEntity = new InterviewPlan
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            FromTurnIndex = turnIndex,
            PlannedQuestionsJson = JsonSerializer.Serialize(plan.PlannedQuestions),
            PlanRationaleJson = JsonSerializer.Serialize(new PlanRationaleState
            {
                FallbackUsed = fallbackUsed,
                SchemaValid = analyzeSchemaValid && planSchemaValid,
                GuardrailBlocked = guardrailBlocked,
                AnalyzeLatencyMs = analyzeLatencyMs,
                PlanLatencyMs = planLatencyMs
            }),
            CreatedAt = DateTime.UtcNow
        };
        _db.InterviewPlans.Add(planEntity);

        if (fallbackUsed || guardrailBlocked)
        {
            await _db.SaveChangesAsync(ct);
            return new InterviewTurnResponse(sessionId, candidateMessageId, null, null, session.AiMode, true, false, guardrailBlocked);
        }

        var next = plan.PlannedQuestions.FirstOrDefault();
        if (next is null)
        {
            await _db.SaveChangesAsync(ct);
            return new InterviewTurnResponse(sessionId, candidateMessageId, null, null, session.AiMode, true, false, false);
        }

        var selected = await MapToBankQuestionAsync(next, ct);
        var questionText = selected?.QuestionText ?? next.QuestionText;
        if (string.IsNullOrWhiteSpace(questionText))
        {
            await _db.SaveChangesAsync(ct);
            return new InterviewTurnResponse(sessionId, candidateMessageId, null, null, session.AiMode, true, false, false);
        }

        var systemMessage = new InterviewMessage
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Role = "system",
            Content = TruncateAndSanitize(questionText, 350),
            QuestionBankId = selected?.Id,
            CreatedAt = DateTime.UtcNow
        };
        _db.InterviewMessages.Add(systemMessage);

        var askedCount = await _db.InterviewMessages
            .CountAsync(x => x.SessionId == sessionId && x.Role == "system", ct);
        if (askedCount >= 8)
        {
            session.Status = InterviewSessionStatus.Completed;
            session.CompletedAt = DateTime.UtcNow;
            _db.InterviewMessages.Add(new InterviewMessage
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                Role = "system",
                Content = "Teşekkürler, mülakat tamamlandı.",
                CreatedAt = DateTime.UtcNow
            });
        }

        session.AiLastPlanAt = DateTime.UtcNow;
        session.AiModelName = _llmOptions.Model;
        await _db.SaveChangesAsync(ct);

        return new InterviewTurnResponse(sessionId, candidateMessageId, systemMessage.Id, systemMessage.Content, session.AiMode, false, true, false);
    }

    private async Task<InterviewTurnResponse> AskTemplateQuestionAsync(
        InterviewSession session,
        Guid candidateMessageId,
        bool usedFallback,
        bool planGenerated,
        bool guardrailBlocked,
        CancellationToken ct)
    {
        var askedCount = await _db.InterviewMessages.CountAsync(x => x.SessionId == session.Id && x.Role == "system", ct);
        var latestCandidateMessage = await _db.InterviewMessages
            .AsNoTracking()
            .Where(x => x.SessionId == session.Id && x.Role == "candidate")
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.Content)
            .FirstOrDefaultAsync(ct);
        var question = BuildConversationalFallbackMessage(latestCandidateMessage, askedCount, session.Application?.Job?.Title);
        var systemMessage = new InterviewMessage
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Role = "system",
            Content = TruncateAndSanitize(question, 350),
            CreatedAt = DateTime.UtcNow
        };
        _db.InterviewMessages.Add(systemMessage);
        if (askedCount >= 8)
        {
            session.Status = InterviewSessionStatus.Completed;
            session.CompletedAt = DateTime.UtcNow;
            _db.InterviewMessages.Add(new InterviewMessage
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                Role = "system",
                Content = "Tesekkurler, mulakatimizi burada tamamliyoruz. Katkilarin icin tesekkur ederim.",
                CreatedAt = DateTime.UtcNow
            });
        }
        if (NormalizeMode(session.AiMode) != "OFF")
        {
            session.AiModelName ??= "simulated-assistant-v1";
        }
        await _db.SaveChangesAsync(ct);
        return new InterviewTurnResponse(session.Id, candidateMessageId, systemMessage.Id, systemMessage.Content, session.AiMode, usedFallback, planGenerated, guardrailBlocked);
    }
    private static string BuildConversationalFallbackMessage(string? latestCandidateMessage, int askedCount, string? jobTitle)
    {
        var candidateText = TruncateAndSanitize(latestCandidateMessage, 240);
        var acknowledgement = askedCount switch
        {
            0 => "Paylasimin icin tesekkurler, iyi bir baslangic yaptik.",
            1 => "Anlattigin noktalar net, biraz daha derine inelim.",
            2 => "Bunu not ettim, simdi karar mekanizmani acalim.",
            _ => "Guzel, bir adim daha derinlestirelim."
        };
        if (string.IsNullOrWhiteSpace(candidateText))
        {
            return $"{acknowledgement} Son projenizde en kritik teknik karari hangi verilere bakarak verdiniz?";
        }
        var normalized = candidateText.ToLowerInvariant();
        var followUp = normalized switch
        {
            var x when x.Contains("modern") || x.Contains("teknoloji") || x.Contains("stack")
                => "Kullandigin teknolojiler arasinda secim yaparken hangi olcutleri kullandin ve neyi feda ettin?",
            var x when x.Contains("takim") || x.Contains("ekip") || x.Contains("iletisim")
                => "Takim ici bir fikir ayriliginda nasil ilerledin, sonuc ne oldu?",
            var x when x.Contains("performans") || x.Contains("optimiz")
                => "Performansi iyilestirmek icin hangi metrikleri takip ettin ve olculebilir sonuc neydi?",
            var x when x.Contains("problem") || x.Contains("hata") || x.Contains("bug")
                => "Bu problemi cozerken izledigin adimlari ve alternatifleri karsilastirma seklini anlatir misin?",
            _ => "Bu deneyimde verdigin en zor karari ve trade-off mantigini adim adim anlatir misin?"
        };
        var context = string.IsNullOrWhiteSpace(jobTitle) ? string.Empty : $" {jobTitle} rolu baglaminda dusunursen";
        return $"{acknowledgement}{context}, {followUp}";
    }
    private async Task<(AdaptiveAnalyzeOutput Output, bool SchemaValid, bool GuardrailBlocked, long? LatencyMs)> AnalyzeAsync(
        JobPosting job,
        List<InterviewMessage> messages,
        HashSet<string> allowedTopics,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var userPrompt = JsonSerializer.Serialize(new
            {
                job = new
                {
                    title = job.Title,
                    requiredSkills = ParseSkills(job.RequiredSkillsJson),
                    niceToHave = ParseSkills(job.NiceToHaveSkillsJson),
                    job.MinExperienceMonths,
                    competencyWeights = job.CompetencyWeightsJson
                },
                rubric_keys = RubricKeys.All,
                messages = messages.Select(x => new { x.Role, x.Content })
            });

            var raw = await _llmClient.GenerateStructuredAsync(AnalyzeSystemPrompt, userPrompt, AnalyzeSchema, ct);
            var output = JsonSerializer.Deserialize<AdaptiveAnalyzeOutput>(raw, JsonOptions) ?? new AdaptiveAnalyzeOutput();
            var (sanitized, guardrailBlocked) = SanitizeAnalyze(output, allowedTopics);
            return (sanitized, true, guardrailBlocked, stopwatch.ElapsedMilliseconds);
        }
        catch (LlmProviderException)
        {
            return (DeterministicAnalyze(messages), false, false, stopwatch.ElapsedMilliseconds);
        }
        catch (LlmSchemaException)
        {
            return (DeterministicAnalyze(messages), false, false, stopwatch.ElapsedMilliseconds);
        }
        catch (LlmTimeoutException)
        {
            return (DeterministicAnalyze(messages), false, false, stopwatch.ElapsedMilliseconds);
        }
        catch
        {
            return (DeterministicAnalyze(messages), false, false, stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task<(AdaptivePlanOutput Output, bool SchemaValid, bool FallbackUsed, bool GuardrailBlocked, long? LatencyMs)> BuildPlanAsync(
        JobPosting job,
        AdaptiveAnalyzeOutput analyze,
        List<InterviewMessage> messages,
        HashSet<string> allowedTopics,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var bank = await _db.InterviewQuestionBanks
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Category)
                .Take(50)
                .ToListAsync(ct);

            foreach (var item in bank.Select(x => SanitizeTopicKey(x.TopicKey)))
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    allowedTopics.Add(item);
                }
            }

            var prompt = JsonSerializer.Serialize(new
            {
                analyze,
                requiredSkills = ParseSkills(job.RequiredSkillsJson),
                mandatoryCategories = new[] { "technical", "behavioral", "case" },
                bank = bank.Select(x => new { x.Category, x.TopicKey, x.Difficulty, x.QuestionText })
            });

            var raw = await _llmClient.GenerateStructuredAsync(PlanSystemPrompt, prompt, PlanSchema, ct);
            var output = JsonSerializer.Deserialize<AdaptivePlanOutput>(raw, JsonOptions) ?? new AdaptivePlanOutput();
            var (sanitized, guardrailBlocked) = SanitizePlan(output, allowedTopics);
            if (sanitized.PlannedQuestions.Count == 0)
            {
                return (DeterministicPlan(analyze, job), false, true, guardrailBlocked, stopwatch.ElapsedMilliseconds);
            }

            return (sanitized, true, false, guardrailBlocked, stopwatch.ElapsedMilliseconds);
        }
        catch (LlmProviderException)
        {
            return (DeterministicPlan(analyze, job), false, true, false, stopwatch.ElapsedMilliseconds);
        }
        catch (LlmSchemaException)
        {
            return (DeterministicPlan(analyze, job), false, true, false, stopwatch.ElapsedMilliseconds);
        }
        catch (LlmTimeoutException)
        {
            return (DeterministicPlan(analyze, job), false, true, false, stopwatch.ElapsedMilliseconds);
        }
        catch
        {
            return (DeterministicPlan(analyze, job), false, true, false, stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task<InterviewQuestionBank?> MapToBankQuestionAsync(AdaptivePlannedQuestion question, CancellationToken ct)
    {
        if (!question.Type.Equals("bank", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return await _db.InterviewQuestionBanks
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Where(x => x.Category == question.Category && x.TopicKey == question.TopicKey)
            .OrderBy(x => Math.Abs(x.Difficulty - question.Difficulty))
            .FirstOrDefaultAsync(ct);
    }

    private static (AdaptiveAnalyzeOutput Output, bool GuardrailBlocked) SanitizeAnalyze(AdaptiveAnalyzeOutput input, HashSet<string> allowedTopics)
    {
        var guardrailBlocked = false;

        var signals = new List<AdaptiveSignal>();
        foreach (var item in input.Signals ?? new List<AdaptiveSignal>())
        {
            var topic = SanitizeTopicKey(item.TopicKey);
            var term = TruncateAndSanitize(item.Term, 80);
            if (topic.Length == 0 || term.Length == 0 || !allowedTopics.Contains(topic))
            {
                guardrailBlocked = true;
                continue;
            }

            signals.Add(new AdaptiveSignal
            {
                Term = term,
                TopicKey = topic,
                Confidence = Math.Clamp(item.Confidence, 0, 1)
            });
        }
        input.Signals = signals;

        input.CompetencyEstimates = (input.CompetencyEstimates ?? new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase))
            .Where(x => RubricKeys.All.Contains(x.Key, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(x => x.Key, x => Math.Clamp(x.Value, 0, 5), StringComparer.OrdinalIgnoreCase);

        input.Depth = (input.Depth ?? new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase))
            .Where(x => x.Key is "clarity" or "specificity")
            .ToDictionary(x => x.Key, x => Math.Clamp(x.Value, 0, 5), StringComparer.OrdinalIgnoreCase);

        input.RiskFlags = (input.RiskFlags ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase))
            .Where(x => x.Key is "vague_answer" or "contradiction" or "overclaim")
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

        var evidence = new List<AdaptiveEvidence>();
        foreach (var item in input.EvidenceSnippets ?? new List<AdaptiveEvidence>())
        {
            var quote = TruncateAndSanitize(item.Quote, 200);
            if (quote.Length == 0)
            {
                continue;
            }

            if (ContainsSensitive(quote))
            {
                guardrailBlocked = true;
                continue;
            }

            evidence.Add(new AdaptiveEvidence
            {
                Quote = quote,
                RelatedTo = TruncateAndSanitize(item.RelatedTo, 60)
            });
        }
        input.EvidenceSnippets = evidence.Take(5).ToList();

        var nextFocus = new List<AdaptiveNextFocus>();
        foreach (var item in input.NextFocus ?? new List<AdaptiveNextFocus>())
        {
            var topic = SanitizeTopicKey(item.TopicKey);
            if (topic.Length == 0 || !allowedTopics.Contains(topic))
            {
                guardrailBlocked = true;
                continue;
            }

            nextFocus.Add(new AdaptiveNextFocus
            {
                TopicKey = topic,
                Why = TruncateAndSanitize(item.Why, 180),
                Priority = Math.Clamp(item.Priority, 1, 5)
            });
        }
        input.NextFocus = nextFocus.OrderBy(x => x.Priority).Take(5).ToList();

        return (input, guardrailBlocked);
    }

    private AdaptiveAnalyzeOutput DeterministicAnalyze(List<InterviewMessage> messages)
    {
        var candidateText = string.Join(" ", messages
            .Where(x => x.Role.Equals("candidate", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Content));

        var skills = ParseTerms(candidateText, ["c#", ".net", "sql", "docker", "kubernetes", "react", "cqrs"]);
        var evidence = messages
            .Where(x => x.Role.Equals("candidate", StringComparison.OrdinalIgnoreCase))
            .Select(x => TruncateAndSanitize(x.Content, 200))
            .Where(x => x.Length > 0)
            .TakeLast(3)
            .Select(x => new AdaptiveEvidence { Quote = x, RelatedTo = "technical" })
            .ToList();

        return new AdaptiveAnalyzeOutput
        {
            Signals = skills.Select(x => new AdaptiveSignal { Term = x, TopicKey = SanitizeTopicKey(x), Confidence = 0.7 }).ToList(),
            CompetencyEstimates = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                [RubricKeys.Technical] = skills.Count > 0 ? 3 : 2,
                [RubricKeys.ProblemSolving] = candidateText.Contains("trade-off", StringComparison.OrdinalIgnoreCase) ? 3 : 2,
                [RubricKeys.Communication] = candidateText.Length > 120 ? 3 : 2,
                [RubricKeys.CultureFit] = candidateText.Contains("takım", StringComparison.OrdinalIgnoreCase) ? 3 : 2,
                [RubricKeys.DomainKnowledge] = skills.Count > 1 ? 3 : 2
            },
            Depth = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["clarity"] = candidateText.Length > 140 ? 3 : 2,
                ["specificity"] = candidateText.Any(char.IsDigit) ? 3 : 2
            },
            RiskFlags = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                ["vague_answer"] = candidateText.Length < 50,
                ["contradiction"] = false,
                ["overclaim"] = false
            },
            EvidenceSnippets = evidence,
            NextFocus = skills.Take(2).Select((x, idx) => new AdaptiveNextFocus
            {
                TopicKey = SanitizeTopicKey(x),
                Why = "Detected candidate term; probe implementation depth.",
                Priority = idx + 1
            }).ToList()
        };
    }

    private AdaptivePlanOutput DeterministicPlan(AdaptiveAnalyzeOutput analyze, JobPosting job)
    {
        var required = ParseSkills(job.RequiredSkillsJson).Select(SanitizeTopicKey).Where(x => x.Length > 0).ToList();
        var focus = analyze.NextFocus.Select(x => x.TopicKey).Concat(required).Distinct(StringComparer.OrdinalIgnoreCase).Take(_adaptiveOptions.PlanHorizonMax).ToList();
        var questions = new List<AdaptivePlannedQuestion>();
        string? previousTopic = null;

        foreach (var topic in focus)
        {
            if (previousTopic is not null && topic.Equals(previousTopic, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            questions.Add(new AdaptivePlannedQuestion
            {
                Type = "generated",
                Category = "technical",
                TopicKey = topic,
                Difficulty = 3,
                QuestionText = TruncateAndSanitize($"{topic} konusunda gerçek bir üretim senaryosunda aldığın kararları ve trade-off'ları adım adım anlatır mısın?", 350),
                Why = "Fallback deterministic follow-up",
                GuardrailsOk = true
            });
            previousTopic = topic;
        }

        if (questions.Count < 2)
        {
            questions.Add(new AdaptivePlannedQuestion
            {
                Type = "generated",
                Category = "behavioral",
                TopicKey = "decision_making",
                Difficulty = 2,
                QuestionText = "Belirsiz gereksinimlerde karar verme yaklaşımını somut bir örnekle anlatır mısın?",
                Why = "Ensure horizon coverage",
                GuardrailsOk = true
            });
        }

        return new AdaptivePlanOutput
        {
            PlanHorizon = Math.Clamp(questions.Count, _adaptiveOptions.PlanHorizonMin, _adaptiveOptions.PlanHorizonMax),
            PlannedQuestions = questions.Take(_adaptiveOptions.PlanHorizonMax).ToList()
        };
    }

    private (AdaptivePlanOutput Output, bool GuardrailBlocked) SanitizePlan(AdaptivePlanOutput output, HashSet<string> allowedTopics)
    {
        output.PlanHorizon = Math.Clamp(output.PlanHorizon, _adaptiveOptions.PlanHorizonMin, _adaptiveOptions.PlanHorizonMax);
        var sanitized = new List<AdaptivePlannedQuestion>();
        var guardrailBlocked = false;
        string? previousTopic = null;

        foreach (var item in output.PlannedQuestions ?? new List<AdaptivePlannedQuestion>())
        {
            var category = item.Category?.Trim().ToLowerInvariant() ?? string.Empty;
            var topic = SanitizeTopicKey(item.TopicKey);
            var question = TruncateAndSanitize(item.QuestionText, 350);
            if (!AllowedCategories.Contains(category) || topic.Length == 0 || question.Length == 0)
            {
                guardrailBlocked = true;
                continue;
            }

            if (!allowedTopics.Contains(topic))
            {
                guardrailBlocked = true;
                continue;
            }

            if (ContainsSensitive(question) || ContainsSensitive(item.Why))
            {
                guardrailBlocked = true;
                continue;
            }

            if (previousTopic is not null && previousTopic.Equals(topic, StringComparison.OrdinalIgnoreCase))
            {
                guardrailBlocked = true;
                continue;
            }

            sanitized.Add(new AdaptivePlannedQuestion
            {
                Type = item.Type?.Equals("bank", StringComparison.OrdinalIgnoreCase) == true ? "bank" : "generated",
                Category = category,
                TopicKey = topic,
                Difficulty = Math.Clamp(item.Difficulty, 1, 5),
                QuestionText = question,
                Why = TruncateAndSanitize(item.Why, 180),
                GuardrailsOk = item.GuardrailsOk
            });
            previousTopic = topic;

            if (sanitized.Count >= _adaptiveOptions.PlanHorizonMax)
            {
                break;
            }
        }

        output.PlannedQuestions = sanitized;
        output.PlanHorizon = Math.Clamp(sanitized.Count == 0 ? _adaptiveOptions.PlanHorizonMin : sanitized.Count, _adaptiveOptions.PlanHorizonMin, _adaptiveOptions.PlanHorizonMax);
        return (output, guardrailBlocked);
    }

    private HashSet<string> BuildAllowedTopics(JobPosting job)
    {
        var allowed = new HashSet<string>(TopicAllowlist, StringComparer.OrdinalIgnoreCase);

        foreach (var skill in ParseSkills(job.RequiredSkillsJson))
        {
            var normalized = SanitizeTopicKey(skill);
            if (normalized.Length > 0)
            {
                allowed.Add(normalized);
            }
        }

        foreach (var skill in ParseSkills(job.NiceToHaveSkillsJson))
        {
            var normalized = SanitizeTopicKey(skill);
            if (normalized.Length > 0)
            {
                allowed.Add(normalized);
            }
        }

        return allowed;
    }

    private static string SanitizeTopicKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().ToLowerInvariant()
            .Replace(" ", "_")
            .Replace("#", "sharp")
            .Replace(".net", "dotnet")
            .Replace("nodejs", "node.js");

        normalized = new string(normalized.Where(c => char.IsLetterOrDigit(c) || c is '_' or '-' or '.').ToArray());
        return ContainsSensitive(normalized) ? string.Empty : normalized;
    }

    private static string TruncateAndSanitize(string? text, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var clean = text.Replace('\n', ' ').Replace('\r', ' ').Trim();
        if (clean.Length > maxLen)
        {
            clean = clean[..maxLen];
        }

        return clean;
    }

    private static bool ContainsSensitive(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return SensitiveKeywords.Any(x => value.Contains(x, StringComparison.OrdinalIgnoreCase))
            || SensitivePatterns.Any(x => x.IsMatch(value));
    }

    private static string NormalizeMode(string? mode)
    {
        var normalized = mode?.Trim().ToUpperInvariant();
        return normalized is "OFF" or "ASSIST" or "ADAPTIVE" ? normalized : "OFF";
    }

    private static List<string> ParseTerms(string text, IEnumerable<string> allow)
        => allow.Where(x => text.Contains(x, StringComparison.OrdinalIgnoreCase)).ToList();

    private static List<string> ParseSkills(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static PlanRationaleState ParseRationale(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new PlanRationaleState();
        }

        try
        {
            return JsonSerializer.Deserialize<PlanRationaleState>(json, JsonOptions) ?? new PlanRationaleState();
        }
        catch
        {
            return new PlanRationaleState();
        }
    }

    private const string AnalyzeSystemPrompt = """
INTERVIEW_ANALYZE_JSON
Return JSON only and strictly follow the schema contract.
Never include sensitive attributes (health, religion, politics, race, sexual life).
""";

    private const string PlanSystemPrompt = """
INTERVIEW_PLAN_JSON
Return JSON only and strictly follow the schema contract.
Use only allowed job-related topics and keep question_text max 350 chars.
""";

    private const string AnalyzeSchema = """
{
  "signals": [{"term":"string","topic_key":"string","confidence":0.0}],
  "competency_estimates": {"technical":0.0,"problem_solving":0.0,"communication":0.0,"culture_fit":0.0,"domain_knowledge":0.0},
  "depth": {"clarity":0.0,"specificity":0.0},
  "risk_flags": {"vague_answer":true,"contradiction":false,"overclaim":false},
  "evidence_snippets": [{"quote":"string","related_to":"string"}],
  "next_focus": [{"topic_key":"string","why":"string","priority":1}]
}
""";

    private const string PlanSchema = """
{
  "plan_horizon": 3,
  "planned_questions": [
    {"type":"bank|generated","category":"technical|behavioral|case|culture","topic_key":"string","difficulty":3,"question_text":"string","why":"string","guardrails_ok":true}
  ]
}
""";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private class PlanRationaleState
    {
        public bool FallbackUsed { get; set; }
        public bool SchemaValid { get; set; } = true;
        public bool GuardrailBlocked { get; set; }
        public long? AnalyzeLatencyMs { get; set; }
        public long? PlanLatencyMs { get; set; }
    }
}


