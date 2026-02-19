using System.ComponentModel.DataAnnotations;

namespace IkOtomasyon.Api.Contracts;

public class JobCompetencyWeightsUpdateRequest : IValidatableObject
{
    [Required]
    public Dictionary<string, double> Weights { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Weights.Count < 3)
        {
            yield return new ValidationResult("At least 3 competency weights are required.", new[] { nameof(Weights) });
        }

        var sum = 0.0;
        foreach (var (key, value) in Weights)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                yield return new ValidationResult("Competency key cannot be empty.", new[] { nameof(Weights) });
            }

            if (value is < 0 or > 1)
            {
                yield return new ValidationResult($"Competency weight '{key}' must be between 0 and 1.", new[] { nameof(Weights) });
            }

            sum += value;
        }

        if (Math.Abs(sum - 1.0) > 0.01)
        {
            yield return new ValidationResult("Competency weights must sum to 1.0 (±0.01).", new[] { nameof(Weights) });
        }
    }
}
