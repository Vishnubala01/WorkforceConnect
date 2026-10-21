using WorkforceConnect.Domain.Enums;

namespace WorkforceConnect.Domain.Entities;

public class Role : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>What data users with this role can reach (see <see cref="DataScopeLevel"/>).</summary>
    public DataScopeLevel DataScope { get; set; } = DataScopeLevel.Self;

    /// <summary>System roles are created by the seeder and are protected from deletion.</summary>
    public bool IsSystem { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    public ICollection<User> Users { get; set; } = new List<User>();
}
