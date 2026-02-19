using IkOtomasyon.Api.Data;
using IkOtomasyon.Api.Authorization;
using Microsoft.EntityFrameworkCore;

namespace IkOtomasyon.Api.Services;

public class PermissionService
{
    private readonly AppDbContext _db;
    private readonly AuthorizationRuntimeState _authorizationState;

    public PermissionService(AppDbContext db, AuthorizationRuntimeState authorizationState)
    {
        _db = db;
        _authorizationState = authorizationState;
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string permissionKey, CancellationToken ct = default)
    {
        if (!_authorizationState.Enforced)
        {
            return true;
        }

        return await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role!.RolePermissions)
            .AnyAsync(rp => rp.Permission!.Key == permissionKey, ct);
    }
}
