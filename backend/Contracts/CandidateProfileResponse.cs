namespace IkOtomasyon.Api.Contracts;

public record CandidateProfileResponse(
    Guid CandidateId,
    string FullNameSnapshot,
    string? EmailSnapshot,
    string? Summary,
    int? TotalExperienceMonths,
    string SkillsJson,
    string EducationJson,
    string ExperienceJson,
    string LanguagesJson,
    string LinksJson,
    DateTime UpdatedAt);
