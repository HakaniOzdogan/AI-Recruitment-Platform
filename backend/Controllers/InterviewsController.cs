using IkOtomasyon.Api.Authorization;
using IkOtomasyon.Api.Contracts;
using IkOtomasyon.Api.Middleware;
using IkOtomasyon.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IkOtomasyon.Api.Controllers;

[ApiController]
[Authorize]
[Route("interviews")]
public class InterviewsController : ControllerBase
{
    private readonly InterviewScoringService _scoringService;
    private readonly InterviewOrchestratorService _orchestrator;
    private readonly PermissionService _permissionService;

    public InterviewsController(
        InterviewScoringService scoringService,
        InterviewOrchestratorService orchestrator,
        PermissionService permissionService)
    {
        _scoringService = scoringService;
        _orchestrator = orchestrator;
        _permissionService = permissionService;
    }

    [HttpPatch("{sessionId:guid}/mode")]
    [RequirePermission(PermissionKeys.CandidateManage)]
    public async Task<ActionResult<object>> UpdateMode(Guid sessionId, [FromBody] InterviewModeUpdateRequest request, CancellationToken ct)
    {
        try
        {
            var session = await _orchestrator.UpdateModeAsync(sessionId, request.AiMode, ct);
            HttpContext.SetAuditInfo(AuditActions.InterviewAiModeChange, "InterviewSession", sessionId.ToString());
            return Ok(new
            {
                session.Id,
                session.AiMode,
                session.AiModelName,
                session.AiLastPlanAt
            });
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

    [HttpGet("{sessionId:guid}")]
    public async Task<ActionResult<object>> GetSession(Guid sessionId, CancellationToken ct = default)
    {
        var userId = User.TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (!await HasInterviewAccessAsync(userId.Value, ct))
        {
            return Forbid();
        }

        var session = await _orchestrator.GetSessionAsync(sessionId, ct);
        if (session is null)
        {
            return NotFound();
        }

        return Ok(new
        {
            session.Id,
            session.Status,
            session.AiMode
        });
    }

    [HttpGet("{sessionId:guid}/messages")]
    public async Task<ActionResult<IEnumerable<object>>> GetMessages(Guid sessionId, CancellationToken ct = default)
    {
        var userId = User.TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (!await HasInterviewAccessAsync(userId.Value, ct))
        {
            return Forbid();
        }

        var messages = await _orchestrator.GetMessagesAsync(sessionId, ct);
        if (messages is null)
        {
            return NotFound();
        }

        return Ok(messages.Select(x => new
        {
            x.Id,
            x.Role,
            x.Content,
            x.CreatedAt
        }));
    }

    [HttpGet("{sessionId:guid}/ai")]
    public async Task<ActionResult<InterviewAiStateResponse>> GetAiState(Guid sessionId, CancellationToken ct = default)
    {
        var userId = User.TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (!await HasInterviewAccessAsync(userId.Value, ct))
        {
            return Forbid();
        }

        try
        {
            return Ok(await _orchestrator.GetAiStateAsync(sessionId, ct));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{sessionId:guid}/messages")]
    public async Task<ActionResult<InterviewTurnResponse>> AddCandidateMessage(Guid sessionId, [FromBody] InterviewMessageRequest request, CancellationToken ct = default)
    {
        var userId = User.TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (!await HasInterviewAccessAsync(userId.Value, ct))
        {
            return Forbid();
        }

        try
        {
            var result = await _orchestrator.AddCandidateMessageAsync(sessionId, request.Content, ct);
            if (result.GuardrailBlocked)
            {
                HttpContext.AddAuditInfo(AuditActions.LlmGuardrailBlock, "InterviewSession", sessionId.ToString());
            }

            if (result.UsedFallback)
            {
                HttpContext.AddAuditInfo(AuditActions.LlmFallbackUsed, "InterviewSession", sessionId.ToString());
            }

            if (!result.UsedFallback && result.PlanGenerated)
            {
                HttpContext.AddAuditInfo(AuditActions.InterviewAiQuestion, "InterviewSession", sessionId.ToString());
            }

            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (RequestValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors });
        }
    }

    [HttpPost("{sessionId:guid}/messages/hiring")]
    public async Task<ActionResult<object>> AddHiringMessage(Guid sessionId, [FromBody] InterviewMessageRequest request, CancellationToken ct = default)
    {
        var userId = User.TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (!await HasScorePermissionAsync(userId.Value, ct))
        {
            return Forbid();
        }

        try
        {
            var message = await _orchestrator.AddInterviewerQuestionAsync(sessionId, request.Content, ct);
            return Ok(new
            {
                message.Id,
                message.Role,
                message.Content,
                message.CreatedAt
            });
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

    [HttpPost("{sessionId:guid}/messages/candidate")]
    public async Task<ActionResult<object>> AddCandidateAnswer(Guid sessionId, [FromBody] InterviewMessageRequest request, CancellationToken ct = default)
    {
        var userId = User.TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (!await HasInterviewAccessAsync(userId.Value, ct))
        {
            return Forbid();
        }

        try
        {
            var message = await _orchestrator.AddCandidateAnswerAsync(sessionId, request.Content, ct);
            return Ok(new
            {
                message.Id,
                message.Role,
                message.Content,
                message.CreatedAt
            });
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

    [HttpPost("{sessionId:guid}/score/auto")]
    public async Task<ActionResult<InterviewScoreResponse>> AutoScore(Guid sessionId, [FromQuery] bool force = false, CancellationToken ct = default)
    {
        var userId = User.TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (!await HasScorePermissionAsync(userId.Value, ct))
        {
            return Forbid();
        }

        try
        {
            var result = await _scoringService.AutoScoreAsync(sessionId, userId.Value, force, ct);
            HttpContext.SetAuditInfo(AuditActions.AutoScoreRun, "InterviewSession", sessionId.ToString());
            foreach (var auditAction in result.LlmEnrichmentEvents)
            {
                HttpContext.AddAuditInfo(auditAction, "InterviewSession", sessionId.ToString());
            }
            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (RequestValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors });
        }
    }

    [HttpPost("{sessionId:guid}/score/human")]
    public async Task<ActionResult<InterviewScoreResponse>> HumanOverride(Guid sessionId, [FromBody] HumanScoreOverrideRequest request, CancellationToken ct = default)
    {
        var userId = User.TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (!await HasScorePermissionAsync(userId.Value, ct))
        {
            return Forbid();
        }

        try
        {
            var result = await _scoringService.AddHumanOverrideAsync(sessionId, userId.Value, request, ct);
            HttpContext.SetAuditInfo(AuditActions.HumanScoreOverride, "InterviewSession", sessionId.ToString());
            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (RequestValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors });
        }
    }

    [HttpGet("{sessionId:guid}/score")]
    public async Task<ActionResult<InterviewScoreResponse>> GetScore(Guid sessionId, CancellationToken ct = default)
    {
        var userId = User.TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (!await HasScorePermissionAsync(userId.Value, ct))
        {
            return Forbid();
        }

        try
        {
            return Ok(await _scoringService.GetScoreAsync(sessionId, ct));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private async Task<bool> HasScorePermissionAsync(Guid userId, CancellationToken ct)
    {
        return await _permissionService.HasPermissionAsync(userId, PermissionKeys.CandidateManage, ct)
            || await _permissionService.HasPermissionAsync(userId, PermissionKeys.ApplicationStageUpdate, ct);
    }

    private async Task<bool> HasInterviewAccessAsync(Guid userId, CancellationToken ct)
    {
        return await _permissionService.HasPermissionAsync(userId, PermissionKeys.CandidateManage, ct)
            || await _permissionService.HasPermissionAsync(userId, PermissionKeys.ApplicationStageUpdate, ct)
            || await _permissionService.HasPermissionAsync(userId, PermissionKeys.InterviewParticipate, ct);
    }
}
