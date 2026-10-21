namespace WorkforceConnect.Domain.Entities;

public class Organization : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Team> Teams { get; set; } = new List<Team>();

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
