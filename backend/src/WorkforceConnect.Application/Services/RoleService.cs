using Microsoft.EntityFrameworkCore;
using WorkforceConnect.Application.DTOs;
using WorkforceConnect.Application.Interfaces;

namespace WorkforceConnect.Application.Services;

public sealed class RoleService(IApplicationDbContext db) : IRoleService
{
    public async Task<IReadOnlyList<RoleResponse>> ListRolesAsync(CancellationToken ct = default)
    {
        var roles = await db.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Id)
            .ToListAsync(ct);

        var userCounts = await db.Users
            .AsNoTracking()
            .GroupBy(u => u.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, ct);

        return roles
            .Select(r => new RoleResponse(
                r.Id,
                r.Name,
                r.Description,
                r.DataScope.ToString(),
                r.IsSystem,
                userCounts.GetValueOrDefault(r.Id),
                r.RolePermissions
                    .Select(rp => rp.Permission.Name)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToList()))
            .ToList();
    }

    public async Task<IReadOnlyList<PermissionResponse>> ListPermissionsAsync(CancellationToken ct = default)
    {
        return await db.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .Select(p => new PermissionResponse(p.Id, p.Name, p.Module, p.Description))
            .ToListAsync(ct);
    }
}
