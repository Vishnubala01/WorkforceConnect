using System.ComponentModel.DataAnnotations;

namespace WorkforceConnect.Application.DTOs;

public sealed class CreateTeamRequest
{
    [Range(1, int.MaxValue)]
    public int OrganizationId { get; init; }

    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }
}

public sealed class UpdateTeamRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }
}

public sealed record TeamResponse(
    int Id,
    int OrganizationId,
    string OrganizationName,
    string Name,
    string? Description,
    bool IsActive,
    int EmployeeCount,
    int? ProjectLeadId,
    string? ProjectLeadName,
    DateTime CreatedAt,
    DateTime UpdatedAt);
