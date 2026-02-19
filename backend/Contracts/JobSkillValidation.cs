using System.ComponentModel.DataAnnotations;

namespace IkOtomasyon.Api.Contracts;

public static class JobSkillValidation
{
    public static IEnumerable<ValidationResult> ValidateSkills(List<string>? values, string memberName)
    {
        if (values is null)
        {
            yield break;
        }

        if (values.Count > 50)
        {
            yield return new ValidationResult($"{memberName} cannot contain more than 50 items.", new[] { memberName });
        }

        for (var i = 0; i < values.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(values[i]))
            {
                yield return new ValidationResult($"{memberName}[{i}] cannot be empty.", new[] { memberName });
            }
        }
    }
}
