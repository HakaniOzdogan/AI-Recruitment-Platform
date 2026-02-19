using System.ComponentModel.DataAnnotations;

namespace IkOtomasyon.Api.Contracts;

public class JobCreateRequest : IValidatableObject
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Department { get; set; }

    [MaxLength(150)]
    public string? Location { get; set; }

    [MaxLength(100)]
    public string? EmploymentType { get; set; }

    [MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    public List<string>? RequiredSkills { get; set; }
    public List<string>? NiceToHaveSkills { get; set; }

    [Range(0, 600)]
    public int? MinExperienceMonths { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in JobSkillValidation.ValidateSkills(RequiredSkills, nameof(RequiredSkills)))
        {
            yield return result;
        }

        foreach (var result in JobSkillValidation.ValidateSkills(NiceToHaveSkills, nameof(NiceToHaveSkills)))
        {
            yield return result;
        }
    }
}
