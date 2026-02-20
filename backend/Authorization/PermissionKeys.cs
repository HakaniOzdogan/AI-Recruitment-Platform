namespace IkOtomasyon.Api.Authorization;

public static class PermissionKeys
{
    public static readonly string[] All =
    [
        AuditRead,
        UserManage,
        RoleManage,
        JobRead,
        JobCreate,
        JobUpdate,
        JobPublish,
        JobWeightsRead,
        JobWeightsUpdate,
        CandidateRead,
        CandidateCreate,
        CandidateUpdate,
        CandidateCvUpload,
        CandidateCvParse,
        ApplicationCreate,
        ApplicationRead,
        ApplicationUpdateStage,
        InterviewRead,
        InterviewCreate,
        InterviewMessageSend,
        ScorecardRead,
        ScorecardRunAuto,
        ScorecardOverride,
        AiEvaluationRead,
        AiEvaluationRun
    ];

    public const string AuditRead = "Audit.Read";
    public const string UserManage = "User.Manage";
    public const string RoleManage = "Role.Manage";

    public const string JobRead = "Job.Read";
    public const string JobCreate = "Job.Create";
    public const string JobUpdate = "Job.Update";
    public const string JobPublish = "Job.Publish";
    public const string JobWeightsRead = "Job.Weights.Read";
    public const string JobWeightsUpdate = "Job.Weights.Update";

    public const string CandidateRead = "Candidate.Read";
    public const string CandidateCreate = "Candidate.Create";
    public const string CandidateUpdate = "Candidate.Update";
    public const string CandidateCvUpload = "Candidate.Cv.Upload";
    public const string CandidateCvParse = "Candidate.Cv.Parse";

    public const string ApplicationCreate = "Application.Create";
    public const string ApplicationRead = "Application.Read";
    public const string ApplicationUpdateStage = "Application.UpdateStage";

    public const string InterviewRead = "Interview.Read";
    public const string InterviewCreate = "Interview.Create";
    public const string InterviewMessageSend = "Interview.Message.Send";

    public const string ScorecardRead = "Scorecard.Read";
    public const string ScorecardRunAuto = "Scorecard.RunAuto";
    public const string ScorecardOverride = "Scorecard.Override";

    public const string AiEvaluationRead = "AiEvaluation.Read";
    public const string AiEvaluationRun = "AiEvaluation.Run";
}
