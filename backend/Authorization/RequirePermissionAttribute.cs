using Microsoft.AspNetCore.Authorization;

namespace IkOtomasyon.Api.Authorization;

public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permissionKey)
    {
        Policy = $"{PermissionPolicyProvider.PolicyPrefix}{permissionKey}";
    }
}
