namespace IkOtomasyon.Api.Contracts;

public record CvDocumentResponse(
    Guid Id,
    Guid CandidateId,
    string OriginalFileName,
    string FileType,
    long FileSize,
    DateTime UploadedAt,
    Guid UploadedByUserId,
    string ParseStatus,
    string? ParseError,
    DateTime? ParsedAt);
