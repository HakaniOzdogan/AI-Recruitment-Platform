namespace IkOtomasyon.Api.Contracts;

public record JobRubricResponse(
    Guid TemplateId,
    Guid? JobId,
    string Name,
    bool IsDefault,
    DateTime CreatedAt,
    IReadOnlyCollection<JobRubricCriterionResponse> Criteria);

public record JobRubricCriterionResponse(
    string Key,
    string Title,
    string Description,
    double Weight,
    int Order);
