namespace IkOtomasyon.Api.Entities;

public class CvDocument
{
    public Guid Id { get; set; }
    public Guid CandidateId { get; set; }
    public Candidate? Candidate { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public Guid UploadedByUserId { get; set; }
    public CvParseStatus ParseStatus { get; set; } = CvParseStatus.Pending;
    public string? ParseError { get; set; }
    public DateTime? ParsedAt { get; set; }
}
