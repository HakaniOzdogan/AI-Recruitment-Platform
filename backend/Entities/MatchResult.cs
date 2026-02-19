namespace IkOtomasyon.Api.Entities;

public class MatchResult
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public JobPosting? Job { get; set; }
    public Guid CandidateId { get; set; }
    public Candidate? Candidate { get; set; }
    public int Score { get; set; }
    public string ReasonsJson { get; set; } = "[]";
    public string GapsJson { get; set; } = "[]";
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
    public Guid? ComputedByUserId { get; set; }
}
