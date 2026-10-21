using Microsoft.EntityFrameworkCore;
using WorkforceConnect.Application.Interfaces;

namespace WorkforceConnect.Application.Security;

/// <summary>
/// Reads a user's permissions from User -> Role -> RolePermissions -> Permissions on every request
/// (cached only for the lifetime of that request), so changes to a role or deactivating a user
/// take effect immediately instead of when a token expires.
/// </summary>
public sealed class PermissionChecker(IApplicationDbContext db) : IPermissionChecker
{
    private readonly Dictionary<int, HashSet<string>> _cache = new();

    public async Task<IReadOnlySet<string>> GetPermissionsAsync(int userId, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(userId, out var cached))
        {
            return cached;
        }

        var names = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && u.IsActive)
            .SelectMany(u => u.Role.RolePermissions.Select(rp => rp.Permission.Name))
            .ToListAsync(ct);

        var set = new HashSet<string>(names, StringComparer.Ordinal);
        _cache[userId] = set;
        return set;
    }

    public async Task<bool> HasPermissionAsync(int userId, string permission, CancellationToken ct = default)
    {
        var permissions = await GetPermissionsAsync(userId, ct);
        return permissions.Contains(permission);
    }
}
