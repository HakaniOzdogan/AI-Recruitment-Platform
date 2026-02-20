using System.Security.Claims;
using IkOtomasyon.Api.Authorization;

namespace IkOtomasyon.Api.Services;

public static class UserIdentityExtensions
{
    public static Guid? TryGetUserId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    public static Guid? TryGetTenantId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue("tenantId");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    public static bool HasAnyRole(this ClaimsPrincipal user, params string[] roles)
    {
        var userRoles = user.FindAll(ClaimTypes.Role)
            .Select(x => x.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (userRoles.Count == 0)
        {
            return false;
        }

        return roles.Any(role => userRoles.Contains(role));
    }

    public static bool IsApplicant(this ClaimsPrincipal user)
        => user.HasAnyRole(RoleKeys.Applicant, RoleKeys.LegacyUser);

    public static bool IsRecruiter(this ClaimsPrincipal user)
        => user.HasAnyRole(RoleKeys.Recruiter, RoleKeys.LegacyHr);
}
