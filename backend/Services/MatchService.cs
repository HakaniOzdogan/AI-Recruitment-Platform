using System.Text.Json;
using IkOtomasyon.Api.Contracts;
using IkOtomasyon.Api.Data;
using IkOtomasyon.Api.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IkOtomasyon.Api.Services;

public class MatchService
{
    private static readonly TimeSpan PersistentFreshnessWindow = TimeSpan.FromHours(24);
    private static readonly TimeSpan MemoryCacheTtl = TimeSpan.FromMinutes(10);

    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public MatchService(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<MatchResult> GetOrComputeMatchAsync(Guid jobId, Guid candidateId, Guid? computedByUserId, bool force, CancellationToken ct = default)
    {
        var cacheKey = GetCacheKey(jobId, candidateId);
        if (!force && _cache.TryGetValue<MatchResult>(cacheKey, out var cached) && cached is not null)
        {
            if (DateTime.UtcNow - cached.ComputedAt <= PersistentFreshnessWindow)
            {
                return cached;
            }
        }

        var job = await _db.JobPostings.FirstOrDefaultAsync(x => x.Id == jobId, ct)
            ?? throw new NotFoundException("Job not found.");

        var candidate = await _db.Candidates.FirstOrDefaultAsync(x => x.Id == candidateId, ct)
            ?? throw new NotFoundException("Candidate not found.");

        var profile = await _db.CandidateProfiles.FirstOrDefaultAsync(x => x.CandidateId == candidateId, ct)
            ?? throw new ConflictException("CV not parsed; profile missing");

        var existing = await _db.MatchResults.FirstOrDefaultAsync(x => x.JobId == jobId && x.CandidateId == candidateId, ct);
        if (existing is not null && !force && DateTime.UtcNow - existing.ComputedAt <= PersistentFreshnessWindow)
        {
            _cache.Set(cacheKey, existing, MemoryCacheTtl);
            return existing;
        }

        var computed = Compute(job, profile);
        var now = DateTime.UtcNow;

        if (existing is null)
        {
            existing = new MatchResult
            {
                Id = Guid.NewGuid(),
                JobId = job.Id,
                CandidateId = candidate.Id
            };
            _db.MatchResults.Add(existing);
        }

        existing.Score = computed.Score;
        existing.ReasonsJson = JsonSerializer.Serialize(computed.Reasons);
        existing.GapsJson = JsonSerializer.Serialize(computed.Gaps);
        existing.ComputedAt = now;
        existing.ComputedByUserId = computedByUserId;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            existing = await _db.MatchResults.FirstAsync(x => x.JobId == jobId && x.CandidateId == candidateId, ct);
            existing.Score = computed.Score;
            existing.ReasonsJson = JsonSerializer.Serialize(computed.Reasons);
            existing.GapsJson = JsonSerializer.Serialize(computed.Gaps);
            existing.ComputedAt = now;
            existing.ComputedByUserId = computedByUserId;
            await _db.SaveChangesAsync(ct);
        }

        _cache.Set(cacheKey, existing, MemoryCacheTtl);
        return existing;
    }

    public MatchComputation Compute(JobPosting job, CandidateProfile profile)
    {
        var requiredSkills = ParseSkillJson(job.RequiredSkillsJson);
        var niceToHaveSkills = ParseSkillJson(job.NiceToHaveSkillsJson);
        var candidateSkills = ParseSkillJson(profile.SkillsJson);

        var candidateSkillSet = candidateSkills.Select(SkillNormalization.Normalize)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var normalizedRequired = requiredSkills.Select(SkillNormalization.Normalize).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var normalizedNice = niceToHaveSkills.Select(SkillNormalization.Normalize).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var reasons = new List<string>();
        var gaps = new List<string>();

        double requiredScore = 0;
        var matchedRequired = normalizedRequired.Where(candidateSkillSet.Contains).OrderBy(x => x).ToList();
        var missingRequired = normalizedRequired.Where(x => !candidateSkillSet.Contains(x)).OrderBy(x => x).ToList();

        if (normalizedRequired.Count == 0)
        {
            reasons.Add("Required skills are not defined for this job.");
        }
        else
        {
            var coverage = (double)matchedRequired.Count / normalizedRequired.Count;
            requiredScore = coverage * 70.0;
            reasons.Add($"Matched required skills: {matchedRequired.Count}/{normalizedRequired.Count} (list: {string.Join(", ", matchedRequired)})");
            if (missingRequired.Count > 0)
            {
                gaps.Add($"Missing required skills: {string.Join(", ", missingRequired)}");
            }
        }

        var matchedNice = normalizedNice.Where(candidateSkillSet.Contains).OrderBy(x => x).ToList();
        var missingNice = normalizedNice.Where(x => !candidateSkillSet.Contains(x)).OrderBy(x => x).ToList();
        var niceScore = normalizedNice.Count == 0 ? 0 : ((double)matchedNice.Count / normalizedNice.Count) * 20.0;
        reasons.Add($"Matched nice-to-have skills: {matchedNice.Count}/{normalizedNice.Count} (list: {string.Join(", ", matchedNice)})");
        if (missingNice.Count > 0)
        {
            gaps.Add($"Missing nice-to-have skills: {string.Join(", ", missingNice)}");
        }

        double expScore = 0;
        if (job.MinExperienceMonths is null)
        {
            reasons.Add("Experience requirement is not defined for this job.");
        }
        else if (profile.TotalExperienceMonths is null)
        {
            gaps.Add("Experience not detected");
        }
        else
        {
            var ratio = Math.Min((double)profile.TotalExperienceMonths.Value / job.MinExperienceMonths.Value, 1.0);
            expScore = ratio * 10.0;
            reasons.Add($"Experience: candidate {profile.TotalExperienceMonths.Value} months vs required {job.MinExperienceMonths.Value} months");
            if (profile.TotalExperienceMonths.Value < job.MinExperienceMonths.Value)
            {
                gaps.Add($"Experience insufficient: {profile.TotalExperienceMonths.Value}/{job.MinExperienceMonths.Value} months");
            }
        }

        var total = (int)Math.Round(Math.Clamp(requiredScore + niceScore + expScore, 0, 100), MidpointRounding.AwayFromZero);
        return new MatchComputation(total, reasons, gaps);
    }

    public static IReadOnlyCollection<string> ParseReasonJson(string json) => ParseStringJson(json);
    public static IReadOnlyCollection<string> ParseGapJson(string json) => ParseStringJson(json);

    private static IReadOnlyCollection<string> ParseStringJson(string json)
    {
        try
        {
            var arr = JsonSerializer.Deserialize<List<string>>(json);
            return (arr ?? new List<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static List<string> ParseSkillJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            var arr = JsonSerializer.Deserialize<List<string>>(json);
            return arr ?? new List<string>();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid skills JSON payload: {ex.Message}");
        }
    }

    private static string GetCacheKey(Guid jobId, Guid candidateId) => $"match:{jobId:N}:{candidateId:N}";
}
