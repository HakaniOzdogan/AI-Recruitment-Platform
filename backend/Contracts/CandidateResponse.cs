namespace IkOtomasyon.Api.Contracts;

public record CandidateResponse(
    Guid Id,
    string FullName,
    string? Email,
    string? Phone,
    string? Source,
    DateTime CreatedAt);
