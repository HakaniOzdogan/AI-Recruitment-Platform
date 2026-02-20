using IkOtomasyon.Api.Data;
using IkOtomasyon.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace IkOtomasyon.Api.Authorization;

public class ResourceAuthorizationHandler : AuthorizationHandler<ResourceAuthorizationRequirement, IAuthorizationResource>
{
    private readonly AppDbContext _db;
    private readonly AuthorizationRuntimeState _authorizationState;

    public ResourceAuthorizationHandler(AppDbContext db, AuthorizationRuntimeState authorizationState)
    {
        _db = db;
        _authorizationState = authorizationState;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceAuthorizationRequirement requirement,
        IAuthorizationResource resource)
    {
        if (!_authorizationState.Enforced)
        {
            context.Succeed(requirement);
            return;
        }

        var userId = context.User.TryGetUserId();
        if (userId is null)
        {
            return;
        }

        var tenantId = context.User.TryGetTenantId();
        if (context.User.HasAnyRole(RoleKeys.Admin))
        {
            var hasResource = await ResourceExistsInTenantAsync(resource, tenantId);
            if (hasResource)
            {
                context.Succeed(requirement);
            }

            return;
        }

        var allowed = (resource, requirement.PolicyName) switch
        {
            (JobAuthorizationResource x, ResourcePolicies.CanReadJob) => await CanReadJobAsync(context, userId.Value, tenantId, x.JobId),
            (JobAuthorizationResource x, ResourcePolicies.CanManageJob) => await CanManageJobAsync(context, userId.Value, tenantId, x.JobId),
            (CandidateAuthorizationResource x, ResourcePolicies.CanReadCandidate) => await CanReadCandidateAsync(context, userId.Value, tenantId, x.CandidateId),
            (CandidateAuthorizationResource x, ResourcePolicies.CanEditCandidate) => await CanEditCandidateAsync(context, userId.Value, tenantId, x.CandidateId),
            (ApplyAuthorizationResource x, ResourcePolicies.CanApplyApplication) => await CanApplyApplicationAsync(context, userId.Value, tenantId, x.JobId, x.CandidateId),
            (ApplicationAuthorizationResource x, ResourcePolicies.CanReadApplication) => await CanReadApplicationAsync(context, userId.Value, tenantId, x.ApplicationId),
            (ApplicationAuthorizationResource x, ResourcePolicies.CanManageApplication) => await CanManageApplicationAsync(context, userId.Value, tenantId, x.ApplicationId),
            (InterviewAuthorizationResource x, ResourcePolicies.CanAccessInterview) => await CanAccessInterviewAsync(context, userId.Value, tenantId, x.SessionId),
            (InterviewAuthorizationResource x, ResourcePolicies.CanOverrideScorecard) => await CanOverrideScorecardBySessionAsync(context, userId.Value, tenantId, x.SessionId),
            (ScorecardAuthorizationResource x, ResourcePolicies.CanAccessScorecard) => await CanAccessScorecardAsync(context, userId.Value, tenantId, x.ScorecardId),
            (ScorecardAuthorizationResource x, ResourcePolicies.CanOverrideScorecard) => await CanOverrideScorecardAsync(context, userId.Value, tenantId, x.ScorecardId),
            (AiEvaluationAuthorizationResource x, ResourcePolicies.CanRunAiEvaluation) => await CanRunAiEvaluationAsync(context, userId.Value, tenantId, x.JobId, x.CandidateId),
            (CvDocumentAuthorizationResource x, ResourcePolicies.CanParseCvDocument) => await CanParseCvDocumentAsync(context, userId.Value, tenantId, x.CvDocumentId),
            _ => false
        };

        if (allowed)
        {
            context.Succeed(requirement);
        }
    }

