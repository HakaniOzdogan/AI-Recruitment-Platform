using IkOtomasyon.Api.Data;
using IkOtomasyon.Api.Entities;
using IkOtomasyon.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IkOtomasyon.Api.Tests;

public class InterviewScoringEnrichmentFlowTests
{
    [Fact]
    public async Task EnrichmentSchemaInvalid_FallsBackToDeterministic()
    {
        using var sp = BuildServiceProvider();
        await using var db = sp.GetRequiredService<AppDbContext>();
        var sessionId = await SeedCompletedSessionAsync(db, "I used SQL indexes and Docker in production.");

        var service = BuildScoringService(
            db,
            new QueueLlmClient(
            [
                """
                {
                  "criterion_key":"technical",
                  "suggested_score":10,
                  "rationale":"",
                  "additional_evidence_quotes":[{"quote":"x","related_to":"technical"}],
                  "confidence":1.5
                }
                """
            ]),
            scoringMode: "llm_assisted",
            llmEnabled: true,
            llmScoringEnabled: true);

        var response = await service.AutoScoreAsync(sessionId, Guid.NewGuid(), force: true);

        Assert.Equal("failed", response.LlmEnrichment);
        Assert.Contains(AuditActions.LlmScoreEnrichFail, response.LlmEnrichmentEvents);
        Assert.DoesNotContain(response.Latest, x => x.EnrichmentStatus == InterviewScoreEnrichmentStatus.Enriched);
    }

    [Fact]
    public async Task EnrichmentDisabled_SkipsAndReturnsDeterministic()
    {
        using var sp = BuildServiceProvider();
        await using var db = sp.GetRequiredService<AppDbContext>();
        var sessionId = await SeedCompletedSessionAsync(db, "I used SQL indexes and Docker in production.");

        var service = BuildScoringService(
            db,
            new QueueLlmClient([]),
            scoringMode: "llm_assisted",
            llmEnabled: true,
            llmScoringEnabled: false);

        var response = await service.AutoScoreAsync(sessionId, Guid.NewGuid(), force: true);

        Assert.Equal("skipped_disabled", response.LlmEnrichment);
        Assert.Contains(AuditActions.LlmScoreEnrichSkip, response.LlmEnrichmentEvents);
        Assert.Contains(response.Latest, x => x.EnrichmentStatus == InterviewScoreEnrichmentStatus.Skipped || x.Status == InterviewCriterionScoreStatus.InsufficientEvidence);
    }

    [Fact]
    public async Task EnrichmentSuccess_CreatesReplacesScoreAndUpdatesOverall()
    {
        using var sp = BuildServiceProvider();
        await using var db = sp.GetRequiredService<AppDbContext>();
        var sessionId = await SeedCompletedSessionAsync(db, "I used SQL indexes and Docker in production.");

        var service = BuildScoringService(
            db,
            new QueueLlmClient(
            [
                """
                {
                  "criterion_key":"technical",
                  "suggested_score":5,
                  "rationale":"Strong technical depth shown through SQL indexing and containerized deployment examples.",
                  "additional_evidence_quotes":[{"quote":"I used SQL indexes and Docker in production.","related_to":"technical"}],
                  "confidence":0.87
                }
                """
            ]),
            scoringMode: "llm_assisted",
            llmEnabled: true,
            llmScoringEnabled: true);

        var response = await service.AutoScoreAsync(sessionId, Guid.NewGuid(), force: true);

        Assert.Contains(AuditActions.LlmScoreEnriched, response.LlmEnrichmentEvents);
        Assert.True(response.OverallScore is not null);
        Assert.Contains(response.Latest, x => x.EnrichmentStatus == InterviewScoreEnrichmentStatus.Enriched);

        var enriched = await db.InterviewCriterionScores.AsNoTracking()
            .Where(x => x.EnrichmentStatus == InterviewScoreEnrichmentStatus.Enriched)
            .ToListAsync();
        Assert.NotEmpty(enriched);
        Assert.Contains(enriched, x => x.ReplacesScoreId is not null);
    }

