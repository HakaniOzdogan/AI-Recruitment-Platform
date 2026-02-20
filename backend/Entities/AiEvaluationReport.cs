namespace IkOtomasyon.Api.Entities;

public class AiEvaluationReport
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public Guid JobId { get; set; }
    public JobPosting? Job { get; set; }
    public Guid CandidateId { get; set; }
    public Candidate? Candidate { get; set; }
    public Guid? ApplicationId { get; set; }
    public Application? Application { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string InputSnapshotJson { get; set; } = string.Empty;
    public string InputSnapshotHash { get; set; } = string.Empty;
    public string OverallRecommendation { get; set; } = string.Empty;
    public string StrengthsJson { get; set; } = "[]";
    public string RisksJson { get; set; } = "[]";
    public string VerificationQuestionsJson { get; set; } = "[]";
    public string EvidenceQuotesJson { get; set; } = "[]";
    public string CompetencyAssessmentJson { get; set; } = "[]";
    public string SkillAssessmentJson { get; set; } = "{}";
    public double Confidence { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
    public int Version { get; set; }
}
