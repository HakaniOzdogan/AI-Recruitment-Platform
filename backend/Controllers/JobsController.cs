using IkOtomasyon.Api.Authorization;
using IkOtomasyon.Api.Contracts;
using IkOtomasyon.Api.Data;
using IkOtomasyon.Api.Entities;
using IkOtomasyon.Api.Middleware;
using IkOtomasyon.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace IkOtomasyon.Api.Controllers;

[ApiController]
[Authorize]
[Route("jobs")]
public class JobsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AtsWorkflowService _workflow;
    private readonly MatchService _matchService;
    private readonly PermissionService _permissionService;
    private readonly RubricService _rubricService;

    public JobsController(
        AppDbContext db,
        AtsWorkflowService workflow,
        MatchService matchService,
        PermissionService permissionService,
        RubricService rubricService)
    {
        _db = db;
        _workflow = workflow;
        _matchService = matchService;
        _permissionService = permissionService;
        _rubricService = rubricService;
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.JobCreate)]
    public async Task<ActionResult<JobResponse>> Create([FromBody] JobCreateRequest request, CancellationToken ct)
    {
        var userId = User.TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var job = await _workflow.CreateJobAsync(request, userId.Value, ct);
        return CreatedAtAction(nameof(GetById), new { id = job.Id }, ToResponse(job));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobResponse>>> List([FromQuery] JobPostingStatus? status, [FromQuery] string? q, CancellationToken ct)
    {
        var query = _db.JobPostings.AsNoTracking().AsQueryable();

        if (status is not null)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim();
            query = query.Where(x =>
                EF.Functions.ILike(x.Title, $"%{search}%") ||
                (x.Department != null && EF.Functions.ILike(x.Department, $"%{search}%")) ||
                (x.Location != null && EF.Functions.ILike(x.Location, $"%{search}%")));
        }

        var data = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => ToResponse(x))
            .ToListAsync(ct);

        return Ok(data);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobResponse>> GetById(Guid id, CancellationToken ct)
    {
        var job = await _db.JobPostings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (job is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(job));
    }

    [HttpPatch("{id:guid}")]
    [RequirePermission(PermissionKeys.JobCreate)]
    public async Task<ActionResult<JobResponse>> Update(Guid id, [FromBody] JobUpdateRequest request, CancellationToken ct)
    {
        try
        {
            var job = await _workflow.UpdateJobAsync(id, request, ct);
            return Ok(ToResponse(job));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:guid}/publish")]
    [RequirePermission(PermissionKeys.JobPublish)]
    public async Task<ActionResult<JobResponse>> Publish(Guid id, CancellationToken ct)
    {
        try
        {
            var job = await _workflow.PublishJobAsync(id, ct);
            HttpContext.SetAuditInfo(AuditActions.JobPublish, "JobPosting", job.Id.ToString());
            return Ok(ToResponse(job));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (InvalidStateTransitionException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/close")]
    [RequirePermission(PermissionKeys.JobPublish)]
    public async Task<ActionResult<JobResponse>> Close(Guid id, CancellationToken ct)
    {
        try
        {
            var job = await _workflow.CloseJobAsync(id, ct);
            HttpContext.SetAuditInfo(AuditActions.JobClose, "JobPosting", job.Id.ToString());
            return Ok(ToResponse(job));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (InvalidStateTransitionException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{jobId:guid}/candidates/{candidateId:guid}/match")]
    public async Task<ActionResult<MatchResponse>> Match(Guid jobId, Guid candidateId, [FromQuery] bool force = false, CancellationToken ct = default)
    {
        var userId = User.TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (!await HasMatchViewPermissionAsync(userId.Value, ct))
        {
            return Forbid();
        }

        try
        {
            var result = await _matchService.GetOrComputeMatchAsync(jobId, candidateId, userId, force, ct);
            if (force)
            {
                await WriteMatchRecomputeAuditAsync(userId.Value, jobId, candidateId, ct);
            }

            return Ok(ToMatchResponse(result));
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

    [HttpGet("{jobId:guid}/matches")]
    public async Task<ActionResult<IEnumerable<JobMatchListItemResponse>>> ListMatches(
        Guid jobId,
        [FromQuery] int? minScore,
        [FromQuery] Guid? stageId,
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0,
        CancellationToken ct = default)
    {
        var userId = User.TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (!await HasMatchViewPermissionAsync(userId.Value, ct))
        {
            return Forbid();
        }

        var jobExists = await _db.JobPostings.AnyAsync(x => x.Id == jobId, ct);
        if (!jobExists)
        {
            return NotFound(new { message = "Job not found." });
        }

        limit = Math.Clamp(limit, 1, 200);
        offset = Math.Max(0, offset);

        var appQuery = _db.Applications
            .AsNoTracking()
            .Include(x => x.Candidate)
            .Include(x => x.Stage)
            .Where(x => x.JobId == jobId);

        if (stageId is not null)
        {
            appQuery = appQuery.Where(x => x.StageId == stageId.Value);
        }

        var apps = await appQuery.ToListAsync(ct);
        var items = new List<JobMatchListItemResponse>();

        foreach (var app in apps)
        {
            try
            {
                var match = await _matchService.GetOrComputeMatchAsync(jobId, app.CandidateId, userId, force: false, ct);
                if (minScore is not null && match.Score < minScore.Value)
                {
                    continue;
                }

                items.Add(new JobMatchListItemResponse(
                    app.CandidateId,
                    app.Candidate?.FullName ?? string.Empty,
                    app.Id,
                    app.StageId,
                    app.Stage?.Name ?? string.Empty,
                    match.Score,
                    match.ComputedAt,
                    MatchService.ParseReasonJson(match.ReasonsJson),
                    MatchService.ParseGapJson(match.GapsJson)));
            }
            catch (ConflictException)
            {
                continue;
            }
        }

        var paged = items
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.ComputedAt)
            .ThenBy(x => x.CandidateFullName)
            .Skip(offset)
            .Take(limit)
            .ToList();

        return Ok(paged);
    }

    [HttpPut("{jobId:guid}/weights/competencies")]
    [RequirePermission(PermissionKeys.JobCreate)]
    public async Task<ActionResult<JobWeightsResponse>> UpdateCompetencyWeights(Guid jobId, [FromBody] JobCompetencyWeightsUpdateRequest request, CancellationToken ct)
    {
        try
        {
            await _workflow.UpdateCompetencyWeightsAsync(jobId, request.Weights, ct);
            var data = await _workflow.GetJobWeightsAsync(jobId, ct);
            HttpContext.SetAuditInfo(AuditActions.JobWeightsUpdate, "JobPosting", jobId.ToString());
            return Ok(ToWeightsResponse(jobId, data.Competencies, data.Skills));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{jobId:guid}/weights/skills")]
    [RequirePermission(PermissionKeys.JobCreate)]
    public async Task<ActionResult<JobWeightsResponse>> UpsertSkillWeights(Guid jobId, [FromBody] JobSkillWeightsUpdateRequest request, CancellationToken ct)
    {
        try
        {
            await _workflow.UpsertSkillWeightsAsync(jobId, request.Skills, ct);
            var data = await _workflow.GetJobWeightsAsync(jobId, ct);
            HttpContext.SetAuditInfo(AuditActions.JobWeightsUpdate, "JobPosting", jobId.ToString());
            return Ok(ToWeightsResponse(jobId, data.Competencies, data.Skills));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{jobId:guid}/weights")]
    [RequirePermission(PermissionKeys.JobCreate)]
    public async Task<ActionResult<JobWeightsResponse>> GetWeights(Guid jobId, CancellationToken ct)
    {
        try
        {
            var data = await _workflow.GetJobWeightsAsync(jobId, ct);
            return Ok(ToWeightsResponse(jobId, data.Competencies, data.Skills));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{jobId:guid}/rubric")]
    [RequirePermission(PermissionKeys.JobCreate)]
    public async Task<ActionResult<JobRubricResponse>> GetRubric(Guid jobId, CancellationToken ct)
    {
        try
        {
            var template = await _rubricService.GetJobRubricOrDefaultAsync(jobId, ct);
            return Ok(ToRubricResponse(template));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{jobId:guid}/rubric")]
    [RequirePermission(PermissionKeys.JobCreate)]
    public async Task<ActionResult<JobRubricResponse>> UpsertRubric(Guid jobId, [FromBody] JobRubricUpdateRequest request, CancellationToken ct)
    {
        try
        {
            var template = await _rubricService.UpsertJobRubricAsync(jobId, request, ct);
            HttpContext.SetAuditInfo(AuditActions.JobWeightsUpdate, "RubricTemplate", template.Id.ToString());
            return Ok(ToRubricResponse(template));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private static JobResponse ToResponse(JobPosting entity)
    {
        return new JobResponse(
            entity.Id,
            entity.Title,
            entity.Department,
            entity.Location,
            entity.EmploymentType,
            entity.Description,
            ParseSkills(entity.RequiredSkillsJson),
            ParseSkills(entity.NiceToHaveSkillsJson),
            entity.MinExperienceMonths,
            entity.Status,
            entity.CreatedByUserId,
            entity.CreatedAt,
            entity.PublishedAt,
            entity.ClosedAt);
    }

    private static MatchResponse ToMatchResponse(MatchResult result)
    {
        return new MatchResponse(
            result.JobId,
            result.CandidateId,
            result.Score,
            MatchService.ParseReasonJson(result.ReasonsJson),
            MatchService.ParseGapJson(result.GapsJson),
            result.ComputedAt);
    }

    private static IReadOnlyCollection<string> ParseSkills(string json)
    {
        try
        {
            var values = JsonSerializer.Deserialize<List<string>>(json);
            return values ?? new List<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static JobWeightsResponse ToWeightsResponse(Guid jobId, Dictionary<string, double> competencies, IReadOnlyCollection<JobSkillWeight> skills)
    {
        var skillResponses = skills.Select(x => new JobSkillWeightResponse(
            x.SkillNameNormalized,
            x.Weight,
            x.IsRequired,
            x.CreatedAt)).ToList();

        return new JobWeightsResponse(jobId, competencies, skillResponses);
    }

    private static JobRubricResponse ToRubricResponse(RubricTemplate template)
    {
        var criteria = template.Criteria
            .OrderBy(x => x.Order)
            .Select(x => new JobRubricCriterionResponse(x.Key, x.Title, x.Description, x.Weight, x.Order))
            .ToList();

        return new JobRubricResponse(
            template.Id,
            template.JobId,
            template.Name,
            template.IsDefault,
            template.CreatedAt,
            criteria);
    }

    private async Task<bool> HasMatchViewPermissionAsync(Guid userId, CancellationToken ct)
    {
        return await _permissionService.HasPermissionAsync(userId, PermissionKeys.JobCreate, ct)
            || await _permissionService.HasPermissionAsync(userId, PermissionKeys.CandidateManage, ct);
    }

    private async Task WriteMatchRecomputeAuditAsync(Guid actorUserId, Guid jobId, Guid candidateId, CancellationToken ct)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Method = "GET",
            Path = HttpContext.Request.Path.Value ?? string.Empty,
            Action = AuditActions.MatchRecompute,
            Entity = "MatchResult",
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
