namespace IkOtomasyon.Api.Authorization;

public static class RoleKeys
{
    public const string Admin = "Admin";
    public const string Recruiter = "Recruiter";
    public const string HiringManager = "HiringManager";
    public const string Interviewer = "Interviewer";
    public const string Applicant = "Applicant";

    // Legacy role aliases kept for backward compatibility with existing users/tests.
    public const string LegacyHr = "HR";
    public const string LegacyUser = "User";
}
