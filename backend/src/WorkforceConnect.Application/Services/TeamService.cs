using Microsoft.EntityFrameworkCore;
using WorkforceConnect.Application.Common;
using WorkforceConnect.Application.DTOs;
using WorkforceConnect.Application.Interfaces;
using WorkforceConnect.Application.Security;
using WorkforceConnect.Domain.Entities;
using WorkforceConnect.Domain.Security;

namespace WorkforceConnect.Application.Services;

/// <summary>Teams are database rows. Adding "QA" to NNX is a POST; the UI simply re-reads the list.</summary>
public sealed class TeamService(IApplicationDbContext db, IDataScopeService scopes) : ITeamService
{
    public async Task<IReadOnlyList<TeamResponse>> ListByOrganizationAsync(int organizationId, CancellationToken ct = default)
    {
        var scope = await scopes.GetAsync(ct);

        var organizationExists = await db.Organizations
            .AsNoTracking()
            .AnyAsync(o => o.Id == organizationId && o.IsActive, ct);

        if (!organizationExists || !scope.CanViewOrganization(organizationId))
        {
            throw new NotFoundException("Organization not found.");
        }

        var teams = await scope
            .Filter(db.Teams.AsNoTracking().Where(t => t.OrganizationId == organizationId && t.IsActive))
            .OrderBy(t => t.Id)
            .ToListAsync(ct);

        return await ToResponsesAsync(teams, scope, ct);
    }

    public async Task<TeamResponse> GetAsync(int id, CancellationToken ct = default)
    {
        var scope = await scopes.GetAsync(ct);
        var team = await FindAccessibleTeamAsync(id, scope, track: false, ct);
        return (await ToResponsesAsync(new[] { team }, scope, ct))[0];
    }

    public async Task<TeamResponse> CreateAsync(CreateTeamRequest request, CancellationToken ct = default)
    {
        var scope = await scopes.GetAsync(ct);

        var organizationId = request.OrganizationId;
        var organizationExists = await db.Organizations.AnyAsync(o => o.Id == organizationId && o.IsActive, ct);
        if (!organizationExists || !scope.CanViewOrganization(organizationId))
        {
            throw new NotFoundException("Organization not found.");
        }

        if (!scope.CanAdministerOrganization(organizationId))
        {
            throw new ForbiddenException("You cannot add teams to this organization.");
        }

        var name = request.Name.Trim();
        var description = request.Description.NullIfBlank();

        var existing = await db.Teams.FirstOrDefaultAsync(t => t.OrganizationId == organizationId && t.Name == name, ct);
        if (existing is not null)
        {
            if (existing.IsActive)
            {
                throw new ConflictException($"A team named '{existing.Name}' already exists in this organization.");
            }

            // A previously deleted team with the same name is restored rather than duplicated.
            existing.IsActive = true;
            existing.Description = description;
            await db.SaveChangesAsync(ct);
            return (await ToResponsesAsync(new[] { existing }, scope, ct))[0];
        }

        var team = new Team { OrganizationId = organizationId, Name = name, Description = description, IsActive = true };
        db.Teams.Add(team);
        await db.SaveChangesAsync(ct);

        return (await ToResponsesAsync(new[] { team }, scope, ct))[0];
    }

    public async Task<TeamResponse> UpdateAsync(int id, UpdateTeamRequest request, CancellationToken ct = default)
    {
        var scope = await scopes.GetAsync(ct);
        var team = await FindAccessibleTeamAsync(id, scope, track: true, ct);

        var name = request.Name.Trim();
        var teamId = team.Id;
        var organizationId = team.OrganizationId;

        if (await db.Teams.AnyAsync(t => t.OrganizationId == organizationId && t.Id != teamId && t.Name == name, ct))
        {
            throw new ConflictException($"A team named '{name}' already exists in this organization.");
        }

        team.Name = name;
        team.Description = request.Description.NullIfBlank();
        await db.SaveChangesAsync(ct);

        return (await ToResponsesAsync(new[] { team }, scope, ct))[0];
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var scope = await scopes.GetAsync(ct);
        var team = await FindAccessibleTeamAsync(id, scope, track: true, ct);

        var teamId = team.Id;
        if (await db.Employees.AnyAsync(e => e.TeamId == teamId && e.IsActive, ct))
        {
            throw new ConflictException("This team still has active employees. Move or remove them first.");
        }

        team.IsActive = false;
        await db.SaveChangesAsync(ct);
    }

