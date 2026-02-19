namespace IkOtomasyon.Api.Entities;

public class InterviewSession
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public Application? Application { get; set; }
    public InterviewSessionStatus Status { get; set; } = InterviewSessionStatus.Scheduled;
    public string AiMode { get; set; } = "OFF";
    public string? AiModelName { get; set; }
    public DateTime? AiLastPlanAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public ICollection<InterviewMessage> Messages { get; set; } = new List<InterviewMessage>();
    public ICollection<InterviewScorecard> Scorecards { get; set; } = new List<InterviewScorecard>();
    public ICollection<InterviewInsight> Insights { get; set; } = new List<InterviewInsight>();
    public ICollection<InterviewPlan> Plans { get; set; } = new List<InterviewPlan>();
}
