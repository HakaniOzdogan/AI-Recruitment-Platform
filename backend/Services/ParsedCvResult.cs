namespace IkOtomasyon.Api.Services;

public record ParsedCvResult(
    string? FullNameSnapshot,
    string? EmailSnapshot,
    string? Summary,
    int? TotalExperienceMonths,
    IReadOnlyCollection<string> Skills,
    IReadOnlyCollection<string> Education,
    IReadOnlyCollection<string> Experience,
    IReadOnlyCollection<string> Languages,
    IReadOnlyCollection<string> Links);
