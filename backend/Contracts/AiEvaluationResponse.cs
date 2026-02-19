namespace IkOtomasyon.Api.Contracts;

public record AiEvaluationResponse(
    Guid Id,
    Guid JobId,
    Guid CandidateId,
    Guid? ApplicationId,
    string ModelName,
    string OverallRecommendation,
    string StrengthsJson,
    string RisksJson,
    string VerificationQuestionsJson,
    string EvidenceQuotesJson,
    string CompetencyAssessmentJson,
    string SkillAssessmentJson,
    double Confidence,
    DateTime CreatedAt,
    int Version);
