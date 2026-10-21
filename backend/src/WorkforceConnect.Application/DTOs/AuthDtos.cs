using System.ComponentModel.DataAnnotations;

namespace WorkforceConnect.Application.DTOs;

public sealed class LoginRequest
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(200)]
    public string Password { get; init; } = string.Empty;
}

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAt,
    CurrentUserResponse User);

/// <summary>Everything the frontend needs to build its UI: identity, role, permissions, data scope, org/team.</summary>
public sealed record CurrentUserResponse(
    int Id,
    string Name,
    string Email,
    string Role,
    IReadOnlyList<string> Permissions,
    DataScopeResponse DataScope,
    int? EmployeeId,
    NamedRef? Organization,
    NamedRef? Team);

public sealed record DataScopeResponse(
    string Level,
    IReadOnlyList<int> OrganizationIds,
    IReadOnlyList<int> TeamIds);

public sealed record NamedRef(int Id, string Name);
