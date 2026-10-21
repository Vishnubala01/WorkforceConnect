namespace WorkforceConnect.Domain.Entities;

public class Permission
{
    public int Id { get; set; }

    /// <summary>Unique name in the form Module.Action, for example Organization.Create.</summary>
    public string Name { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
