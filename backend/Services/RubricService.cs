using System.Text.Json;
using IkOtomasyon.Api.Contracts;
using IkOtomasyon.Api.Data;
using IkOtomasyon.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace IkOtomasyon.Api.Services;

public class RubricService
{
    private readonly AppDbContext _db;

    public RubricService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<RubricTemplate> GetJobRubricOrDefaultAsync(Guid jobId, CancellationToken ct = default)
    {
        var job = await _db.JobPostings.FirstOrDefaultAsync(x => x.Id == jobId, ct);
        if (job is null)
        {
            throw new NotFoundException("Job not found.");
        }

        var template = await _db.RubricTemplates
            .Include(x => x.Criteria)
            .Where(x => x.JobId == jobId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (template is not null)
        {
            template.Criteria = template.Criteria.OrderBy(x => x.Order).ToList();
            return template;
        }

        var competencyWeights = ParseWeights(job.CompetencyWeightsJson);
        if (competencyWeights.Count > 0)
        {
            return await CreateJobTemplateFromCompetenciesAsync(jobId, competencyWeights, ct);
        }

        var fallback = await _db.RubricTemplates
            .Include(x => x.Criteria)
            .Where(x => x.IsDefault && x.JobId == null)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (fallback is null)
        {
            fallback = await CreateDefaultTemplateAsync(ct);
        }

        fallback.Criteria = fallback.Criteria.OrderBy(x => x.Order).ToList();
        return fallback;
    }

    public async Task<RubricTemplate> UpsertJobRubricAsync(Guid jobId, JobRubricUpdateRequest request, CancellationToken ct = default)
    {
        var job = await _db.JobPostings.FirstOrDefaultAsync(x => x.Id == jobId, ct)
            ?? throw new NotFoundException("Job not found.");

        var template = await _db.RubricTemplates
            .Include(x => x.Criteria)
            .Where(x => x.JobId == jobId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (template is null)
        {
            template = new RubricTemplate
            {
                Id = Guid.NewGuid(),
                JobId = jobId,
                Name = request.Name.Trim(),
                IsDefault = false,
                CreatedAt = DateTime.UtcNow
            };

            _db.RubricTemplates.Add(template);
        }
        else
        {
            template.Name = request.Name.Trim();
            _db.RubricCriteria.RemoveRange(template.Criteria);
        }

        foreach (var item in request.Criteria
                     .OrderBy(x => x.Order)
                     .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            _db.RubricCriteria.Add(new RubricCriterion
            {
                Id = Guid.NewGuid(),
                TemplateId = template.Id,
                Key = item.Key.Trim().ToLowerInvariant(),
                Title = item.Title.Trim(),
                Description = item.Description.Trim(),
                Weight = item.Weight,
                Order = item.Order
            });
        }

        if (request.Criteria.Count == 0)
        {
            var defaults = BuildDefaultsFromJob(job);
            foreach (var item in defaults)
            {
                _db.RubricCriteria.Add(item);
            }
        }

        await _db.SaveChangesAsync(ct);

        var refreshed = await _db.RubricTemplates
            .Include(x => x.Criteria)
            .FirstAsync(x => x.Id == template.Id, ct);
        refreshed.Criteria = refreshed.Criteria.OrderBy(x => x.Order).ToList();
        return refreshed;
    }

    private async Task<RubricTemplate> CreateDefaultTemplateAsync(CancellationToken ct)
    {
        var template = new RubricTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Default Interview Rubric",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.RubricTemplates.Add(template);
        foreach (var (key, weight, order) in DefaultCriteria())
        {
            _db.RubricCriteria.Add(new RubricCriterion
            {
                Id = Guid.NewGuid(),
                TemplateId = template.Id,
                Key = key,
                Title = DefaultTitle(key),
                Description = DefaultDescription(key),
                Weight = weight,
                Order = order
            });
        }

        await _db.SaveChangesAsync(ct);
        return await _db.RubricTemplates.Include(x => x.Criteria).FirstAsync(x => x.Id == template.Id, ct);
    }

    private static List<RubricCriterion> BuildDefaultsFromJob(JobPosting job)
    {
        var weights = ParseWeights(job.CompetencyWeightsJson);
        var defaultWeight = weights.Count == 0 ? 0.20 : 0;
        var criteria = new List<RubricCriterion>();

        for (var i = 0; i < RubricKeys.All.Length; i++)
        {
            var key = RubricKeys.All[i];
            criteria.Add(new RubricCriterion
            {
                Id = Guid.NewGuid(),
                Key = key,
                Title = DefaultTitle(key),
                Description = DefaultDescription(key),
                Weight = weights.TryGetValue(key, out var value) ? value : defaultWeight,
                Order = i + 1
            });
        }

        return criteria;
    }

    private async Task<RubricTemplate> CreateJobTemplateFromCompetenciesAsync(Guid jobId, Dictionary<string, double> weights, CancellationToken ct)
    {
        var template = new RubricTemplate
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            Name = "Job Rubric (Auto)",
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };
        _db.RubricTemplates.Add(template);

        for (var i = 0; i < RubricKeys.All.Length; i++)
        {
            var key = RubricKeys.All[i];
            _db.RubricCriteria.Add(new RubricCriterion
            {
                Id = Guid.NewGuid(),
                TemplateId = template.Id,
                Key = key,
                Title = DefaultTitle(key),
                Description = DefaultDescription(key),
                Weight = weights.TryGetValue(key, out var value) ? value : 0,
                Order = i + 1
            });
        }

        await _db.SaveChangesAsync(ct);
        return await _db.RubricTemplates.Include(x => x.Criteria).FirstAsync(x => x.Id == template.Id, ct);
    }

    private static IEnumerable<(string Key, double Weight, int Order)> DefaultCriteria()
    {
        for (var i = 0; i < RubricKeys.All.Length; i++)
        {
            yield return (RubricKeys.All[i], 0.20, i + 1);
        }
    }

    private static Dictionary<string, double> ParseWeights(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, double>>(json)
                ?.ToDictionary(x => x.Key.Trim().ToLowerInvariant(), x => x.Value, StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string DefaultTitle(string key) => key switch
    {
        RubricKeys.Technical => "Technical",
        RubricKeys.ProblemSolving => "Problem Solving",
        RubricKeys.Communication => "Communication",
        RubricKeys.CultureFit => "Culture Fit",
        RubricKeys.DomainKnowledge => "Domain Knowledge",
        _ => key
    };

    private static string DefaultDescription(string key) => key switch
    {
        RubricKeys.Technical => "Depth and correctness of technical knowledge.",
        RubricKeys.ProblemSolving => "Structured analysis and trade-off reasoning.",
        RubricKeys.Communication => "Clarity, structure, and concise expression.",
        RubricKeys.CultureFit => "Teamwork, ownership, and working style alignment.",
        RubricKeys.DomainKnowledge => "Relevant domain and business context understanding.",
        _ => key
    };
}
