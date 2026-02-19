namespace IkOtomasyon.Api.Entities;

public class CandidateProfile
{
    public Guid CandidateId { get; set; }
    public Candidate? Candidate { get; set; }
    public string FullNameSnapshot { get; set; } = string.Empty;
    public string? EmailSnapshot { get; set; }
    public string? Summary { get; set; }
    public int? TotalExperienceMonths { get; set; }
    public string SkillsJson { get; set; } = "[]";
    public string EducationJson { get; set; } = "[]";
    public string ExperienceJson { get; set; } = "[]";
    public string LanguagesJson { get; set; } = "[]";
    public string LinksJson { get; set; } = "[]";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