    private async Task<bool> CanReadJobAsync(AuthorizationHandlerContext context, Guid userId, Guid? tenantId, Guid jobId)
    {
        var job = await _db.JobPostings
            .AsNoTracking()
            .Where(x => x.Id == jobId)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.CreatedByUserId,
                x.AssignedManagerUserId
            })
            .FirstOrDefaultAsync();

        if (job is null || !TenantMatches(job.TenantId, tenantId))
        {
            return false;
        }

        if (context.User.IsRecruiter())
        {
            return true;
        }

        if (context.User.HasAnyRole(RoleKeys.HiringManager))
        {
            return job.AssignedManagerUserId == userId || job.CreatedByUserId == userId;
        }

        if (context.User.IsApplicant())
        {
            return true;
        }

        return false;
    }

    private async Task<bool> CanManageJobAsync(AuthorizationHandlerContext context, Guid userId, Guid? tenantId, Guid jobId)
    {
        var job = await _db.JobPostings
            .AsNoTracking()
            .Where(x => x.Id == jobId)
            .Select(x => new
            {
                x.TenantId,
                x.CreatedByUserId,
                x.AssignedManagerUserId
            })
            .FirstOrDefaultAsync();

        if (job is null || !TenantMatches(job.TenantId, tenantId))
        {
            return false;
        }

        if (context.User.IsRecruiter())
        {
            return true;
        }

        if (context.User.HasAnyRole(RoleKeys.HiringManager))
        {
            return job.AssignedManagerUserId == userId || job.CreatedByUserId == userId;
        }

        return false;
    }

    private async Task<bool> CanReadCandidateAsync(AuthorizationHandlerContext context, Guid userId, Guid? tenantId, Guid candidateId)
    {
        var candidate = await _db.Candidates
            .AsNoTracking()
            .Where(x => x.Id == candidateId)
            .Select(x => new
            {
                x.TenantId,
                x.OwnerUserId,
                x.CreatedByUserId
            })
            .FirstOrDefaultAsync();

        if (candidate is null || !TenantMatches(candidate.TenantId, tenantId))
        {
            return false;
        }

        if (context.User.IsRecruiter())
        {
            return true;
        }

        if (context.User.IsApplicant())
        {
            return candidate.OwnerUserId == userId;
        }

        if (context.User.HasAnyRole(RoleKeys.HiringManager))
        {
            var query = _db.Applications
                .AsNoTracking()
                .Where(x => x.CandidateId == candidateId)
                .Join(
                    _db.JobPostings.AsNoTracking(),
                    app => app.JobId,
                    job => job.Id,
                    (app, job) => new { app, job });

            if (tenantId is Guid scopedTenant)
            {
                query = query.Where(x => x.app.TenantId == scopedTenant && x.job.TenantId == scopedTenant);
            }

            return await query.AnyAsync(x => x.job.AssignedManagerUserId == userId || x.job.CreatedByUserId == userId);
        }

        if (context.User.HasAnyRole(RoleKeys.Interviewer))
        {
            var query = _db.InterviewSessions
                .AsNoTracking()
                .Include(x => x.Application)
                .Where(x =>
                    x.Application != null &&
                    x.Application.CandidateId == candidateId &&
                    x.InterviewerUserId == userId);

            if (tenantId is Guid scopedTenant)
            {
                query = query.Where(x => x.TenantId == scopedTenant);
            }

            return await query.AnyAsync();
        }

        return candidate.CreatedByUserId == userId;
    }

    private async Task<bool> CanEditCandidateAsync(AuthorizationHandlerContext context, Guid userId, Guid? tenantId, Guid candidateId)
    {
        var candidate = await _db.Candidates
            .AsNoTracking()
            .Where(x => x.Id == candidateId)
            .Select(x => new
            {
                x.TenantId,
                x.OwnerUserId,
                x.CreatedByUserId
            })
            .FirstOrDefaultAsync();

        if (candidate is null || !TenantMatches(candidate.TenantId, tenantId))
        {
            return false;
        }

        if (context.User.IsRecruiter())
        {
            return true;
        }

        if (context.User.IsApplicant())
        {
            return candidate.OwnerUserId == userId;
        }

        return candidate.CreatedByUserId == userId;
    }

    private async Task<bool> CanApplyApplicationAsync(
        AuthorizationHandlerContext context,
        Guid userId,
        Guid? tenantId,
        Guid jobId,
        Guid candidateId)
    {
        var joined = await _db.JobPostings
            .AsNoTracking()
            .Where(x => x.Id == jobId)
            .Join(
                _db.Candidates.AsNoTracking().Where(x => x.Id == candidateId),
                job => 1,
                candidate => 1,
                (job, candidate) => new
                {
                    JobTenantId = job.TenantId,
                    CandidateTenantId = candidate.TenantId,
                    candidate.OwnerUserId
                })
            .FirstOrDefaultAsync();

        if (joined is null)
        {
            return false;
        }

        if (!TenantMatches(joined.JobTenantId, tenantId) || !TenantMatches(joined.CandidateTenantId, tenantId))
        {
            return false;
        }

        if (context.User.IsRecruiter())
        {
            return true;
        }

        if (context.User.IsApplicant())
        {
            return joined.OwnerUserId == userId;
        }

        return false;
    }

    private async Task<bool> CanReadApplicationAsync(AuthorizationHandlerContext context, Guid userId, Guid? tenantId, Guid applicationId)
    {
        var app = await _db.Applications
            .AsNoTracking()
            .Include(x => x.Job)
            .Include(x => x.Candidate)
            .FirstOrDefaultAsync(x => x.Id == applicationId);

        if (app is null || app.Job is null || app.Candidate is null)
        {
            return false;
        }

        if (!TenantMatches(app.TenantId, tenantId) || !TenantMatches(app.Job.TenantId, tenantId) || !TenantMatches(app.Candidate.TenantId, tenantId))
        {
            return false;
        }

        if (context.User.IsRecruiter())
        {
            return true;
        }

        if (context.User.IsApplicant())
        {
            return app.Candidate.OwnerUserId == userId;
        }

        if (context.User.HasAnyRole(RoleKeys.HiringManager))
        {
            return app.Job.AssignedManagerUserId == userId || app.Job.CreatedByUserId == userId;
        }

        if (context.User.HasAnyRole(RoleKeys.Interviewer))
        {
            var query = _db.InterviewSessions
                .AsNoTracking()
                .Where(x =>
                    x.ApplicationId == applicationId &&
                    x.InterviewerUserId == userId);

            if (tenantId is Guid scopedTenant)
            {
                query = query.Where(x => x.TenantId == scopedTenant);
            }

            return await query.AnyAsync();
        }

        return false;
    }

    private async Task<bool> CanManageApplicationAsync(AuthorizationHandlerContext context, Guid userId, Guid? tenantId, Guid applicationId)
    {
        var app = await _db.Applications
            .AsNoTracking()
            .Include(x => x.Job)
            .FirstOrDefaultAsync(x => x.Id == applicationId);

        if (app is null || app.Job is null)
        {
            return false;
        }

        if (!TenantMatches(app.TenantId, tenantId) || !TenantMatches(app.Job.TenantId, tenantId))
        {
            return false;
        }

        if (context.User.IsRecruiter())
        {
            return true;
        }

        if (context.User.HasAnyRole(RoleKeys.HiringManager))
        {
            return app.Job.AssignedManagerUserId == userId || app.Job.CreatedByUserId == userId;
        }

        return false;
    }

    private async Task<bool> CanAccessInterviewAsync(AuthorizationHandlerContext context, Guid userId, Guid? tenantId, Guid sessionId)
    {
        var session = await _db.InterviewSessions
            .AsNoTracking()
            .Include(x => x.Application)
            .ThenInclude(x => x!.Job)
            .Include(x => x.Application)
            .ThenInclude(x => x!.Candidate)
            .FirstOrDefaultAsync(x => x.Id == sessionId);

        if (session is null || session.Application?.Job is null || session.Application.Candidate is null)
        {
            return false;
        }

        if (!TenantMatches(session.TenantId, tenantId)
            || !TenantMatches(session.Application.TenantId, tenantId)
            || !TenantMatches(session.Application.Job.TenantId, tenantId)
            || !TenantMatches(session.Application.Candidate.TenantId, tenantId))
        {
            return false;
        }

        if (context.User.IsRecruiter())
        {
            return true;
        }

        if (context.User.IsApplicant())
        {
            return session.ApplicantUserId == userId || session.Application.Candidate.OwnerUserId == userId;
        }

        if (context.User.HasAnyRole(RoleKeys.Interviewer))
        {
            return session.InterviewerUserId == userId;
        }

        if (context.User.HasAnyRole(RoleKeys.HiringManager))
        {
            return session.Application.Job.AssignedManagerUserId == userId || session.Application.Job.CreatedByUserId == userId;
        }

        return false;
    }

    private async Task<bool> CanAccessScorecardAsync(AuthorizationHandlerContext context, Guid userId, Guid? tenantId, Guid scorecardId)
    {
        var scorecard = await _db.InterviewScorecards
            .AsNoTracking()
            .Include(x => x.Session)
            .ThenInclude(x => x!.Application)
            .ThenInclude(x => x!.Job)
            .Include(x => x.Session)
            .ThenInclude(x => x!.Application)
            .ThenInclude(x => x!.Candidate)
            .FirstOrDefaultAsync(x => x.Id == scorecardId);

        if (scorecard?.Session?.Application?.Job is null || scorecard.Session.Application.Candidate is null)
        {
            return false;
        }

        if (!TenantMatches(scorecard.TenantId, tenantId)
            || !TenantMatches(scorecard.Session.TenantId, tenantId)
            || !TenantMatches(scorecard.Session.Application.TenantId, tenantId)
            || !TenantMatches(scorecard.Session.Application.Job.TenantId, tenantId)
            || !TenantMatches(scorecard.Session.Application.Candidate.TenantId, tenantId))
        {
            return false;
        }

        if (context.User.IsRecruiter())
        {
            return true;
        }

        if (context.User.HasAnyRole(RoleKeys.HiringManager))
        {
            return scorecard.Session.Application.Job.AssignedManagerUserId == userId
                || scorecard.Session.Application.Job.CreatedByUserId == userId;
        }

        if (context.User.HasAnyRole(RoleKeys.Interviewer))
        {
            return scorecard.Session.InterviewerUserId == userId;
        }

        if (context.User.IsApplicant())
        {
            return scorecard.Session.ApplicantUserId == userId || scorecard.Session.Application.Candidate.OwnerUserId == userId;
        }

        return false;
    }

    private async Task<bool> CanOverrideScorecardAsync(AuthorizationHandlerContext context, Guid userId, Guid? tenantId, Guid scorecardId)
    {
        if (context.User.HasAnyRole(RoleKeys.Interviewer))
        {
            return false;
        }

        return await CanAccessScorecardAsync(context, userId, tenantId, scorecardId);
    }

    private async Task<bool> CanOverrideScorecardBySessionAsync(AuthorizationHandlerContext context, Guid userId, Guid? tenantId, Guid sessionId)
    {
        if (context.User.HasAnyRole(RoleKeys.Interviewer))
        {
            return false;
        }

        return await CanAccessInterviewAsync(context, userId, tenantId, sessionId);
    }

    private async Task<bool> CanRunAiEvaluationAsync(
        AuthorizationHandlerContext context,
        Guid userId,
        Guid? tenantId,
        Guid jobId,
        Guid candidateId)
    {
        var canReadJob = await CanReadJobAsync(context, userId, tenantId, jobId);
        if (!canReadJob)
        {
            return false;
        }

        return await CanReadCandidateAsync(context, userId, tenantId, candidateId);
    }

    private async Task<bool> CanParseCvDocumentAsync(AuthorizationHandlerContext context, Guid userId, Guid? tenantId, Guid cvDocumentId)
    {
        var doc = await _db.CvDocuments
            .AsNoTracking()
            .Include(x => x.Candidate)
            .FirstOrDefaultAsync(x => x.Id == cvDocumentId);

        if (doc?.Candidate is null)
        {
            return false;
        }

        if (!TenantMatches(doc.TenantId, tenantId) || !TenantMatches(doc.Candidate.TenantId, tenantId))
        {
            return false;
        }

        if (context.User.IsRecruiter())
        {
            return true;
        }

        if (context.User.IsApplicant())
        {
            return doc.Candidate.OwnerUserId == userId;
        }

        return doc.UploadedByUserId == userId;
    }

    private async Task<bool> ResourceExistsInTenantAsync(IAuthorizationResource resource, Guid? tenantId)
    {
        return resource switch
        {
            JobAuthorizationResource x => await ResourceExistsAsync(_db.JobPostings.AsNoTracking().Where(y => y.Id == x.JobId).Select(y => y.TenantId), tenantId),
            CandidateAuthorizationResource x => await ResourceExistsAsync(_db.Candidates.AsNoTracking().Where(y => y.Id == x.CandidateId).Select(y => y.TenantId), tenantId),
            ApplicationAuthorizationResource x => await ResourceExistsAsync(_db.Applications.AsNoTracking().Where(y => y.Id == x.ApplicationId).Select(y => y.TenantId), tenantId),
            ApplyAuthorizationResource x => await ResourceExistsAsync(_db.JobPostings.AsNoTracking().Where(y => y.Id == x.JobId).Select(y => y.TenantId), tenantId)
                && await ResourceExistsAsync(_db.Candidates.AsNoTracking().Where(y => y.Id == x.CandidateId).Select(y => y.TenantId), tenantId),
            InterviewAuthorizationResource x => await ResourceExistsAsync(_db.InterviewSessions.AsNoTracking().Where(y => y.Id == x.SessionId).Select(y => y.TenantId), tenantId),
            ScorecardAuthorizationResource x => await ResourceExistsAsync(_db.InterviewScorecards.AsNoTracking().Where(y => y.Id == x.ScorecardId).Select(y => y.TenantId), tenantId),
            AiEvaluationAuthorizationResource x => await ResourceExistsAsync(_db.JobPostings.AsNoTracking().Where(y => y.Id == x.JobId).Select(y => y.TenantId), tenantId)
                && await ResourceExistsAsync(_db.Candidates.AsNoTracking().Where(y => y.Id == x.CandidateId).Select(y => y.TenantId), tenantId),
            CvDocumentAuthorizationResource x => await ResourceExistsAsync(_db.CvDocuments.AsNoTracking().Where(y => y.Id == x.CvDocumentId).Select(y => y.TenantId), tenantId),
            _ => false
        };
    }

    private static async Task<bool> ResourceExistsAsync(IQueryable<Guid?> tenantQuery, Guid? tenantId)
    {
        if (tenantId is null)
        {
            return await tenantQuery.AnyAsync();
        }

        var scopedTenant = tenantId.Value;
        return await tenantQuery.AnyAsync(x => x == scopedTenant);
    }

    private static bool TenantMatches(Guid? resourceTenantId, Guid? userTenantId)
    {
        if (userTenantId is null)
        {
            return true;
        }

        return resourceTenantId == userTenantId;
    }
}
