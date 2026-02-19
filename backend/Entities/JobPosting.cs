namespace IkOtomasyon.Api.Entities;

public class JobPosting
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Location { get; set; }
    public string? EmploymentType { get; set; }
    public string Description { get; set; } = string.Empty;
    public JobPostingStatus Status { get; set; } = JobPostingStatus.Draft;
    public string RequiredSkillsJson { get; set; } = "[]";
    public string NiceToHaveSkillsJson { get; set; } = "[]";
    public string? CompetencyWeightsJson { get; set; }
    public int? MinExperienceMonths { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public ICollection<Application> Applications { get; set; } = new List<Application>();
    public ICollection<MatchResult> MatchResults { get; set; } = new List<MatchResult>();
    public ICollection<AiEvaluationReport> AiEvaluationReports { get; set; } = new List<AiEvaluationReport>();
    public ICollection<JobSkillWeight> SkillWeights { get; set; } = new List<JobSkillWeight>();
    public ICollection<RubricTemplate> RubricTemplates { get; set; } = new List<RubricTemplate>();
}
