namespace IkOtomasyon.Api.Contracts;

public record JobWeightsResponse(
    Guid JobId,
    Dictionary<string, double> Competencies,
    IReadOnlyCollection<JobSkillWeightResponse> Skills);

public record JobSkillWeightResponse(
    string Name,
    double Weight,
    bool IsRequired,
    DateTime CreatedAt);
