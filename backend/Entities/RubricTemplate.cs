namespace IkOtomasyon.Api.Entities;

public class RubricTemplate
{
    public Guid Id { get; set; }
    public Guid? JobId { get; set; }
    public JobPosting? Job { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RubricCriterion> Criteria { get; set; } = new List<RubricCriterion>();
    public ICollection<InterviewScorecard> Scorecards { get; set; } = new List<InterviewScorecard>();
}
