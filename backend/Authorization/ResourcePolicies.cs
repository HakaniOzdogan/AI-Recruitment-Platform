namespace IkOtomasyon.Api.Authorization;

public static class ResourcePolicies
{
    public const string CanReadJob = "Resource.CanReadJob";
    public const string CanManageJob = "Resource.CanManageJob";
    public const string CanReadCandidate = "Resource.CanReadCandidate";
    public const string CanEditCandidate = "Resource.CanEditCandidate";
    public const string CanApplyApplication = "Resource.CanApplyApplication";
    public const string CanReadApplication = "Resource.CanReadApplication";
    public const string CanManageApplication = "Resource.CanManageApplication";
    public const string CanAccessInterview = "Resource.CanAccessInterview";
    public const string CanAccessScorecard = "Resource.CanAccessScorecard";
    public const string CanOverrideScorecard = "Resource.CanOverrideScorecard";
    public const string CanRunAiEvaluation = "Resource.CanRunAiEvaluation";
    public const string CanParseCvDocument = "Resource.CanParseCvDocument";
}
