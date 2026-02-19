using IkOtomasyon.Api.Entities;

namespace IkOtomasyon.Api.Contracts;

public record AdminUserResponse(
    Guid Id,
    string FullName,
    string Email,
    UserStatus Status,
    DateTime CreatedAt,
    IReadOnlyCollection<string> Roles);

public class AdminUserCreateRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserStatus Status { get; set; } = UserStatus.Active;
    public List<Guid> RoleIds { get; set; } = [];
}

public class AdminUserUpdateRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public List<Guid> RoleIds { get; set; } = [];
}

public record AdminRoleResponse(
    Guid Id,
    string Name,
    IReadOnlyCollection<string> Permissions,
    int UserCount);

public class AdminRoleUpsertRequest
{
    public string Name { get; set; } = string.Empty;
    public List<string> PermissionKeys { get; set; } = [];
}

public record AdminPipelineStageResponse(
    Guid Id,
    string Name,
    int Order,
    bool IsTerminal,
    int ApplicationCount);

public class AdminPipelineStageUpsertRequest
{
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsTerminal { get; set; }
}
