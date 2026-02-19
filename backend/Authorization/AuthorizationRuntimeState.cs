namespace IkOtomasyon.Api.Authorization;

public sealed class AuthorizationRuntimeState
{
    public bool Enforced { get; init; }
    public Guid? BypassUserId { get; set; }
}
