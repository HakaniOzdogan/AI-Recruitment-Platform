namespace IkOtomasyon.Api.Contracts;

public record CvUploadResponse(
    Guid CvDocumentId,
    Guid CandidateId,
    string OriginalFileName,
    string FileType,
    long FileSize,
    string ParseStatus,
    DateTime UploadedAt);
