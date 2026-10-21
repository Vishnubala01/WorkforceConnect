namespace WorkforceConnect.Domain.Entities;

/// <summary>Assigns an organization to a user. Drives data scope, not permissions.</summary>
public class UserOrganizationAccess
{
    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public int OrganizationId { get; set; }

    public Organization Organization { get; set; } = null!;
}

/// <summary>Assigns a team to a user. Drives data scope, not permissions.</summary>
public class UserTeamAccess
{
    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public int TeamId { get; set; }

    public Team Team { get; set; } = null!;
}
