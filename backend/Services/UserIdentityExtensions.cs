using System.Security.Claims;

namespace IkOtomasyon.Api.Services;

public static class UserIdentityExtensions
{
    public static Guid? TryGetUserId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
