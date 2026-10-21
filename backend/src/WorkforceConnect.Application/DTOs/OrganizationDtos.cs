using System.ComponentModel.DataAnnotations;

namespace WorkforceConnect.Application.DTOs;

public sealed class SaveOrganizationRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }
}

/// <summary>Team and employee counts only include what the caller's data scope allows them to see.</summary>
public sealed record OrganizationResponse(
    int Id,
    string Name,
    string? Description,
    bool IsActive,
    int TeamCount,
    int EmployeeCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);
