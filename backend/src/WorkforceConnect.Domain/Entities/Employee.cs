namespace WorkforceConnect.Domain.Entities;

public class Employee : AuditableEntity
{
    public string EmployeeCode { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? JobTitle { get; set; }

    public int OrganizationId { get; set; }

    public Organization Organization { get; set; } = null!;

    /// <summary>Optional: an employee belongs to an organization and may belong to one of its teams.</summary>
    public int? TeamId { get; set; }

    public Team? Team { get; set; }

    public bool IsActive { get; set; } = true;

    public User? User { get; set; }

    public ICollection<Organization> SecondaryOrganizations { get; set; } = new List<Organization>();

    public ICollection<Team> SecondaryTeams { get; set; } = new List<Team>();

    public string FullName => $"{FirstName} {LastName}".Trim();
}
