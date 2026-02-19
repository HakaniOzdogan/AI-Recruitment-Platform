using IkOtomasyon.Api.Contracts;
using IkOtomasyon.Api.Data;
using IkOtomasyon.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text.Json;

namespace IkOtomasyon.Api.Services;

public class AtsWorkflowService
{
    private readonly AppDbContext _db;

    public AtsWorkflowService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<JobPosting> CreateJobAsync(JobCreateRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var job = new JobPosting
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Department = request.Department?.Trim(),
            Location = request.Location?.Trim(),
            EmploymentType = request.EmploymentType?.Trim(),
            Description = request.Description.Trim(),
            RequiredSkillsJson = JsonSerializer.Serialize(SanitizeSkills(request.RequiredSkills)),
            NiceToHaveSkillsJson = JsonSerializer.Serialize(SanitizeSkills(request.NiceToHaveSkills)),
            MinExperienceMonths = request.MinExperienceMonths,
            Status = JobPostingStatus.Draft,
            CreatedByUserId = actorUserId,
            CreatedAt = DateTime.UtcNow
        };

        _db.JobPostings.Add(job);
        await _db.SaveChangesAsync(ct);
        return job;
    }

    public async Task<JobPosting> UpdateJobAsync(Guid jobId, JobUpdateRequest request, CancellationToken ct = default)
    {
        var job = await GetJobOrThrowAsync(jobId, ct);

        if (request.Title is not null)
        {
            job.Title = request.Title.Trim();
        }

        if (request.Department is not null)
        {
            job.Department = request.Department.Trim();
        }

        if (request.Location is not null)
        {
            job.Location = request.Location.Trim();
        }

        if (request.EmploymentType is not null)
        {
            job.EmploymentType = request.EmploymentType.Trim();
        }

        if (request.Description is not null)
        {
            job.Description = request.Description.Trim();
        }

        if (request.RequiredSkills is not null)
        {
            job.RequiredSkillsJson = JsonSerializer.Serialize(SanitizeSkills(request.RequiredSkills));
        }

        if (request.NiceToHaveSkills is not null)
        {
            job.NiceToHaveSkillsJson = JsonSerializer.Serialize(SanitizeSkills(request.NiceToHaveSkills));
        }

        if (request.MinExperienceMonths is not null)
        {
            job.MinExperienceMonths = request.MinExperienceMonths;
        }

        await _db.SaveChangesAsync(ct);
        return job;
    }

    public async Task<JobPosting> UpdateCompetencyWeightsAsync(Guid jobId, Dictionary<string, double> weights, CancellationToken ct = default)
    {
        var job = await GetJobOrThrowAsync(jobId, ct);
        var normalized = weights
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .ToDictionary(
                x => x.Key.Trim().ToLowerInvariant(),
                x => Math.Round(x.Value, 4),
                StringComparer.OrdinalIgnoreCase);

        job.CompetencyWeightsJson = JsonSerializer.Serialize(normalized);
        await _db.SaveChangesAsync(ct);
        return job;
    }

    public async Task<IReadOnlyCollection<JobSkillWeight>> UpsertSkillWeightsAsync(Guid jobId, IReadOnlyCollection<JobSkillWeightRequest> skills, CancellationToken ct = default)
    {
        await GetJobOrThrowAsync(jobId, ct);

        var existing = await _db.JobSkillWeights
            .Where(x => x.JobId == jobId)
            .ToListAsync(ct);

        var byNormalized = existing.ToDictionary(x => x.SkillNameNormalized, StringComparer.OrdinalIgnoreCase);
        foreach (var skill in skills)
        {
            var normalized = SkillNormalization.Normalize(skill.Name);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                continue;
            }

            if (!byNormalized.TryGetValue(normalized, out var entity))
            {
                entity = new JobSkillWeight
                {
                    Id = Guid.NewGuid(),
                    JobId = jobId,
                    SkillNameNormalized = normalized,
                    CreatedAt = DateTime.UtcNow
                };
                _db.JobSkillWeights.Add(entity);
                byNormalized[normalized] = entity;
            }

            entity.Weight = skill.Weight;
            entity.IsRequired = skill.IsRequired;
        }

        await _db.SaveChangesAsync(ct);
        return byNormalized.Values.OrderBy(x => x.SkillNameNormalized).ToList();
    }

    public async Task<(Dictionary<string, double> Competencies, IReadOnlyCollection<JobSkillWeight> Skills)> GetJobWeightsAsync(Guid jobId, CancellationToken ct = default)
    {
        var job = await GetJobOrThrowAsync(jobId, ct);
        var skills = await _db.JobSkillWeights
            .AsNoTracking()
            .Where(x => x.JobId == jobId)
            .OrderBy(x => x.SkillNameNormalized)
            .ToListAsync(ct);

        return (ParseCompetencyWeights(job.CompetencyWeightsJson), skills);
    }

    public async Task<JobPosting> PublishJobAsync(Guid jobId, CancellationToken ct = default)
    {
        var job = await GetJobOrThrowAsync(jobId, ct);
        if (job.Status != JobPostingStatus.Draft)
        {
            throw new InvalidStateTransitionException("Job can only be published from Draft state.");
        }

        job.Status = JobPostingStatus.Published;
        job.PublishedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return job;
    }

    public async Task<JobPosting> CloseJobAsync(Guid jobId, CancellationToken ct = default)
    {
        var job = await GetJobOrThrowAsync(jobId, ct);
        if (job.Status is not (JobPostingStatus.Draft or JobPostingStatus.Published))
        {
            throw new InvalidStateTransitionException("Job can only be closed from Draft or Published state.");
        }

        job.Status = JobPostingStatus.Closed;
        job.ClosedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return job;
    }

    public async Task<Candidate> CreateCandidateAsync(CandidateCreateRequest request, CancellationToken ct = default)
    {
        var candidate = new Candidate
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = request.Email?.Trim(),
            Phone = request.Phone?.Trim(),
            Source = request.Source?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Candidates.Add(candidate);
        await _db.SaveChangesAsync(ct);
        return candidate;
    }

    public async Task<Candidate> UpdateCandidateAsync(Guid candidateId, CandidateUpdateRequest request, CancellationToken ct = default)
    {
        var candidate = await _db.Candidates.FirstOrDefaultAsync(x => x.Id == candidateId, ct)
            ?? throw new NotFoundException("Candidate not found.");

        if (request.FullName is not null)
        {
            candidate.FullName = request.FullName.Trim();
        }

        if (request.Email is not null)
        {
            candidate.Email = request.Email.Trim();
        }

        if (request.Phone is not null)
        {
            candidate.Phone = request.Phone.Trim();
        }

        if (request.Source is not null)
        {
            candidate.Source = request.Source.Trim();
        }

        await _db.SaveChangesAsync(ct);
        return candidate;
    }

    public async Task<Application> CreateApplicationAsync(ApplicationCreateRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var jobExists = await _db.JobPostings.AnyAsync(x => x.Id == request.JobId, ct);
        if (!jobExists)
        {
            throw new NotFoundException("Job not found.");
        }

        var candidateExists = await _db.Candidates.AnyAsync(x => x.Id == request.CandidateId, ct);
        if (!candidateExists)
        {
            throw new NotFoundException("Candidate not found.");
        }

        var stage = await _db.PipelineStages.FirstOrDefaultAsync(x => x.Name == "Applied", ct)
            ?? throw new InvalidOperationException("Pipeline stage 'Applied' is not seeded.");

        var entity = new Application
        {
            Id = Guid.NewGuid(),
            JobId = request.JobId,
            CandidateId = request.CandidateId,
            StageId = stage.Id,
            Status = ResolveStatusFromStage(stage),
            AppliedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastUpdatedByUserId = actorUserId
        };

        _db.Applications.Add(entity);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("Candidate already applied to this job.");
        }

        return await GetApplicationOrThrowAsync(entity.Id, ct);
    }

    public async Task<Application> ChangeApplicationStageAsync(Guid applicationId, Guid stageId, Guid actorUserId, CancellationToken ct = default)
    {
        var entity = await _db.Applications
            .Include(x => x.Stage)
            .FirstOrDefaultAsync(x => x.Id == applicationId, ct)
            ?? throw new NotFoundException("Application not found.");

        var stage = await _db.PipelineStages.FirstOrDefaultAsync(x => x.Id == stageId, ct)
            ?? throw new NotFoundException("Stage not found.");

        entity.StageId = stage.Id;
        entity.Status = ResolveStatusFromStage(stage);
        entity.UpdatedAt = DateTime.UtcNow;
        entity.LastUpdatedByUserId = actorUserId;

        await _db.SaveChangesAsync(ct);
        return await GetApplicationOrThrowAsync(entity.Id, ct);
    }

    public async Task<InterviewSession> StartInterviewAsync(Guid applicationId, CancellationToken ct = default)
    {
        var application = await _db.Applications
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == applicationId, ct)
            ?? throw new NotFoundException("Application not found.");

        var existing = await _db.InterviewSessions
            .FirstOrDefaultAsync(
                x => x.ApplicationId == applicationId &&
                     (x.Status == InterviewSessionStatus.Scheduled || x.Status == InterviewSessionStatus.InProgress),
                ct);

        if (existing is not null)
        {
            return existing;
        }

        var session = new InterviewSession
        {
            Id = Guid.NewGuid(),
            ApplicationId = application.Id,
            Status = InterviewSessionStatus.InProgress,
            AiMode = "ASSIST",
            CreatedAt = DateTime.UtcNow
        };

        _db.InterviewSessions.Add(session);
        await _db.SaveChangesAsync(ct);
        return session;
    }

    private async Task<JobPosting> GetJobOrThrowAsync(Guid id, CancellationToken ct)
    {
        return await _db.JobPostings.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Job not found.");
    }

    private async Task<Application> GetApplicationOrThrowAsync(Guid id, CancellationToken ct)
    {
        return await _db.Applications
            .Include(x => x.Job)
            .Include(x => x.Candidate)
            .Include(x => x.Stage)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Application not found.");
    }

    private static ApplicationStatus ResolveStatusFromStage(PipelineStage stage)
    {
        if (!stage.IsTerminal)
        {
            return ApplicationStatus.Active;
        }

        return stage.Name.Equals("Hired", StringComparison.OrdinalIgnoreCase)
            ? ApplicationStatus.Hired
            : ApplicationStatus.Rejected;
    }

    private static List<string> SanitizeSkills(List<string>? skills)
    {
        return (skills ?? new List<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(SkillNormalization.Normalize)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static Dictionary<string, double> ParseCompetencyWeights(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, double>>(json) ?? new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
