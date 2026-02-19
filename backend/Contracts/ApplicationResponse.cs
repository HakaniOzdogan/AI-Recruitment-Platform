using IkOtomasyon.Api.Entities;

namespace IkOtomasyon.Api.Contracts;

public record ApplicationResponse(
    Guid Id,
    Guid JobId,
    string JobTitle,
    Guid CandidateId,
    string CandidateFullName,
    Guid StageId,
    string StageName,
    ApplicationStatus Status,
    DateTime AppliedAt,
    DateTime UpdatedAt,
    Guid LastUpdatedByUserId,
    Guid? InterviewSessionId);
