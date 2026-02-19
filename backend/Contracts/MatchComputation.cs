namespace IkOtomasyon.Api.Contracts;

public record MatchComputation(int Score, IReadOnlyCollection<string> Reasons, IReadOnlyCollection<string> Gaps);
