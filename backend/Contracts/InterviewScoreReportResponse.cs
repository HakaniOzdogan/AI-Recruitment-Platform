namespace IkOtomasyon.Api.Contracts;

public record InterviewScoreReportItemResponse(
    Guid ScorecardId,
    Guid SessionId,
    Guid ApplicationId,
    Guid JobId,
    string JobTitle,
    Guid CandidateId,
    string CandidateFullName,
    Guid StageId,
    string StageName,
    double? OverallScore,
    DateTime ScorecardCreatedAt,
    IReadOnlyCollection<InterviewCriterionLatestResponse> Criteria);
