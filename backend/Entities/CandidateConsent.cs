namespace IkOtomasyon.Api.Entities;

public class CandidateConsent
{
    public Guid CandidateId { get; set; }
    public Candidate? Candidate { get; set; }
    public bool ConsentGiven { get; set; }
    public string ConsentTextVersion { get; set; } = string.Empty;
    public DateTime ConsentAt { get; set; }
    public int DataRetentionDays { get; set; }
    public DateTime? DeleteRequestedAt { get; set; }
}
