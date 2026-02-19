namespace IkOtomasyon.Api.Entities;

public class InterviewScorecard
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public InterviewSession? Session { get; set; }
    public Guid RubricTemplateId { get; set; }
    public RubricTemplate? RubricTemplate { get; set; }
    public double? OverallScore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<InterviewCriterionScore> CriterionScores { get; set; } = new List<InterviewCriterionScore>();
}
