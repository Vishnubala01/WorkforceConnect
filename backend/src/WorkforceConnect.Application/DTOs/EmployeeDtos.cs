using System.ComponentModel.DataAnnotations;

namespace WorkforceConnect.Application.DTOs;

public sealed class SaveEmployeeRequest
{
    [Required, StringLength(30, MinimumLength = 1)]
    [RegularExpression(@"^[A-Za-z0-9_\-]+$", ErrorMessage = "Employee code may contain letters, digits, '-' and '_' only.")]
    public string EmployeeCode { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string FirstName { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string LastName { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int OrganizationId { get; init; }

    public int? TeamId { get; init; }

    public int? RoleId { get; init; }

    [StringLength(100)]
    public string? JobTitle { get; init; }

    public List<int> SecondaryOrganizationIds { get; init; } = new();

    public List<int> SecondaryTeamIds { get; init; } = new();
}

public sealed class EmployeeListQuery
{
    public int? OrganizationId { get; init; }

    public int? TeamId { get; init; }

    public string? Search { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}

public sealed record EmployeeResponse(
    int Id,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string? JobTitle,
    int OrganizationId,
    string OrganizationName,
    int? TeamId,
    string? TeamName,
    string? RoleName,
    string? ProjectLeadName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<int>? SecondaryOrganizationIds = null,
    List<int>? SecondaryTeamIds = null);

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Total,
    int Page,
    int PageSize);
