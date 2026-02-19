namespace IkOtomasyon.Api.Entities;

public class InterviewCriterionScore
{
    public Guid Id { get; set; }
    public Guid ScorecardId { get; set; }
    public InterviewScorecard? Scorecard { get; set; }
    public string CriterionKey { get; set; } = string.Empty;
    public string Status { get; set; } = InterviewCriterionScoreStatus.Scored;
    public double? Score { get; set; }
    public string? Rationale { get; set; }
    public string EvidenceQuotesJson { get; set; } = "[]";
    public string EvaluatorType { get; set; } = "AI";
    public string EnrichmentStatus { get; set; } = InterviewScoreEnrichmentStatus.None;
    public string? EnrichedByModel { get; set; }
    public string? EnrichmentErrorsJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public Guid? ReplacesScoreId { get; set; }
    public InterviewCriterionScore? ReplacesScore { get; set; }
}
