namespace IkOtomasyon.Api.Services;

public static class AuditActions
{
    public const string JobPublish = "JOB_PUBLISH";
    public const string JobClose = "JOB_CLOSE";
    public const string CandidateCreate = "CANDIDATE_CREATE";
    public const string CandidateUpdate = "CANDIDATE_UPDATE";
    public const string ApplicationCreate = "APP_CREATE";
    public const string ApplicationStageChange = "APP_STAGE_CHANGE";
    public const string InterviewStart = "INTERVIEW_START";
    public const string CvUpload = "CV_UPLOAD";
    public const string CvParse = "CV_PARSE";
    public const string MatchRecompute = "MATCH_RECOMPUTE";
    public const string JobWeightsUpdate = "JOB_WEIGHTS_UPDATE";
    public const string AiEvaluateRun = "AI_EVALUATE_RUN";
    public const string AutoScoreRun = "AUTO_SCORE_RUN";
    public const string HumanScoreOverride = "HUMAN_SCORE_OVERRIDE";
    public const string InterviewAiModeChange = "INTERVIEW_AI_MODE_CHANGE";
    public const string InterviewAiQuestion = "INTERVIEW_AI_QUESTION";
    public const string LlmGuardrailBlock = "LLM_GUARDRAIL_BLOCK";
    public const string LlmFallbackUsed = "LLM_FALLBACK_USED";
    public const string LlmScoreEnriched = "LLM_SCORE_ENRICHED";
    public const string LlmScoreEnrichSkip = "LLM_SCORE_ENRICH_SKIP";
    public const string LlmScoreEnrichFail = "LLM_SCORE_ENRICH_FAIL";
    public const string LlmScoreEnrichGuardrailBlock = "LLM_SCORE_ENRICH_GUARDRAIL_BLOCK";
}
