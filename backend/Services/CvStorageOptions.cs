namespace IkOtomasyon.Api.Services;

public class CvStorageOptions
{
    public const string SectionName = "CvStorage";

    public string RootPath { get; set; } = "/app/storage";
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
}