    private async Task<Team> FindAccessibleTeamAsync(int id, DataScope scope, bool track, CancellationToken ct)
    {
        var query = track ? db.Teams.AsQueryable() : db.Teams.AsNoTracking();

        var team = await query
            .Include(t => t.Organization)
            .FirstOrDefaultAsync(t => t.Id == id && t.IsActive && t.Organization.IsActive, ct);

        if (team is null || !scope.CanAccessTeam(team.Id, team.OrganizationId))
        {
            throw new NotFoundException("Team not found.");
        }

        return team;
    }

    private async Task<IReadOnlyList<TeamResponse>> ToResponsesAsync(
        IReadOnlyList<Team> teams,
        DataScope scope,
        CancellationToken ct)
    {
        if (teams.Count == 0)
        {
            return Array.Empty<TeamResponse>();
        }

        var teamIds = teams.Select(t => t.Id).ToList();
        var organizationIds = teams.Select(t => t.OrganizationId).Distinct().ToList();

        var organizationNames = await db.Organizations
            .AsNoTracking()
            .Where(o => organizationIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, o => o.Name, ct);

        var primaryCounts = await scope
            .Filter(db.Employees.AsNoTracking().Where(e => e.IsActive && e.TeamId.HasValue && teamIds.Contains(e.TeamId.Value)))
            .GroupBy(e => e.TeamId!.Value)
            .Select(g => new { TeamId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TeamId, x => x.Count, ct);

        var secondaryPairs = await scope
            .Filter(db.Employees.AsNoTracking().Where(e => e.IsActive))
            .SelectMany(e => e.SecondaryTeams.Where(t => teamIds.Contains(t.Id)).Select(t => new { e.Id, TeamId = t.Id }))
            .ToListAsync(ct);

        var employeeCounts = teamIds.ToDictionary(
            tid => tid,
            tid =>
            {
                var primary = primaryCounts.GetValueOrDefault(tid);
                var secondary = secondaryPairs.Count(p => p.TeamId == tid);
                return primary + secondary;
            });

        var projectLeadsRaw = await db.Users
            .AsNoTracking()
            .Where(u =>
                u.IsActive &&
                u.EmployeeId.HasValue &&
                u.Employee!.TeamId.HasValue &&
                teamIds.Contains(u.Employee.TeamId.Value) &&
                u.Role.Name == Domain.Security.SystemRoles.TeamLead)
            .Select(u => new
            {
                TeamId = u.Employee!.TeamId!.Value,
                u.EmployeeId,
                FullName = u.Employee.FirstName + " " + u.Employee.LastName
            })
            .ToListAsync(ct);

        var projectLeads = projectLeadsRaw
            .GroupBy(x => x.TeamId)
            .ToDictionary(g => g.Key, g => g.First());

        return teams
            .Select(t =>
            {
                var lead = projectLeads.GetValueOrDefault(t.Id);
                return new TeamResponse(
                    t.Id,
                    t.OrganizationId,
                    organizationNames.GetValueOrDefault(t.OrganizationId, string.Empty),
                    t.Name,
                    t.Description,
                    t.IsActive,
                    employeeCounts.GetValueOrDefault(t.Id),
                    lead?.EmployeeId,
                    lead?.FullName,
                    t.CreatedAt,
                    t.UpdatedAt);
            })
            .ToList();
    }
}
