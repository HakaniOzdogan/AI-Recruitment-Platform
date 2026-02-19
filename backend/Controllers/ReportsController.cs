using System.Text.Json;
using IkOtomasyon.Api.Authorization;
using IkOtomasyon.Api.Contracts;
using IkOtomasyon.Api.Data;
using IkOtomasyon.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IkOtomasyon.Api.Controllers;

[ApiController]
[Authorize]
[Route("reports")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PermissionService _permissionService;

    public ReportsController(AppDbContext db, PermissionService permissionService)
    {
        _db = db;
        _permissionService = permissionService;
    }

    [HttpGet("interview-scores")]
    public async Task<ActionResult<IEnumerable<InterviewScoreReportItemResponse>>> InterviewScores(
        [FromQuery] Guid? jobId,
        [FromQuery] Guid? candidateId,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var userId = User.TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (!await CanViewReportsAsync(userId.Value, ct))
        {
            return Forbid();
        }

        limit = Math.Clamp(limit, 1, 200);

        var scorecards = _db.InterviewScorecards
            .AsNoTracking()
            .Include(x => x.Session)
            .ThenInclude(x => x!.Application)
            .ThenInclude(x => x!.Job)
            .Include(x => x.Session)
            .ThenInclude(x => x!.Application)
            .ThenInclude(x => x!.Candidate)
            .Include(x => x.Session)
            .ThenInclude(x => x!.Application)
            .ThenInclude(x => x!.Stage)
            .AsQueryable();

        if (jobId is not null)
        {
            scorecards = scorecards.Where(x => x.Session!.Application!.JobId == jobId.Value);
        }

        if (candidateId is not null)
        {
            scorecards = scorecards.Where(x => x.Session!.Application!.CandidateId == candidateId.Value);
        }

        var scorecardList = await scorecards
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

        if (scorecardList.Count == 0)
        {
            return Ok(Array.Empty<InterviewScoreReportItemResponse>());
        }

        var scorecardIds = scorecardList.Select(x => x.Id).ToList();
        var allScores = await _db.InterviewCriterionScores
            .AsNoTracking()
            .Where(x => scorecardIds.Contains(x.ScorecardId))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        var latestByScorecard = allScores
            .GroupBy(x => x.ScorecardId)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(x => x.CriterionKey, StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.First())
                    .OrderBy(x => x.CriterionKey, StringComparer.OrdinalIgnoreCase)
                    .ToList());

        var result = scorecardList.Select(card =>
        {
            var session = card.Session;
            var app = session?.Application;
            if (session is null || app is null)
            {
                return null;
            }

            var latest = latestByScorecard.TryGetValue(card.Id, out var items)
                ? items.Select(MapCriterion).ToList()
                : new List<InterviewCriterionLatestResponse>();

            return new InterviewScoreReportItemResponse(
                card.Id,
                card.SessionId,
                app.Id,
                app.JobId,
                app.Job?.Title ?? "(Job silinmis)",
                app.CandidateId,
                app.Candidate?.FullName ?? "(Aday silinmis)",
                app.StageId,
                app.Stage?.Name ?? "(Stage yok)",
                card.OverallScore,
                card.CreatedAt,
                latest);
        })
        .Where(x => x is not null)
        .Cast<InterviewScoreReportItemResponse>()
        .ToList();

        return Ok(result);
    }

    private async Task<bool> CanViewReportsAsync(Guid userId, CancellationToken ct)
    {
        return await _permissionService.HasPermissionAsync(userId, PermissionKeys.CandidateManage, ct)
            || await _permissionService.HasPermissionAsync(userId, PermissionKeys.ApplicationStageUpdate, ct)
            || await _permissionService.HasPermissionAsync(userId, PermissionKeys.UserManage, ct);
    }

    private static InterviewCriterionLatestResponse MapCriterion(IkOtomasyon.Api.Entities.InterviewCriterionScore score)
    {
        return new InterviewCriterionLatestResponse(
            score.CriterionKey,
            score.Status,
            score.Score,
            score.Rationale,
            ParseEvidence(score.EvidenceQuotesJson),
            score.EvaluatorType,
            score.EnrichmentStatus,
            score.EnrichedByModel,
            ParseErrors(score.EnrichmentErrorsJson),
            score.CreatedAt,
            score.CreatedByUserId);
    }

    private static IReadOnlyCollection<EvidenceQuoteResponse> ParseEvidence(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<EvidenceQuoteResponse>>(json)
                ?.Where(x => !string.IsNullOrWhiteSpace(x.Quote))
                .Select(x => new EvidenceQuoteResponse(x.Quote.Trim(), string.IsNullOrWhiteSpace(x.RelatedTo) ? "general" : x.RelatedTo.Trim()))
                .ToList()
                ?? new List<EvidenceQuoteResponse>();
        }
        catch
        {
            return Array.Empty<EvidenceQuoteResponse>();
        }
    }

    private static IReadOnlyCollection<string> ParseErrors(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json)
                ?.Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList()
                ?? new List<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}
