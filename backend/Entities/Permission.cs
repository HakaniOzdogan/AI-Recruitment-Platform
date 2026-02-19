namespace IkOtomasyon.Api.Entities;

public class Permission
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
