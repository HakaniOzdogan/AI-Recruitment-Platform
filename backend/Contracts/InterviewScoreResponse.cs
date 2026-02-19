namespace IkOtomasyon.Api.Contracts;

public record InterviewScoreResponse(
    Guid ScorecardId,
    Guid SessionId,
    Guid RubricTemplateId,
    double? OverallScore,
    bool InsufficientAll,
    string LlmEnrichment,
    IReadOnlyCollection<string> LlmEnrichmentEvents,
    DateTime CreatedAt,
    IReadOnlyCollection<InterviewCriterionLatestResponse> Latest,
    IReadOnlyCollection<InterviewCriterionHistoryResponse> History);

public record InterviewCriterionLatestResponse(
    string CriterionKey,
    string Status,
    double? Score,
    string? Rationale,
    IReadOnlyCollection<EvidenceQuoteResponse> EvidenceQuotes,
    string EvaluatorType,
    string EnrichmentStatus,
    string? EnrichedByModel,
    IReadOnlyCollection<string> EnrichmentErrors,
    DateTime CreatedAt,
    Guid? CreatedByUserId);

public record InterviewCriterionHistoryResponse(
    Guid Id,
    string CriterionKey,
    string Status,
    double? Score,
    string? Rationale,
    IReadOnlyCollection<EvidenceQuoteResponse> EvidenceQuotes,
    string EvaluatorType,
    string EnrichmentStatus,
    string? EnrichedByModel,
    IReadOnlyCollection<string> EnrichmentErrors,
    DateTime CreatedAt,
    Guid? CreatedByUserId,
    Guid? ReplacesScoreId);

public record EvidenceQuoteResponse(
    string Quote,
    string RelatedTo);
