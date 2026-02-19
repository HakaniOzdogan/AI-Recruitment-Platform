namespace IkOtomasyon.Api.Entities;

public class JobSkillWeight
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public JobPosting? Job { get; set; }
    public string SkillNameNormalized { get; set; } = string.Empty;
    public double Weight { get; set; }
    public bool IsRequired { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
