using System.ComponentModel.DataAnnotations;

namespace IkOtomasyon.Api.Contracts;

public class JobSkillWeightsUpdateRequest : IValidatableObject
{
    [Required]
    public List<JobSkillWeightRequest> Skills { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Skills.Count > 200)
        {
            yield return new ValidationResult("Skills cannot contain more than 200 items.", new[] { nameof(Skills) });
        }

        for (var i = 0; i < Skills.Count; i++)
        {
            var skill = Skills[i];
            if (string.IsNullOrWhiteSpace(skill.Name))
            {
                yield return new ValidationResult($"skills[{i}].name cannot be empty.", new[] { nameof(Skills) });
            }

            if (skill.Weight is < 0 or > 1)
            {
                yield return new ValidationResult($"skills[{i}].weight must be between 0 and 1.", new[] { nameof(Skills) });
            }
        }
    }
}

public class JobSkillWeightRequest
{
    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public double Weight { get; set; }
    public bool IsRequired { get; set; }
}
