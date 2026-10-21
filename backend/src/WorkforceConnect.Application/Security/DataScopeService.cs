using Microsoft.EntityFrameworkCore;
using WorkforceConnect.Application.Common;
using WorkforceConnect.Application.Interfaces;
using WorkforceConnect.Domain.Enums;

namespace WorkforceConnect.Application.Security;

public sealed class DataScopeService(IApplicationDbContext db, ICurrentUser currentUser) : IDataScopeService
{
    private DataScope? _cached;

    public async Task<DataScope> GetAsync(CancellationToken ct = default)
    {
        _cached ??= await BuildAsync(currentUser.UserId, ct);
        return _cached;
    }

    public Task<DataScope> GetForUserAsync(int userId, CancellationToken ct = default) =>
        BuildAsync(userId, ct);

    private async Task<DataScope> BuildAsync(int userId, CancellationToken ct)
    {
        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.Employee)
            .Include(u => u.OrganizationAccess)
            .Include(u => u.TeamAccess)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, ct)
            ?? throw new UnauthenticatedException();

        var level = user.Role.DataScope;
        var organizationIds = new HashSet<int>();
        var teamIds = new HashSet<int>();

        switch (level)
        {
            case DataScopeLevel.All:
                // Unrestricted; the id sets are not consulted.
                break;

            case DataScopeLevel.Organization:
                organizationIds.UnionWith(user.OrganizationAccess.Select(a => a.OrganizationId));
                teamIds.UnionWith(user.TeamAccess.Select(a => a.TeamId));
                if (user.Employee is not null)
                {
                    organizationIds.Add(user.Employee.OrganizationId);
                }

                break;

            case DataScopeLevel.Team:
                teamIds.UnionWith(user.TeamAccess.Select(a => a.TeamId));
                if (user.Employee?.TeamId is int ownTeamId)
                {
                    teamIds.Add(ownTeamId);
                }

                // Cross-functional teams (QA and DevOps) within the user's organization are visible
                if (user.Employee?.OrganizationId is int orgId)
                {
                    var crossFunctional = await db.Teams
                        .AsNoTracking()
                        .Where(t => t.OrganizationId == orgId && t.IsActive && (t.Name == "QA" || t.Name == "DevOps"))
                        .Select(t => t.Id)
                        .ToListAsync(ct);
                    teamIds.UnionWith(crossFunctional);
                }

                break;

            case DataScopeLevel.Self:
                // Only the caller's own employee record; nothing else is added.
                break;
        }

        var organizationIdsOfTeams = new List<int>();
        if (level != DataScopeLevel.All && teamIds.Count > 0)
        {
            var ids = teamIds.ToList();
            organizationIdsOfTeams = await db.Teams
                .AsNoTracking()
                .Where(t => ids.Contains(t.Id))
                .Select(t => t.OrganizationId)
                .Distinct()
                .ToListAsync(ct);
        }

        return new DataScope(level, user.EmployeeId, organizationIds, teamIds, organizationIdsOfTeams);
    }
}
