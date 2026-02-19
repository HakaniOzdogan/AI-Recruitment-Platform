using IkOtomasyon.Api.Entities;

namespace IkOtomasyon.Api.Contracts;

public record ParseCvResponse(
    Guid CvDocumentId,
    Guid CandidateId,
    CvParseStatus ParseStatus,
    string? ParseError,
    DateTime? ParsedAt,
    CandidateProfileResponse? Profile);
