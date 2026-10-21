namespace WorkforceConnect.Application.DTOs;

public sealed record RoleResponse(
    int Id,
    string Name,
    string? Description,
    string DataScope,
    bool IsSystem,
    int UserCount,
    IReadOnlyList<string> Permissions);

public sealed record PermissionResponse(
    int Id,
    string Name,
    string Module,
    string? Description);
