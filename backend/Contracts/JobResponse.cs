using IkOtomasyon.Api.Entities;

namespace IkOtomasyon.Api.Contracts;

public record JobResponse(
    Guid Id,
    string Title,
    string? Department,
    string? Location,
    string? EmploymentType,
    string Description,
    IReadOnlyCollection<string> RequiredSkills,
    IReadOnlyCollection<string> NiceToHaveSkills,
    int? MinExperienceMonths,
    JobPostingStatus Status,
    Guid CreatedByUserId,
    DateTime CreatedAt,
    DateTime? PublishedAt,
    DateTime? ClosedAt);
