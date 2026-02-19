namespace IkOtomasyon.Api.Services;

public record CvStoredFile(
    string StoredFileName,
    string StoragePath,
    string FileType,
    long FileSize,
    string OriginalFileName);
