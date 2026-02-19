using System.Security.Claims;
using IkOtomasyon.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace IkOtomasyon.Api.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly PermissionService _permissionService;
    private readonly AuthorizationRuntimeState _authorizationState;

    public PermissionAuthorizationHandler(PermissionService permissionService, AuthorizationRuntimeState authorizationState)
    {
        _permissionService = permissionService;
        _authorizationState = authorizationState;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!_authorizationState.Enforced)
        {
            context.Succeed(requirement);
            return;
        }

        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return;
        }

        if (await _permissionService.HasPermissionAsync(userId, requirement.PermissionKey))
        {
            context.Succeed(requirement);
        }
    }
}
