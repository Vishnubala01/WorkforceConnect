namespace WorkforceConnect.Domain.Entities;

public class DailyStatus : AuditableEntity
{
    public DateTime Date { get; set; }

    public int EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public int OrganizationId { get; set; }

    public Organization Organization { get; set; } = null!;

    public int? TeamId { get; set; }

    public Team? Team { get; set; }

    public string? ProjectOrTeamName { get; set; }

    public string? WorkItemId { get; set; }

    public string TaskActivity { get; set; } = string.Empty;

    public string PlannedWorkForToday { get; set; } = string.Empty;

    public string WorkCompletedProgress { get; set; } = string.Empty;

    /// <summary>Status e.g. "Completed", "In Progress", "Blocked", "On Hold", "Under Review"</summary>
    public string Status { get; set; } = "In Progress";

    public decimal HoursSpent { get; set; }

    public string? BlockerDependency { get; set; }

    public string? SupportRequired { get; set; }

    public string? ReviewerLead { get; set; }

    public string? Remarks { get; set; }
}
