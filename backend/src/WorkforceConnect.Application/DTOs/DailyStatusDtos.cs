using System.ComponentModel.DataAnnotations;

namespace WorkforceConnect.Application.DTOs;

public sealed class SaveDailyStatusRequest
{
    [Required]
    public DateTime Date { get; init; }

    public int? EmployeeId { get; init; }

    public int? OrganizationId { get; init; }

    public int? TeamId { get; init; }

    [StringLength(150)]
    public string? ProjectOrTeamName { get; init; }

    [StringLength(100)]
    public string? WorkItemId { get; init; }

    [Required, StringLength(500)]
    public string TaskActivity { get; init; } = string.Empty;

    [Required, StringLength(2000)]
    public string PlannedWorkForToday { get; init; } = string.Empty;

    [Required, StringLength(2000)]
    public string WorkCompletedProgress { get; init; } = string.Empty;

    [Required, StringLength(50)]
    public string Status { get; init; } = "In Progress";

    [Range(0, 24)]
    public decimal HoursSpent { get; init; }

    [StringLength(1000)]
    public string? BlockerDependency { get; init; }

    [StringLength(1000)]
    public string? SupportRequired { get; init; }

    [StringLength(150)]
    public string? ReviewerLead { get; init; }

    [StringLength(1000)]
    public string? Remarks { get; init; }
}

public sealed class DailyStatusQuery
{
    public DateTime? FromDate { get; init; }

    public DateTime? ToDate { get; init; }

    public int? EmployeeId { get; init; }

    public int? OrganizationId { get; init; }

    public int? TeamId { get; init; }

    public string? Status { get; init; }

    public string? Search { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 50;
}

public sealed record DailyStatusResponse(
    int Id,
    DateTime Date,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string EmployeeEmail,
    int OrganizationId,
    string OrganizationName,
    int? TeamId,
    string? TeamName,
    string? ProjectOrTeamName,
    string? WorkItemId,
    string TaskActivity,
    string PlannedWorkForToday,
    string WorkCompletedProgress,
    string Status,
    decimal HoursSpent,
    string? BlockerDependency,
    string? SupportRequired,
    string? ReviewerLead,
    string? Remarks,
    DateTime CreatedAt,
    DateTime UpdatedAt);