    [Fact]
    public async Task GuardrailBlock_DoesNotOverwriteDeterministic()
    {
        using var sp = BuildServiceProvider();
        await using var db = sp.GetRequiredService<AppDbContext>();
        var sessionId = await SeedCompletedSessionAsync(db, "I used SQL indexes and Docker in production.");

        var service = BuildScoringService(
            db,
            new QueueLlmClient(
            [
                """
                {
                  "criterion_key":"technical",
                  "suggested_score":4,
                  "rationale":"Candidate political views indicate strong collaboration.",
                  "additional_evidence_quotes":[{"quote":"politics discussion","related_to":"culture_fit"}],
                  "confidence":0.76
                }
                """
            ]),
            scoringMode: "llm_assisted",
            llmEnabled: true,
            llmScoringEnabled: true);

        var response = await service.AutoScoreAsync(sessionId, Guid.NewGuid(), force: true);

        Assert.Contains(AuditActions.LlmScoreEnrichGuardrailBlock, response.LlmEnrichmentEvents);
        Assert.DoesNotContain(response.Latest, x => x.EnrichmentStatus == InterviewScoreEnrichmentStatus.Enriched);
        Assert.Contains(response.Latest, x => x.EnrichmentStatus == InterviewScoreEnrichmentStatus.Failed || x.Status == InterviewCriterionScoreStatus.InsufficientEvidence);
    }

    private static InterviewScoringService BuildScoringService(
        AppDbContext db,
        ILLMClient llmClient,
        string scoringMode,
        bool llmEnabled,
        bool llmScoringEnabled)
    {
        var rubricService = new RubricService(db);
        var enrichment = new InterviewScoringEnrichmentService(
            llmClient,
            Options.Create(new LlmOptions
            {
                Enabled = llmEnabled,
                Model = "mock-model"
            }));

        return new InterviewScoringService(
            db,
            rubricService,
            enrichment,
            Options.Create(new ScoringOptions
            {
                Mode = scoringMode,
                LlmScoringEnabled = llmScoringEnabled
            }),
            Options.Create(new LlmOptions
            {
                Enabled = llmEnabled,
                Model = "mock-model"
            }),
            new MemoryCache(new MemoryCacheOptions()));
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        var root = new InMemoryDatabaseRoot();
        var dbName = Guid.NewGuid().ToString("N");
        services.AddSingleton(root);
        services.AddDbContext<AppDbContext>((sp, opts) => opts.UseInMemoryDatabase(dbName, sp.GetRequiredService<InMemoryDatabaseRoot>()));
        return services.BuildServiceProvider();
    }

    private static async Task<Guid> SeedCompletedSessionAsync(AppDbContext db, string candidateMessage)
    {
        var stage = new PipelineStage
        {
            Id = Guid.NewGuid(),
            Name = "Applied",
            Order = 1,
            IsTerminal = false
        };
        var candidate = new Candidate
        {
            Id = Guid.NewGuid(),
            FullName = "Test Candidate",
            CreatedAt = DateTime.UtcNow
        };
        var job = new JobPosting
        {
            Id = Guid.NewGuid(),
            Title = "Backend Engineer",
            Description = "Role",
            RequiredSkillsJson = """["SQL","Docker"]""",
            NiceToHaveSkillsJson = "[]",
            CreatedByUserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
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
            Status = InterviewSessionStatus.Completed,
            CompletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        db.PipelineStages.Add(stage);
        db.Candidates.Add(candidate);
        db.JobPostings.Add(job);
        db.Applications.Add(app);
        db.InterviewSessions.Add(session);
        db.InterviewMessages.Add(new InterviewMessage
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Role = "candidate",
            Content = candidateMessage,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return session.Id;
    }

    private sealed class QueueLlmClient : ILLMClient
    {
        private readonly Queue<string> _responses;

        public QueueLlmClient(IEnumerable<string> responses)
        {
            _responses = new Queue<string>(responses);
        }

        public Task<string> GenerateStructuredAsync(string systemPrompt, string userPrompt, string jsonSchema, CancellationToken ct = default)
        {
            if (_responses.Count == 0)
            {
                throw new LlmProviderException("No mock LLM response configured.");
            }

            return Task.FromResult(_responses.Dequeue());
        }
    }
}
