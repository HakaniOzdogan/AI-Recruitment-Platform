namespace IkOtomasyon.Api.Entities;

public class InterviewPlan
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public InterviewSession? Session { get; set; }
    public int FromTurnIndex { get; set; }
    public string PlannedQuestionsJson { get; set; } = "[]";
    public string PlanRationaleJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
