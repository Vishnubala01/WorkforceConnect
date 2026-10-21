namespace WorkforceConnect.Domain.Entities;

public class User : AuditableEntity
{
    public int? EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>A real relationship to the Roles table, never a free-text role name.</summary>
    public int RoleId { get; set; }

    public Role Role { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAt { get; set; }

    /// <summary>Organizations assigned to this user (used by roles with Organization data scope).</summary>
    public ICollection<UserOrganizationAccess> OrganizationAccess { get; set; } = new List<UserOrganizationAccess>();

    /// <summary>Teams assigned to this user (used by roles with Team or Organization data scope).</summary>
    public ICollection<UserTeamAccess> TeamAccess { get; set; } = new List<UserTeamAccess>();
}
