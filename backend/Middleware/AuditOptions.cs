namespace IkOtomasyon.Api.Middleware;

public class AuditOptions
{
    public const string SectionName = "Audit";

    public bool LogUnauthorized { get; set; } = true;
}
