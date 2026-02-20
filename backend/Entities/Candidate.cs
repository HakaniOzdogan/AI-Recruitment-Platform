namespace IkOtomasyon.Api.Entities;

public class Candidate
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? OwnerUserId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Source { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Application> Applications { get; set; } = new List<Application>();
    public ICollection<CvDocument> CvDocuments { get; set; } = new List<CvDocument>();
    public CandidateProfile? Profile { get; set; }
    public CandidateConsent? Consent { get; set; }
    public ICollection<AiEvaluationReport> AiEvaluationReports { get; set; } = new List<AiEvaluationReport>();
}
