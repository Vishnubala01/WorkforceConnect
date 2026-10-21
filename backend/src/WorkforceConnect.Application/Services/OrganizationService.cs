using Microsoft.EntityFrameworkCore;
using WorkforceConnect.Application.Common;
using WorkforceConnect.Application.DTOs;
using WorkforceConnect.Application.Interfaces;
using WorkforceConnect.Application.Security;
using WorkforceConnect.Domain.Entities;

namespace WorkforceConnect.Application.Services;

/// <summary>
/// Permissions (Organization.*) are enforced by the controller attributes. This service enforces
/// the second layer: data scope. Out-of-scope resources look exactly like missing ones (404).
/// </summary>
public sealed class OrganizationService(IApplicationDbContext db, IDataScopeService scopes) : IOrganizationService
{
    public async Task<IReadOnlyList<OrganizationResponse>> ListAsync(CancellationToken ct = default)
    {
        var scope = await scopes.GetAsync(ct);

        var organizations = await scope
            .Filter(db.Organizations.AsNoTracking().Where(o => o.IsActive))
            .OrderBy(o => o.Id)
            .ToListAsync(ct);

        return await ToResponsesAsync(organizations, scope, ct);
    }

    public async Task<OrganizationResponse> GetAsync(int id, CancellationToken ct = default)
    {
        var scope = await scopes.GetAsync(ct);

        var organization = await db.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id && o.IsActive, ct);

        if (organization is null || !scope.CanViewOrganization(id))
        {
            throw new NotFoundException("Organization not found.");
        }

        return (await ToResponsesAsync(new[] { organization }, scope, ct))[0];
    }

    public async Task<OrganizationResponse> CreateAsync(SaveOrganizationRequest request, CancellationToken ct = default)
    {
        var scope = await scopes.GetAsync(ct);
        if (!scope.IsAll)
        {
            throw new ForbiddenException("Only users with access to all organizations can create an organization.");
        }

        var name = request.Name.Trim();
        var description = request.Description.NullIfBlank();

        var existing = await db.Organizations.FirstOrDefaultAsync(o => o.Name == name, ct);
        if (existing is not null)
        {
            if (existing.IsActive)
            {
                throw new ConflictException($"An organization named '{existing.Name}' already exists.");
            }

            // A previously deleted organization with the same name is restored rather than duplicated.
            existing.IsActive = true;
            existing.Description = description;
            await db.SaveChangesAsync(ct);
            return (await ToResponsesAsync(new[] { existing }, scope, ct))[0];
        }

        var organization = new Organization { Name = name, Description = description, IsActive = true };
        db.Organizations.Add(organization);
        await db.SaveChangesAsync(ct);

        return (await ToResponsesAsync(new[] { organization }, scope, ct))[0];
    }

    public async Task<OrganizationResponse> UpdateAsync(int id, SaveOrganizationRequest request, CancellationToken ct = default)
    {
        var scope = await scopes.GetAsync(ct);

        var organization = await db.Organizations.FirstOrDefaultAsync(o => o.Id == id && o.IsActive, ct);
        if (organization is null || !scope.CanViewOrganization(id))
        {
            throw new NotFoundException("Organization not found.");
        }

        if (!scope.CanAdministerOrganization(id))
        {
            throw new ForbiddenException("You do not have access to edit this organization.");
        }

        var name = request.Name.Trim();
        if (await db.Organizations.AnyAsync(o => o.Id != id && o.Name == name, ct))
        {
            throw new ConflictException($"An organization named '{name}' already exists.");
        }

        organization.Name = name;
        organization.Description = request.Description.NullIfBlank();
        await db.SaveChangesAsync(ct);

        return (await ToResponsesAsync(new[] { organization }, scope, ct))[0];
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var scope = await scopes.GetAsync(ct);

        var organization = await db.Organizations.FirstOrDefaultAsync(o => o.Id == id && o.IsActive, ct);
        if (organization is null || !scope.CanViewOrganization(id))
        {
            throw new NotFoundException("Organization not found.");
        }

        if (!scope.IsAll)
        {
            throw new ForbiddenException("Only users with access to all organizations can delete an organization.");
        }

        if (await db.Teams.AnyAsync(t => t.OrganizationId == id && t.IsActive, ct))
        {
            throw new ConflictException("This organization still has active teams. Remove or move them first.");
        }

        if (await db.Employees.AnyAsync(e => e.OrganizationId == id && e.IsActive, ct))
        {
            throw new ConflictException("This organization still has active employees. Remove or move them first.");
        }

        organization.IsActive = false;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Team and employee counts are computed through the caller's data scope so they never reveal hidden data.</summary>
    private async Task<IReadOnlyList<OrganizationResponse>> ToResponsesAsync(
        IReadOnlyList<Organization> organizations,
        DataScope scope,
        CancellationToken ct)
    {
        if (organizations.Count == 0)
        {
            return Array.Empty<OrganizationResponse>();
        }

        var ids = organizations.Select(o => o.Id).ToList();

        var teamCounts = await scope
            .Filter(db.Teams.AsNoTracking().Where(t => t.IsActive && ids.Contains(t.OrganizationId)))
            .GroupBy(t => t.OrganizationId)
            .Select(g => new { OrganizationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.OrganizationId, x => x.Count, ct);

        var employeeCounts = await scope
            .Filter(db.Employees.AsNoTracking().Where(e => e.IsActive && ids.Contains(e.OrganizationId)))
            .GroupBy(e => e.OrganizationId)
            .Select(g => new { OrganizationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.OrganizationId, x => x.Count, ct);

        return organizations
            .Select(o => new OrganizationResponse(
                o.Id,
                o.Name,
                o.Description,
                o.IsActive,
                teamCounts.GetValueOrDefault(o.Id),
                employeeCounts.GetValueOrDefault(o.Id),
                o.CreatedAt,
                o.UpdatedAt))
            .ToList();
    }
}
