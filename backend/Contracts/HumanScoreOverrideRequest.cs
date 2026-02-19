using System.ComponentModel.DataAnnotations;
using IkOtomasyon.Api.Services;

namespace IkOtomasyon.Api.Contracts;

public class HumanScoreOverrideRequest : IValidatableObject
{
    [Required]
    [MaxLength(80)]
    public string CriterionKey { get; set; } = string.Empty;

    [Range(0, 5)]
    public double Score { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Rationale { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public List<EvidenceQuoteRequest> EvidenceQuotes { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var key = CriterionKey.Trim().ToLowerInvariant();
        if (!RubricKeys.All.Contains(key, StringComparer.OrdinalIgnoreCase))
        {
            yield return new ValidationResult("criterionKey is not allowed.", new[] { nameof(CriterionKey) });
        }

        for (var i = 0; i < EvidenceQuotes.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(EvidenceQuotes[i].Quote))
            {
                yield return new ValidationResult($"evidenceQuotes[{i}].quote is required.", new[] { nameof(EvidenceQuotes) });
            }
        }
    }
}

public class EvidenceQuoteRequest
{
    [Required]
    [MaxLength(200)]
    public string Quote { get; set; } = string.Empty;

    [MaxLength(120)]
    public string RelatedTo { get; set; } = "general";
}
