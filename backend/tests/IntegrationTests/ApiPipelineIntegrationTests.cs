using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IkOtomasyon.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IkOtomasyon.Api.IntegrationTests;

public class ApiPipelineIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public ApiPipelineIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task JobsWeights_Get_AuthAndPermission_Works()
    {
        await _fixture.ResetDatabaseAsync();
        var (_, hrEmail, hrPassword) = await _fixture.EnsureUserAsync("HR", "hr");
        var (_, hmEmail, hmPassword) = await _fixture.EnsureUserAsync("HiringManager", "hm");

        var hrToken = await _fixture.GetJwtAsync(hrEmail, hrPassword);
        var hmToken = await _fixture.GetJwtAsync(hmEmail, hmPassword);

        var jobId = await CreateJobAsync(hrToken);

        using var noTokenClient = _fixture.Factory.CreateClient();
        var noToken = await noTokenClient.GetAsync($"/jobs/{jobId}/weights");
        Assert.Equal(HttpStatusCode.Unauthorized, noToken.StatusCode);

        using var hmClient = _fixture.Factory.CreateClient();
        IntegrationTestFixture.SetBearer(hmClient, hmToken);
        var forbidden = await hmClient.GetAsync($"/jobs/{jobId}/weights");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        using var hrClient = _fixture.Factory.CreateClient();
        IntegrationTestFixture.SetBearer(hrClient, hrToken);
        var ok = await hrClient.GetAsync($"/jobs/{jobId}/weights");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
    }

    [Fact]
    public async Task BOLA_Candidate_Read_OtherUsersRecord_Returns404()
    {
        await _fixture.ResetDatabaseAsync();
        var (_, applicantAEmail, applicantAPassword) = await _fixture.EnsureUserAsync("Applicant", "app-a");
        var (_, applicantBEmail, applicantBPassword) = await _fixture.EnsureUserAsync("Applicant", "app-b");
        var applicantAToken = await _fixture.GetJwtAsync(applicantAEmail, applicantAPassword);
        var applicantBToken = await _fixture.GetJwtAsync(applicantBEmail, applicantBPassword);

        var candidateAId = await CreateCandidateAsync(applicantAToken, "Applicant A");
        var candidateBId = await CreateCandidateAsync(applicantBToken, "Applicant B");
        Assert.NotEqual(candidateAId, candidateBId);

        using var client = _fixture.Factory.CreateClient();
        IntegrationTestFixture.SetBearer(client, applicantAToken);
        var response = await client.GetAsync($"/candidates/{candidateBId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task BOLA_Application_Read_OtherUsersRecord_Returns404()
    {
        await _fixture.ResetDatabaseAsync();
        var (_, recruiterEmail, recruiterPassword) = await _fixture.EnsureUserAsync("Recruiter", "rec");
        var (_, applicantAEmail, applicantAPassword) = await _fixture.EnsureUserAsync("Applicant", "app-a");
        var (_, applicantBEmail, applicantBPassword) = await _fixture.EnsureUserAsync("Applicant", "app-b");

        var recruiterToken = await _fixture.GetJwtAsync(recruiterEmail, recruiterPassword);
        var applicantAToken = await _fixture.GetJwtAsync(applicantAEmail, applicantAPassword);
        var applicantBToken = await _fixture.GetJwtAsync(applicantBEmail, applicantBPassword);

        var jobId = await CreateJobAsync(recruiterToken);
        var candidateAId = await CreateCandidateAsync(applicantAToken, "Applicant A");
        var candidateBId = await CreateCandidateAsync(applicantBToken, "Applicant B");
        _ = await CreateApplicationAsync(applicantAToken, jobId, candidateAId);
        var appBId = await CreateApplicationAsync(applicantBToken, jobId, candidateBId);

        using var client = _fixture.Factory.CreateClient();
        IntegrationTestFixture.SetBearer(client, applicantAToken);
        var response = await client.GetAsync($"/applications/{appBId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task BOLA_Interview_Read_OtherUsersSession_Returns404()
    {
        await _fixture.ResetDatabaseAsync();
        var (_, recruiterEmail, recruiterPassword) = await _fixture.EnsureUserAsync("Recruiter", "rec");
        var (_, applicantAEmail, applicantAPassword) = await _fixture.EnsureUserAsync("Applicant", "app-a");
        var (_, applicantBEmail, applicantBPassword) = await _fixture.EnsureUserAsync("Applicant", "app-b");

        var recruiterToken = await _fixture.GetJwtAsync(recruiterEmail, recruiterPassword);
        var applicantAToken = await _fixture.GetJwtAsync(applicantAEmail, applicantAPassword);
        var applicantBToken = await _fixture.GetJwtAsync(applicantBEmail, applicantBPassword);

        var jobId = await CreateJobAsync(recruiterToken);
        var candidateAId = await CreateCandidateAsync(applicantAToken, "Applicant A");
        var candidateBId = await CreateCandidateAsync(applicantBToken, "Applicant B");
        _ = await CreateApplicationAsync(applicantAToken, jobId, candidateAId);
        var appBId = await CreateApplicationAsync(applicantBToken, jobId, candidateBId);

        Guid sessionBId;
        using (var recruiterClient = _fixture.Factory.CreateClient())
        {
            IntegrationTestFixture.SetBearer(recruiterClient, recruiterToken);
            var start = await recruiterClient.PostAsync($"/applications/{appBId}/interviews/start", null);
            start.EnsureSuccessStatusCode();
            sessionBId = await ExtractGuidAsync(start, "sessionId");
        }

        using var applicantClient = _fixture.Factory.CreateClient();
        IntegrationTestFixture.SetBearer(applicantClient, applicantAToken);
        var forbidden = await applicantClient.GetAsync($"/interviews/{sessionBId}");
        Assert.Equal(HttpStatusCode.NotFound, forbidden.StatusCode);
    }

    [Fact]
    public async Task RoleEscalation_Applicant_CannotUpdateStage_OrReadWeights()
    {
        await _fixture.ResetDatabaseAsync();
        var (_, recruiterEmail, recruiterPassword) = await _fixture.EnsureUserAsync("Recruiter", "rec");
        var (_, applicantEmail, applicantPassword) = await _fixture.EnsureUserAsync("Applicant", "app");

        var recruiterToken = await _fixture.GetJwtAsync(recruiterEmail, recruiterPassword);
        var applicantToken = await _fixture.GetJwtAsync(applicantEmail, applicantPassword);
        var jobId = await CreateJobAsync(recruiterToken);
        var candidateId = await CreateCandidateAsync(applicantToken, "Applicant");
        var applicationId = await CreateApplicationAsync(applicantToken, jobId, candidateId);
        var stageId = await GetAnyStageIdAsync();

        using var client = _fixture.Factory.CreateClient();
        IntegrationTestFixture.SetBearer(client, applicantToken);

        var stageUpdate = await client.PatchAsync($"/applications/{applicationId}/stage", JsonContent.Create(new { stageId }));
        Assert.Equal(HttpStatusCode.Forbidden, stageUpdate.StatusCode);

        var weights = await client.GetAsync($"/jobs/{jobId}/weights");
        Assert.Equal(HttpStatusCode.Forbidden, weights.StatusCode);
    }

    [Fact]
    public async Task TenantIsolation_SameRole_DifferentTenant_CannotReadResource()
    {
        await _fixture.ResetDatabaseAsync();
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();
        var (_, recruiter1Email, recruiter1Password) = await _fixture.EnsureUserAsync("Recruiter", "tenant1-rec", tenant1);
        var (_, recruiter2Email, recruiter2Password) = await _fixture.EnsureUserAsync("Recruiter", "tenant2-rec", tenant2);

        var recruiter1Token = await _fixture.GetJwtAsync(recruiter1Email, recruiter1Password);
        var recruiter2Token = await _fixture.GetJwtAsync(recruiter2Email, recruiter2Password);

        var jobId = await CreateJobAsync(recruiter1Token);

        using var client = _fixture.Factory.CreateClient();
        IntegrationTestFixture.SetBearer(client, recruiter2Token);
        var response = await client.GetAsync($"/jobs/{jobId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UnauthorizedMutatingRequest_IsAudited_WhenEnabled()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = _fixture.Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/jobs", new
        {
            title = "Unauthorized Job",
            description = "No token request"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var logs = await _fixture.GetAuditLogsAsync("POST", "/jobs");
        Assert.Contains(logs, x => x.StatusCode == 401);
    }

    [Fact]
    public async Task Interview_MessageFlow_WritesCandidateAndSystemMessages()
    {
        await _fixture.ResetDatabaseAsync();
        var (_, hrEmail, hrPassword) = await _fixture.EnsureUserAsync("HR", "hr");
        var token = await _fixture.GetJwtAsync(hrEmail, hrPassword);
        var jobId = await CreateJobAsync(token);
        await PublishJobAsync(token, jobId);
        var candidateId = await CreateCandidateAsync(token);
        var applicationId = await CreateApplicationAsync(token, jobId, candidateId);
        var sessionId = await _fixture.CreateInterviewSessionAsync(applicationId, InterviewSessionStatus.InProgress);

        using var client = _fixture.Factory.CreateClient();
        IntegrationTestFixture.SetBearer(client, token);
        var response = await client.PostAsJsonAsync($"/interviews/{sessionId}/messages", new
        {
            content = "Kisa bir cevap."
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.TryGetProperty("systemMessageId", out _));
        Assert.True(json.RootElement.TryGetProperty("systemQuestion", out var question) && !string.IsNullOrWhiteSpace(question.GetString()));

        var messages = await _fixture.GetInterviewMessagesAsync(sessionId);
        Assert.Contains(messages, x => x.Role == "candidate");
        Assert.Contains(messages, x => x.Role == "system");
    }

    [Fact]
    public async Task AdaptiveMode_WhenLlmDisabled_Returns409_AndOffModeUsesDeterministicQuestion()
    {
        await _fixture.ResetDatabaseAsync();
        var (_, hrEmail, hrPassword) = await _fixture.EnsureUserAsync("HR", "hr");
        var token = await _fixture.GetJwtAsync(hrEmail, hrPassword);
        var jobId = await CreateJobAsync(token);
        var candidateId = await CreateCandidateAsync(token);
        var applicationId = await CreateApplicationAsync(token, jobId, candidateId);
        var sessionId = await _fixture.CreateInterviewSessionAsync(applicationId, InterviewSessionStatus.InProgress);

        using var client = _fixture.Factory.CreateClient();
        IntegrationTestFixture.SetBearer(client, token);

        var adaptive = await client.PatchAsync(
            $"/interviews/{sessionId}/mode",
            JsonContent.Create(new { aiMode = "ADAPTIVE" }));
        Assert.Equal(HttpStatusCode.Conflict, adaptive.StatusCode);

        var off = await client.PatchAsync(
            $"/interviews/{sessionId}/mode",
            JsonContent.Create(new { aiMode = "OFF" }));
        Assert.Equal(HttpStatusCode.OK, off.StatusCode);

        var message = await client.PostAsJsonAsync($"/interviews/{sessionId}/messages", new
        {
            content = "Deterministic mode testi"
        });
        Assert.Equal(HttpStatusCode.OK, message.StatusCode);

        var payload = await message.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(payload);
        Assert.False(json.RootElement.GetProperty("planGenerated").GetBoolean());
    }

    [Fact]
    public async Task AutoScore_InsufficientEvidence_UsesNullScoreAndNullableOverall()
    {
        await _fixture.ResetDatabaseAsync();
        var (_, hrEmail, hrPassword) = await _fixture.EnsureUserAsync("HR", "hr");
        var token = await _fixture.GetJwtAsync(hrEmail, hrPassword);
        var jobId = await CreateJobAsync(token);
        var candidateId = await CreateCandidateAsync(token);
        var applicationId = await CreateApplicationAsync(token, jobId, candidateId);
        var sessionId = await _fixture.CreateInterviewSessionAsync(applicationId, InterviewSessionStatus.Completed);
        await _fixture.AddInterviewMessageAsync(sessionId, "candidate", "Merhaba.");

        using var client = _fixture.Factory.CreateClient();
        IntegrationTestFixture.SetBearer(client, token);
        var response = await client.PostAsync($"/interviews/{sessionId}/score/auto", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var latest = json.RootElement.GetProperty("latest").EnumerateArray().ToList();
        Assert.NotEmpty(latest);
        Assert.Contains(latest, x =>
            x.GetProperty("status").GetString() == "INSUFFICIENT_EVIDENCE"
            && x.GetProperty("score").ValueKind == JsonValueKind.Null);
    }

    [Fact]
    public async Task HumanOverride_WhitespaceEvidence_Returns400WithFieldError()
    {
        await _fixture.ResetDatabaseAsync();
        var (_, hrEmail, hrPassword) = await _fixture.EnsureUserAsync("HR", "hr");
        var token = await _fixture.GetJwtAsync(hrEmail, hrPassword);
        var jobId = await CreateJobAsync(token);
        var candidateId = await CreateCandidateAsync(token);
        var applicationId = await CreateApplicationAsync(token, jobId, candidateId);
        var sessionId = await _fixture.CreateInterviewSessionAsync(applicationId, InterviewSessionStatus.Completed);
        await _fixture.AddInterviewMessageAsync(sessionId, "candidate", "SQL ve Docker kullandim.");

        using var client = _fixture.Factory.CreateClient();
        IntegrationTestFixture.SetBearer(client, token);

        var response = await client.PostAsJsonAsync($"/interviews/{sessionId}/score/human", new
        {
            criterionKey = "technical",
            score = 3,
            rationale = "Manual override",
            evidenceQuotes = new[]
            {
                new { quote = "   ", relatedTo = "technical" }
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.TryGetProperty("errors", out var errors));
        var hasEvidenceError = errors.EnumerateObject()
            .Any(x => x.Name.Contains("evidence", StringComparison.OrdinalIgnoreCase));
        Assert.True(hasEvidenceError);
    }

    [Fact]
    public async Task StrictRateLimit_Triggers429_OnHeavyEndpoint()
    {
        await _fixture.ResetDatabaseAsync();
        var (_, hrEmail, hrPassword) = await _fixture.EnsureUserAsync("HR", "hr");
        var token = await _fixture.GetJwtAsync(hrEmail, hrPassword);
        using var client = _fixture.Factory.CreateClient();
        IntegrationTestFixture.SetBearer(client, token);

        var cvId = Guid.NewGuid();
        HttpStatusCode? throttled = null;
        for (var i = 0; i < 12; i++)
        {
            var response = await client.PostAsync($"/cv/{cvId}/parse", null);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throttled = response.StatusCode;
                break;
            }
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, throttled);
    }

    [Fact]
    public async Task CorrelationId_IsReturned_InResponseHeader()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = _fixture.Factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.TryGetValues("X-Correlation-Id", out var values));
        Assert.False(string.IsNullOrWhiteSpace(values.FirstOrDefault()));
    }

    private async Task<Guid> CreateJobAsync(string token)
    {
        using var client = _fixture.Factory.CreateClient();
        IntegrationTestFixture.SetBearer(client, token);
        SetUniqueForwardedFor(client);
        var response = await client.PostAsJsonAsync("/jobs", new
        {
            title = $"Backend Engineer {Guid.NewGuid():N}",
            department = "Engineering",
            location = "Remote",
            employmentType = "FullTime",
            description = "Integration test job",
            requiredSkills = new[] { "SQL", "Docker" },
            niceToHaveSkills = new[] { "Kubernetes" },
            minExperienceMonths = 12
        });
        response.EnsureSuccessStatusCode();
        return await ExtractGuidAsync(response, "id");
    }

    private async Task PublishJobAsync(string token, Guid jobId)
    {
        using var client = _fixture.Factory.CreateClient();
        IntegrationTestFixture.SetBearer(client, token);
        SetUniqueForwardedFor(client);
        var response = await client.PostAsync($"/jobs/{jobId}/publish", null);
        response.EnsureSuccessStatusCode();
    }

    private async Task<Guid> CreateCandidateAsync(string token, string fullName = "Integration Candidate")
    {
        using var client = _fixture.Factory.CreateClient();
        IntegrationTestFixture.SetBearer(client, token);
        SetUniqueForwardedFor(client);
        var response = await client.PostAsJsonAsync("/candidates", new
        {
            fullName,
            email = $"candidate.{Guid.NewGuid():N}@test.local",
            phone = "5550000000",
            source = "integration"
        });
        response.EnsureSuccessStatusCode();
        return await ExtractGuidAsync(response, "id");
    }

    private async Task<Guid> CreateApplicationAsync(string token, Guid jobId, Guid candidateId)
    {
        using var client = _fixture.Factory.CreateClient();
        IntegrationTestFixture.SetBearer(client, token);
        SetUniqueForwardedFor(client);
        var response = await client.PostAsJsonAsync("/applications", new
        {
            jobId,
            candidateId
        });
        response.EnsureSuccessStatusCode();
        return await ExtractGuidAsync(response, "id");
    }

    private async Task<Guid> GetAnyStageIdAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IkOtomasyon.Api.Data.AppDbContext>();
        var stage = await db.PipelineStages.AsNoTracking().OrderBy(x => x.Order).FirstAsync();
        return stage.Id;
    }

    private static async Task<Guid> ExtractGuidAsync(HttpResponseMessage response, string property)
    {
        var payload = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(payload);
        return json.RootElement.TryGetProperty(property, out var idElement) && Guid.TryParse(idElement.GetString(), out var id)
            ? id
            : json.RootElement.GetProperty(property).GetGuid();
    }

    private static void SetUniqueForwardedFor(HttpClient client)
    {
        client.DefaultRequestHeaders.Remove("X-Forwarded-For");
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.0.{Random.Shared.Next(1, 254)}.{Random.Shared.Next(1, 254)}");
    }
}
