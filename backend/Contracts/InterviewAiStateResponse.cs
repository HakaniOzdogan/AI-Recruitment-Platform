namespace IkOtomasyon.Api.Contracts;

public record InterviewAiStateResponse(
    Guid SessionId,
    string AiMode,
    string? AiModelName,
    string? AiProvider,
    DateTime? AiLastPlanAt,
    long? LastAnalyzeLatencyMs,
    long? LastPlanLatencyMs,
    string? LatestInsightJson,
    string? LatestPlanJson,
    bool FallbackUsed,
    bool SchemaValid,
    bool GuardrailBlocked);
