namespace WorkforceConnect.Domain.Entities;

public class Team : AuditableEntity
{
    public int OrganizationId { get; set; }

    public Organization Organization { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
