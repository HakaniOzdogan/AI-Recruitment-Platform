namespace IkOtomasyon.Api.Contracts;

public record JobMatchListItemResponse(
    Guid CandidateId,
    string CandidateFullName,
    Guid ApplicationId,
    Guid StageId,
    string StageName,
    int Score,
    DateTime ComputedAt,
    IReadOnlyCollection<string> Reasons,
    IReadOnlyCollection<string> Gaps);
