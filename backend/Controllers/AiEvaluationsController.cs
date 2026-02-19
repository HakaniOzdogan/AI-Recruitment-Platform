using IkOtomasyon.Api.Authorization;
using IkOtomasyon.Api.Contracts;
using IkOtomasyon.Api.Data;
using IkOtomasyon.Api.Entities;
using IkOtomasyon.Api.Middleware;
using IkOtomasyon.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IkOtomasyon.Api.Controllers;

[ApiController]
[Authorize]
public class AiEvaluationsController : ControllerBase
{
    private readonly AiEvaluationService _service;
    private readonly PermissionService _permissionService;
    private readonly AppDbContext _db;

    public AiEvaluationsController(AiEvaluationService service, PermissionService permissionService, AppDbContext db)
    {
        _service = service;
        _permissionService = permissionService;
        _db = db;
    }

    [HttpPost("jobs/{jobId:guid}/candidates/{candidateId:guid}/ai-evaluate")]
    public async Task<ActionResult<AiEvaluationResponse>> Evaluate(Guid jobId, Guid candidateId, [FromQuery] bool force = false, CancellationToken ct = default)
    {
        var userId = User.TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (!await HasPermissionAsync(userId.Value, ct))
        {
            return Forbid();
        }

        try
        {
            var report = await _service.EvaluateAsync(jobId, candidateId, userId.Value, force, ct);
            if (force)
            {
                await WriteAuditAsync(userId.Value, jobId, candidateId, ct);
            }

            return Ok(ToResponse(report));
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

    [HttpGet("jobs/{jobId:guid}/candidates/{candidateId:guid}/ai-evaluate/latest")]
    public async Task<ActionResult<AiEvaluationResponse>> Latest(Guid jobId, Guid candidateId, CancellationToken ct = default)
    {
        var userId = User.TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (!await HasPermissionAsync(userId.Value, ct))
        {
            return Forbid();
        }

        try
        {
            var report = await _service.GetLatestAsync(jobId, candidateId, ct);
            return Ok(ToResponse(report));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("jobs/{jobId:guid}/candidates/{candidateId:guid}/ai-evaluate/history")]
    public async Task<ActionResult<IEnumerable<AiEvaluationResponse>>> History(
        Guid jobId,
        Guid candidateId,
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0,
        CancellationToken ct = default)
    {
        var userId = User.TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (!await HasPermissionAsync(userId.Value, ct))
        {
            return Forbid();
        }

        var history = await _service.GetHistoryAsync(jobId, candidateId, limit, offset, ct);
        return Ok(history.Select(ToResponse));
    }

    [HttpGet("applications/{applicationId:guid}/ai-evaluate/latest")]
    public async Task<ActionResult<AiEvaluationResponse>> LatestByApplication(Guid applicationId, CancellationToken ct = default)
    {
        var userId = User.TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (!await HasPermissionAsync(userId.Value, ct))
        {
            return Forbid();
        }

        try
        {
            var report = await _service.GetLatestByApplicationAsync(applicationId, ct);
            return Ok(ToResponse(report));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private async Task<bool> HasPermissionAsync(Guid userId, CancellationToken ct)
    {
        return await _permissionService.HasPermissionAsync(userId, PermissionKeys.JobCreate, ct)
            || await _permissionService.HasPermissionAsync(userId, PermissionKeys.CandidateManage, ct);
    }

    private static AiEvaluationResponse ToResponse(AiEvaluationReport report)
    {
        return new AiEvaluationResponse(
            report.Id,
            report.JobId,
            report.CandidateId,
            report.ApplicationId,
            report.ModelName,
            report.OverallRecommendation,
            report.StrengthsJson,
            report.RisksJson,
            report.VerificationQuestionsJson,
            report.EvidenceQuotesJson,
            report.CompetencyAssessmentJson,
            report.SkillAssessmentJson,
            report.Confidence,
            report.CreatedAt,
            report.Version);
    }

    private async Task WriteAuditAsync(Guid actorUserId, Guid jobId, Guid candidateId, CancellationToken ct)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Method = "POST",
            Path = HttpContext.Request.Path.Value ?? string.Empty,
            Action = AuditActions.AiEvaluateRun,
            Entity = "AiEvaluationReport",
            EntityId = $"{jobId}:{candidateId}",
            StatusCode = StatusCodes.Status200OK,
            Ip = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = HttpContext.Request.Headers.UserAgent.ToString(),
            CreatedAt = DateTime.UtcNow
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }
}
