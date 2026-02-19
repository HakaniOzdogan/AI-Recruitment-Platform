namespace IkOtomasyon.Api.Entities;

public class InterviewInsight
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public InterviewSession? Session { get; set; }
    public int TurnIndex { get; set; }
    public string SignalsJson { get; set; } = "[]";
    public string CompetencyJson { get; set; } = "{}";
    public string DepthJson { get; set; } = "{}";
    public string RiskFlagsJson { get; set; } = "{}";
    public string EvidenceSnippetsJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
