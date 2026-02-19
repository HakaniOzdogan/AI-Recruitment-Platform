using IkOtomasyon.Api.Authorization;
using IkOtomasyon.Api.Contracts;
using IkOtomasyon.Api.Data;
using IkOtomasyon.Api.Entities;
using IkOtomasyon.Api.Middleware;
using IkOtomasyon.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IkOtomasyon.Api.Controllers;

[ApiController]
[Authorize]
[Route("applications")]
public class ApplicationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AtsWorkflowService _workflow;
    private readonly PermissionService _permissionService;

    public ApplicationsController(AppDbContext db, AtsWorkflowService workflow, PermissionService permissionService)
    {
        _db = db;
        _workflow = workflow;
        _permissionService = permissionService;
    }

    [HttpPost]
    public async Task<ActionResult<ApplicationResponse>> Create([FromBody] ApplicationCreateRequest request, CancellationToken ct)
    {
        var userId = User.TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }
        if (!await CanCreateApplicationAsync(userId.Value, ct))
        {
            return Forbid();
        }

        try
        {
            var entity = await _workflow.CreateApplicationAsync(request, userId.Value, ct);
            HttpContext.SetAuditInfo(AuditActions.ApplicationCreate, "Application", entity.Id.ToString());
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToResponse(entity));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApplicationResponse>>> List(
        [FromQuery] Guid? jobId,
        [FromQuery] Guid? stageId,
        [FromQuery] ApplicationStatus? status,
        CancellationToken ct)
    {
        var query = _db.Applications
            .AsNoTracking()
            .Include(x => x.Job)
            .Include(x => x.Candidate)
            .Include(x => x.Stage)
            .Include(x => x.InterviewSessions)
            .AsQueryable();

        if (jobId is not null)
        {
            query = query.Where(x => x.JobId == jobId.Value);
        }

        if (stageId is not null)
        {
            query = query.Where(x => x.StageId == stageId.Value);
        }

        if (status is not null)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        var data = await query
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(ct);

        return Ok(data.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApplicationResponse>> GetById(Guid id, CancellationToken ct)
    {
        var entity = await _db.Applications
            .AsNoTracking()
            .Include(x => x.Job)
            .Include(x => x.Candidate)
            .Include(x => x.Stage)
            .Include(x => x.InterviewSessions)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(entity));
    }

    [HttpPatch("{id:guid}/stage")]
    [RequirePermission(PermissionKeys.ApplicationStageUpdate)]
    public async Task<ActionResult<ApplicationResponse>> ChangeStage(Guid id, [FromBody] StageChangeRequest request, CancellationToken ct)
    {
        var userId = User.TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            var entity = await _workflow.ChangeApplicationStageAsync(id, request.StageId, userId.Value, ct);
            HttpContext.SetAuditInfo(AuditActions.ApplicationStageChange, "Application", entity.Id.ToString());
            return Ok(ToResponse(entity));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/interviews/start")]
    public async Task<ActionResult<InterviewStartResponse>> StartInterview(Guid id, CancellationToken ct)
    {
        var userId = User.TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }
        if (!await CanCreateApplicationAsync(userId.Value, ct))
        {
            return Forbid();
        }

        try
        {
            var session = await _workflow.StartInterviewAsync(id, ct);
            HttpContext.SetAuditInfo(AuditActions.InterviewStart, "InterviewSession", session.Id.ToString());
            return Ok(new InterviewStartResponse(session.Id));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private static ApplicationResponse ToResponse(Application entity)
    {
        return new ApplicationResponse(
            entity.Id,
            entity.JobId,
            entity.Job?.Title ?? string.Empty,
            entity.CandidateId,
            entity.Candidate?.FullName ?? string.Empty,
            entity.StageId,
            entity.Stage?.Name ?? string.Empty,
            entity.Status,
            entity.AppliedAt,
            entity.UpdatedAt,
            entity.LastUpdatedByUserId,
            entity.InterviewSessions.OrderByDescending(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefault());
    }

    private async Task<bool> CanCreateApplicationAsync(Guid userId, CancellationToken ct)
    {
        return await _permissionService.HasPermissionAsync(userId, PermissionKeys.CandidateManage, ct)
            || await _permissionService.HasPermissionAsync(userId, PermissionKeys.ApplicationApply, ct);
    }
}
