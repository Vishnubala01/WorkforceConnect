namespace WorkforceConnect.Domain.Entities;

/// <summary>Join entity for the many-to-many relationship between roles and permissions.</summary>
public class RolePermission
{
    public int RoleId { get; set; }

    public Role Role { get; set; } = null!;

    public int PermissionId { get; set; }

    public Permission Permission { get; set; } = null!;
}
