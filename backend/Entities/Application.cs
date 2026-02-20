namespace IkOtomasyon.Api.Entities;

public class Application
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public Guid JobId { get; set; }
    public JobPosting? Job { get; set; }
    public Guid CandidateId { get; set; }
    public Candidate? Candidate { get; set; }
    public Guid StageId { get; set; }
    public PipelineStage? Stage { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Active;
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public Guid LastUpdatedByUserId { get; set; }

    public ICollection<AiEvaluationReport> AiEvaluationReports { get; set; } = new List<AiEvaluationReport>();
    public ICollection<InterviewSession> InterviewSessions { get; set; } = new List<InterviewSession>();
}
