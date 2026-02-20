using Microsoft.AspNetCore.Authorization;

namespace IkOtomasyon.Api.Authorization;

public sealed class ResourceAuthorizationRequirement : IAuthorizationRequirement
{
    public ResourceAuthorizationRequirement(string policyName)
    {
        PolicyName = policyName;
    }

    public string PolicyName { get; }
}
