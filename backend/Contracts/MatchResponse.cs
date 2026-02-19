namespace IkOtomasyon.Api.Contracts;

public record MatchResponse(
    Guid JobId,
    Guid CandidateId,
    int Score,
    IReadOnlyCollection<string> Reasons,
    IReadOnlyCollection<string> Gaps,
    DateTime ComputedAt);
