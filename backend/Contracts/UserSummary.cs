using IkOtomasyon.Api.Entities;

namespace IkOtomasyon.Api.Contracts;

public record UserSummary(Guid Id, string FullName, string Email, UserStatus Status);
