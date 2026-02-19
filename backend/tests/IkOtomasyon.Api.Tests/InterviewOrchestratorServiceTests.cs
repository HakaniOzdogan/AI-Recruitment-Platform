using System.Text.Json;
using IkOtomasyon.Api.Data;
using IkOtomasyon.Api.Entities;
using IkOtomasyon.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IkOtomasyon.Api.Tests;

public class InterviewOrchestratorServiceTests
{
    [Fact]
    public async Task SchemaInvalid_FallsBackToTemplate()
    {
        await using var db = BuildDbContext();
        var session = await SeedSessionAsync(db, "ADAPTIVE", """["sql"]""");
        var orchestrator = BuildService(
            db,
            new SequenceLlmClient(["not json"]),
            llmEnabled: true,
            adaptiveEnabled: true);

        var result = await orchestrator.AddCandidateMessageAsync(session.Id, "SQL ve index optimizasyonu yaptım.");

        Assert.True(result.UsedFallback);
        Assert.False(result.PlanGenerated);
        Assert.False(result.GuardrailBlocked);

        var latestPlan = await db.InterviewPlans.OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync();
        Assert.NotNull(latestPlan);
        Assert.Contains("\"SchemaValid\":false", latestPlan!.PlanRationaleJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AllowlistViolation_TriggersGuardrailAndFallback()
    {
        await using var db = BuildDbContext();
        var session = await SeedSessionAsync(db, "ADAPTIVE", """["sql"]""");
        var llm = new SequenceLlmClient(
        [
            """
            {
              "signals":[{"term":"SQL","topic_key":"sql","confidence":0.9}],
              "competency_estimates":{"technical":3.0},
              "depth":{"clarity":3.0,"specificity":3.0},
              "risk_flags":{"vague_answer":false,"contradiction":false,"overclaim":false},
              "evidence_snippets":[{"quote":"SQL index optimizasyonu yaptım","related_to":"technical"}],
              "next_focus":[{"topic_key":"sql","why":"Detay doğrulama","priority":1}]
            }
            """,
            """
            {
              "plan_horizon":2,
              "planned_questions":[
                {"type":"generated","category":"technical","topic_key":"forbidden_topic","difficulty":3,"question_text":"Forbidden konu anlat","why":"test","guardrails_ok":true}
              ]
            }
            """
        ]);

        var orchestrator = BuildService(db, llm, llmEnabled: true, adaptiveEnabled: true);
        var result = await orchestrator.AddCandidateMessageAsync(session.Id, "SQL tarafında index ve query tuning yaptım.");

        Assert.True(result.UsedFallback);
        Assert.True(result.GuardrailBlocked);
        Assert.False(result.PlanGenerated);

        var latestPlan = await db.InterviewPlans.OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync();
        Assert.NotNull(latestPlan);
        Assert.Contains("\"GuardrailBlocked\":true", latestPlan!.PlanRationaleJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AdaptiveMode_WhenDisabled_ThrowsConflict()
    {
        await using var db = BuildDbContext();
        var session = await SeedSessionAsync(db, "OFF", """["sql"]""");
        var orchestrator = BuildService(db, new SequenceLlmClient(["{}"]), llmEnabled: false, adaptiveEnabled: false);

        await Assert.ThrowsAsync<ConflictException>(() => orchestrator.UpdateModeAsync(session.Id, "ADAPTIVE"));
    }

    [Fact]
    public async Task AdaptiveMode_WhenEnabled_UsesPlanQuestion()
    {
        await using var db = BuildDbContext();
        var session = await SeedSessionAsync(db, "ADAPTIVE", """["sql"]""");
        var llm = new SequenceLlmClient(
        [
            """
            {
              "signals":[{"term":"SQL","topic_key":"sql","confidence":0.9}],
              "competency_estimates":{"technical":3.0,"problem_solving":3.0,"communication":3.0,"culture_fit":2.5,"domain_knowledge":3.0},
              "depth":{"clarity":3.0,"specificity":3.0},
              "risk_flags":{"vague_answer":false,"contradiction":false,"overclaim":false},
              "evidence_snippets":[{"quote":"SQL index optimizasyonu yaptım","related_to":"technical"}],
              "next_focus":[{"topic_key":"sql","why":"Detay doğrulama","priority":1}]
            }
            """,
            """
            {
              "plan_horizon":2,
              "planned_questions":[
                {"type":"generated","category":"technical","topic_key":"sql","difficulty":3,"question_text":"SQL index stratejini adım adım anlatır mısın?","why":"Derinlik kontrolü","guardrails_ok":true},
                {"type":"generated","category":"behavioral","topic_key":"decision_making","difficulty":2,"question_text":"Karar sürecinde nasıl önceliklendiriyorsun?","why":"Davranışsal denge","guardrails_ok":true}
              ]
            }
            """
        ]);

        var orchestrator = BuildService(db, llm, llmEnabled: true, adaptiveEnabled: true);
        var result = await orchestrator.AddCandidateMessageAsync(session.Id, "SQL index ve query tuning deneyimim var.");

        Assert.False(result.UsedFallback);
        Assert.True(result.PlanGenerated);
        Assert.False(result.GuardrailBlocked);
        Assert.NotNull(result.SystemQuestion);
        Assert.Contains("SQL index", result.SystemQuestion!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SensitiveContent_TriggersGuardrailBlock()
    {
        await using var db = BuildDbContext();
        var session = await SeedSessionAsync(db, "ADAPTIVE", """["sql"]""");
        var llm = new SequenceLlmClient(
        [
            """
            {
              "signals":[{"term":"SQL","topic_key":"sql","confidence":0.8}],
              "competency_estimates":{"technical":3.0},
              "depth":{"clarity":3.0,"specificity":2.0},
              "risk_flags":{"vague_answer":false,"contradiction":false,"overclaim":false},
              "evidence_snippets":[{"quote":"Dinî görüşe göre ekip tercih ederim","related_to":"culture_fit"}],
              "next_focus":[{"topic_key":"sql","why":"Detay doğrulama","priority":1}]
            }
            """,
            """
            {
              "plan_horizon":2,
              "planned_questions":[
                {"type":"generated","category":"technical","topic_key":"sql","difficulty":3,"question_text":"SQL transaction yönetimini anlatır mısın?","why":"Derinlik ölçümü","guardrails_ok":true},
                {"type":"generated","category":"behavioral","topic_key":"team_conflict","difficulty":2,"question_text":"Siyasi görüş ayrılıklarında ekip içi uyumu nasıl sağlarsın?","why":"Kültür", "guardrails_ok":true}
              ]
            }
            """
        ]);

        var orchestrator = BuildService(db, llm, llmEnabled: true, adaptiveEnabled: true);
        var result = await orchestrator.AddCandidateMessageAsync(session.Id, "SQL transaction ve isolation level kullandım.");

        Assert.True(result.UsedFallback);
        Assert.True(result.GuardrailBlocked);
    }

    private static InterviewOrchestratorService BuildService(AppDbContext db, ILLMClient llmClient, bool llmEnabled, bool adaptiveEnabled)
    {
        return new InterviewOrchestratorService(
            db,
            llmClient,
            Options.Create(new LlmOptions
            {
                Enabled = llmEnabled,
                Provider = "openai",
                ApiKey = "test-key",
                Model = "test-model",
                TimeoutSeconds = 5
            }),
            Options.Create(new AdaptiveInterviewOptions
            {
                Enabled = adaptiveEnabled,
                LastMessageWindow = 8,
                PlanHorizonMin = 2,
                PlanHorizonMax = 3
            }));
    }

    private static async Task<InterviewSession> SeedSessionAsync(AppDbContext db, string mode, string requiredSkillsJson)
    {
        var job = new JobPosting
        {
            Id = Guid.NewGuid(),
            Title = "Backend Engineer",
            Description = "Role",
            RequiredSkillsJson = requiredSkillsJson,
            NiceToHaveSkillsJson = "[]",
            CreatedByUserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };
        var candidate = new Candidate
        {
            Id = Guid.NewGuid(),
            FullName = "Test Candidate",
            CreatedAt = DateTime.UtcNow
        };
        var stage = new PipelineStage
        {
            Id = Guid.NewGuid(),
            Name = "Applied",
            Order = 1,
            IsTerminal = false
        };
        var app = new Application
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            CandidateId = candidate.Id,
            StageId = stage.Id,
            LastUpdatedByUserId = Guid.NewGuid()
        };
        var session = new InterviewSession
        {
            Id = Guid.NewGuid(),
            ApplicationId = app.Id,
            AiMode = mode,
            Status = InterviewSessionStatus.InProgress,
            CreatedAt = DateTime.UtcNow
        };

        db.JobPostings.Add(job);
        db.Candidates.Add(candidate);
        db.PipelineStages.Add(stage);
        db.Applications.Add(app);
        db.InterviewSessions.Add(session);
        await db.SaveChangesAsync();

        return session;
    }

    private static AppDbContext BuildDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    private class SequenceLlmClient : ILLMClient
    {
        private readonly Queue<string> _responses;

        public SequenceLlmClient(IEnumerable<string> responses)
        {
            _responses = new Queue<string>(responses);
        }

        public Task<string> GenerateStructuredAsync(string systemPrompt, string userPrompt, string jsonSchema, CancellationToken ct = default)
        {
            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No mock LLM response available.");
            }

            return Task.FromResult(_responses.Dequeue());
        }
    }
}
