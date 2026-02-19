namespace IkOtomasyon.Api.Authorization;

public static class PermissionKeys
{
    public static readonly string[] All =
    [
        UserManage,
        RoleManage,
        JobCreate,
        JobPublish,
        CandidateManage,
        ApplicationStageUpdate,
        ApplicationApply,
        InterviewParticipate
    ];

    public const string UserManage = "USER_MANAGE";
    public const string RoleManage = "ROLE_MANAGE";
    public const string JobCreate = "JOB_CREATE";
    public const string JobPublish = "JOB_PUBLISH";
    public const string CandidateManage = "CANDIDATE_MANAGE";
    public const string ApplicationStageUpdate = "APPLICATION_STAGE_UPDATE";
    public const string ApplicationApply = "APPLICATION_APPLY";
    public const string InterviewParticipate = "INTERVIEW_PARTICIPATE";
}
