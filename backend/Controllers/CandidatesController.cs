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
[Route("candidates")]
public class CandidatesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AtsWorkflowService _workflow;
    private readonly CvWorkflowService _cvWorkflow;
    private readonly IAuthorizationService _authorizationService;

    public CandidatesController(AppDbContext db, AtsWorkflowService workflow, CvWorkflowService cvWorkflow, IAuthorizationService authorizationService)
    {
        _db = db;
        _workflow = workflow;
        _cvWorkflow = cvWorkflow;
        _authorizationService = authorizationService;
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.CandidateCreate)]
    public async Task<ActionResult<CandidateResponse>> Create([FromBody] CandidateCreateRequest request, CancellationToken ct)
    {
        var userId = User.TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var candidate = await _workflow.CreateCandidateAsync(request, userId.Value, ownerIsActor: User.IsApplicant(), ct);
        HttpContext.SetAuditInfo(AuditActions.CandidateCreate, "Candidate", candidate.Id.ToString());
        return CreatedAtAction(nameof(GetById), new { id = candidate.Id }, ToResponse(candidate));
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.CandidateRead)]
    public async Task<ActionResult<IEnumerable<CandidateResponse>>> List([FromQuery] string? q, CancellationToken ct)
    {
        var userId = User.TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var query = _db.Candidates.AsNoTracking().AsQueryable();
        if (User.IsApplicant())
        {
            query = query.Where(x => x.OwnerUserId == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim();
            query = query.Where(x =>
                EF.Functions.ILike(x.FullName, $"%{search}%") ||
                (x.Email != null && EF.Functions.ILike(x.Email, $"%{search}%")) ||
                (x.Phone != null && EF.Functions.ILike(x.Phone, $"%{search}%")) ||
                (x.Source != null && EF.Functions.ILike(x.Source, $"%{search}%")));
        }

        var data = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => ToResponse(x))
            .ToListAsync(ct);

        return Ok(data);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.CandidateRead)]
    public async Task<ActionResult<CandidateResponse>> GetById(Guid id, CancellationToken ct)
    {
        if (!await CanAsync(new CandidateAuthorizationResource(id), ResourcePolicies.CanReadCandidate))
        {
            return NotFound();
        }

        var candidate = await _db.Candidates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (candidate is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(candidate));
    }

    [HttpPatch("{id:guid}")]
    [RequirePermission(PermissionKeys.CandidateUpdate)]
    public async Task<ActionResult<CandidateResponse>> Update(Guid id, [FromBody] CandidateUpdateRequest request, CancellationToken ct)
    {
        if (!await CanAsync(new CandidateAuthorizationResource(id), ResourcePolicies.CanEditCandidate))
        {
            return NotFound();
        }

        try
        {
            var candidate = await _workflow.UpdateCandidateAsync(id, request, ct);
            HttpContext.SetAuditInfo(AuditActions.CandidateUpdate, "Candidate", candidate.Id.ToString());
            return Ok(ToResponse(candidate));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{candidateId:guid}/cv")]
    [RequirePermission(PermissionKeys.CandidateCvUpload)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<CvUploadResponse>> UploadCv(Guid candidateId, [FromForm] CvUploadFormRequest request, CancellationToken ct)
    {
        if (!await CanAsync(new CandidateAuthorizationResource(candidateId), ResourcePolicies.CanEditCandidate))
        {
            return NotFound();
        }

        var file = request.File;
        if (file is null)
        {
            return BadRequest(new { message = "file is required." });
        }

        var userId = User.TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            var doc = await _cvWorkflow.UploadCvAsync(candidateId, userId.Value, file, ct);
            HttpContext.SetAuditInfo(AuditActions.CvUpload, "CvDocument", doc.Id.ToString());
            return Ok(new CvUploadResponse(
                doc.Id,
                doc.CandidateId,
                doc.OriginalFileName,
                doc.FileType,
                doc.FileSize,
                doc.ParseStatus.ToString(),
                doc.UploadedAt));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (RequestEntityTooLargeException ex)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{candidateId:guid}/cv")]
    [RequirePermission(PermissionKeys.CandidateRead)]
    public async Task<ActionResult<IEnumerable<CvDocumentResponse>>> ListCv(Guid candidateId, CancellationToken ct)
    {
        if (!await CanAsync(new CandidateAuthorizationResource(candidateId), ResourcePolicies.CanReadCandidate))
        {
            return NotFound();
        }

        var candidateExists = await _db.Candidates.AnyAsync(x => x.Id == candidateId, ct);
        if (!candidateExists)
        {
            return NotFound(new { message = "Candidate not found." });
        }

        var docs = await _db.CvDocuments
            .AsNoTracking()
            .Where(x => x.CandidateId == candidateId)
            .OrderByDescending(x => x.UploadedAt)
            .Select(x => new CvDocumentResponse(
                x.Id,
                x.CandidateId,
                x.OriginalFileName,
                x.FileType,
                x.FileSize,
                x.UploadedAt,
                x.UploadedByUserId,
                x.ParseStatus.ToString(),
                x.ParseError,
                x.ParsedAt))
            .ToListAsync(ct);

        return Ok(docs);
    }

    [HttpGet("{candidateId:guid}/profile")]
    [RequirePermission(PermissionKeys.CandidateRead)]
    public async Task<ActionResult<CandidateProfileResponse>> GetProfile(Guid candidateId, CancellationToken ct)
    {
        if (!await CanAsync(new CandidateAuthorizationResource(candidateId), ResourcePolicies.CanReadCandidate))
        {
            return NotFound();
        }

        var profile = await _db.CandidateProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.CandidateId == candidateId, ct);
        if (profile is null)
        {
            return NotFound();
        }

        return Ok(new CandidateProfileResponse(
            profile.CandidateId,
            profile.FullNameSnapshot,
            profile.EmailSnapshot,
            profile.Summary,
            profile.TotalExperienceMonths,
            profile.SkillsJson,
            profile.EducationJson,
            profile.ExperienceJson,
            profile.LanguagesJson,
            profile.LinksJson,
            profile.UpdatedAt));
    }

    [HttpPost("{candidateId:guid}/consent")]
    [RequirePermission(PermissionKeys.CandidateUpdate)]
    public async Task<ActionResult<object>> UpsertConsent(Guid candidateId, [FromBody] CandidateConsentRequest request, CancellationToken ct)
    {
        if (!await CanAsync(new CandidateAuthorizationResource(candidateId), ResourcePolicies.CanEditCandidate))
        {
            return NotFound();
        }

        var exists = await _db.Candidates.AnyAsync(x => x.Id == candidateId, ct);
        if (!exists)
        {
            return NotFound(new { message = "Candidate not found." });
        }

        var consent = await _db.CandidateConsents.FirstOrDefaultAsync(x => x.CandidateId == candidateId, ct);
        if (consent is null)
        {
            consent = new CandidateConsent
            {
                CandidateId = candidateId
            };
            _db.CandidateConsents.Add(consent);
        }

        consent.ConsentGiven = request.ConsentGiven;
        consent.ConsentTextVersion = request.ConsentTextVersion.Trim();
        consent.ConsentAt = DateTime.UtcNow;
        consent.DataRetentionDays = request.DataRetentionDays;
        consent.DeleteRequestedAt = null;

        await _db.SaveChangesAsync(ct);
        return Ok(new
        {
            consent.CandidateId,
            consent.ConsentGiven,
            consent.ConsentTextVersion,
            consent.ConsentAt,
            consent.DataRetentionDays
        });
    }

    private static CandidateResponse ToResponse(Candidate entity)
    {
        return new CandidateResponse(
            entity.Id,
            entity.FullName,
            entity.Email,
            entity.Phone,
            entity.Source,
            entity.CreatedAt);
    }

    private async Task<bool> CanAsync(IAuthorizationResource resource, string policy)
    {
        var result = await _authorizationService.AuthorizeAsync(User, resource, policy);
        return result.Succeeded;
    }
}
