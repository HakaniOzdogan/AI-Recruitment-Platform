using System.ComponentModel.DataAnnotations;
using IkOtomasyon.Api.Services;

namespace IkOtomasyon.Api.Contracts;

public class JobRubricUpdateRequest : IValidatableObject
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public List<RubricCriterionRequest> Criteria { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Criteria.Count is < 1 or > 10)
        {
            yield return new ValidationResult("Criteria count must be between 1 and 10.", new[] { nameof(Criteria) });
            yield break;
        }

        var keySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sum = 0.0;
        for (var i = 0; i < Criteria.Count; i++)
        {
            var item = Criteria[i];
            if (string.IsNullOrWhiteSpace(item.Key))
            {
                yield return new ValidationResult($"criteria[{i}].key is required.", new[] { nameof(Criteria) });
                continue;
            }

            var key = item.Key.Trim().ToLowerInvariant();
            if (!RubricKeys.All.Contains(key, StringComparer.OrdinalIgnoreCase))
            {
                yield return new ValidationResult($"criteria[{i}].key is not allowed.", new[] { nameof(Criteria) });
            }

            if (!keySet.Add(key))
            {
                yield return new ValidationResult($"criteria[{i}].key is duplicated.", new[] { nameof(Criteria) });
            }

            if (item.Weight is < 0 or > 1)
            {
                yield return new ValidationResult($"criteria[{i}].weight must be between 0 and 1.", new[] { nameof(Criteria) });
            }

            sum += item.Weight;
        }

        if (Math.Abs(sum - 1.0) > 0.01)
        {
            yield return new ValidationResult("Criterion weights must sum to 1.0 (±0.01).", new[] { nameof(Criteria) });
        }
    }
}

public class RubricCriterionRequest
{
    [Required]
    [MaxLength(80)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public double Weight { get; set; }
    public int Order { get; set; }
}
