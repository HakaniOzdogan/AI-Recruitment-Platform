namespace IkOtomasyon.Api.Contracts;

public record InterviewTurnResponse(
    Guid SessionId,
    Guid CandidateMessageId,
    Guid? SystemMessageId,
    string? SystemQuestion,
    string AiMode,
    bool UsedFallback,
    bool PlanGenerated,
    bool GuardrailBlocked);
