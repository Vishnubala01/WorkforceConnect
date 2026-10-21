
using Microsoft.EntityFrameworkCore;
using WorkforceConnect.Application.Common;
using WorkforceConnect.Application.DTOs;
using WorkforceConnect.Application.Interfaces;
using WorkforceConnect.Application.Security;
using WorkforceConnect.Domain.Entities;

namespace WorkforceConnect.Application.Services;

public sealed class EmployeeService(IApplicationDbContext db, IDataScopeService scopes, IPasswordHasher hasher) : IEmployeeService
{
    private static EmployeeResponse MapToResponse(Employee e, string? roleName, string? projectLeadName) => new EmployeeResponse(
        e.Id,
        e.EmployeeCode,
        e.FirstName,
        e.LastName,
        e.FirstName + " " + e.LastName,
        e.Email,
        e.JobTitle,
        e.OrganizationId,
        e.Organization.Name,
        e.TeamId,
        e.Team == null ? null : e.Team.Name,
        roleName,
        projectLeadName,
        e.IsActive,
        e.CreatedAt,
        e.UpdatedAt,
        e.SecondaryOrganizations?.Select(o => o.Id).ToList() ?? new List<int>(),
        e.SecondaryTeams?.Select(t => t.Id).ToList() ?? new List<int>());

    public async Task<PagedResponse<EmployeeResponse>> ListAsync(EmployeeListQuery query, CancellationToken ct = default)
    {
        var scope = await scopes.GetAsync(ct);

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        // The data scope is applied first. Client-supplied organizationId/teamId only narrow it further,
        // so asking for an organization outside the caller's scope simply returns nothing.
        var employees = scope.Filter(db.Employees.AsNoTracking().Where(e => e.IsActive));

        if (query.OrganizationId is int organizationId)
        {
            employees = employees.Where(e => e.OrganizationId == organizationId || e.SecondaryOrganizations.Any(o => o.Id == organizationId));
        }

        if (query.TeamId is int teamId)
        {
            employees = employees.Where(e => e.TeamId == teamId || e.SecondaryTeams.Any(t => t.Id == teamId));
        }

        var search = query.Search.NullIfBlank();
        if (search is not null)
        {
            employees = employees.Where(e =>
                e.FirstName.Contains(search)
                || e.LastName.Contains(search)
                || e.EmployeeCode.Contains(search)
                || e.Email.Contains(search)
                || (e.FirstName + " " + e.LastName).Contains(search));
        }

        var total = await employees.CountAsync(ct);

        var raw = await employees
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .ThenBy(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(e => e.Organization)
            .Include(e => e.Team)
            .Include(e => e.SecondaryOrganizations)
            .Include(e => e.SecondaryTeams)
            .ToListAsync(ct);

        var employeeIds = raw.Select(e => e.Id).ToList();
        var teamIds = raw.Where(e => e.TeamId.HasValue).Select(e => e.TeamId!.Value).Distinct().ToList();

        var rolesRaw = await db.Users
            .AsNoTracking()
            .Where(u => u.EmployeeId.HasValue && employeeIds.Contains(u.EmployeeId.Value))
            .Select(u => new { u.EmployeeId, RoleName = u.Role.Name })
            .ToListAsync(ct);

        var rolesByEmployeeId = rolesRaw
            .GroupBy(x => x.EmployeeId!.Value)
            .ToDictionary(g => g.Key, g => g.First().RoleName);

        var projectLeadsRaw = await db.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.EmployeeId.HasValue && u.Employee!.TeamId.HasValue &&
                        teamIds.Contains(u.Employee.TeamId.Value) &&
                        u.Role.Name == Domain.Security.SystemRoles.TeamLead)
            .Select(u => new { TeamId = u.Employee!.TeamId!.Value, FullName = u.Employee.FirstName + " " + u.Employee.LastName })
            .ToListAsync(ct);

        var projectLeadsByTeam = projectLeadsRaw
            .GroupBy(x => x.TeamId)
            .ToDictionary(g => g.Key, g => g.First().FullName);

        var items = raw
            .Select(e => MapToResponse(e, rolesByEmployeeId.GetValueOrDefault(e.Id), e.TeamId.HasValue ? projectLeadsByTeam.GetValueOrDefault(e.TeamId.Value) : null))
            .ToList();

        return new PagedResponse<EmployeeResponse>(items, total, page, pageSize);
    }

    public async Task<EmployeeResponse> GetAsync(int id, CancellationToken ct = default)
    {
        var scope = await scopes.GetAsync(ct);

        var employee = await scope
            .Filter(db.Employees.AsNoTracking()
                .Include(e => e.Organization)
                .Include(e => e.Team)
                .Include(e => e.SecondaryOrganizations)
                .Include(e => e.SecondaryTeams)
                .Where(e => e.Id == id && e.IsActive))
            .FirstOrDefaultAsync(ct);

        if (employee is null) throw new NotFoundException("Employee not found.");

        var roleName = await db.Users
            .AsNoTracking()
            .Where(u => u.EmployeeId == id)
            .Select(u => u.Role.Name)
            .FirstOrDefaultAsync(ct);

        string? projectLeadName = null;
        if (employee.TeamId.HasValue)
        {
            projectLeadName = await db.Users
                .AsNoTracking()
                .Where(u => u.IsActive && u.EmployeeId.HasValue && u.Employee!.TeamId == employee.TeamId.Value &&
                            u.Role.Name == Domain.Security.SystemRoles.TeamLead)
                .Select(u => u.Employee!.FirstName + " " + u.Employee.LastName)
                .FirstOrDefaultAsync(ct);
        }

        return MapToResponse(employee, roleName, projectLeadName);
    }

    public async Task<EmployeeResponse> CreateAsync(SaveEmployeeRequest request, CancellationToken ct = default)
    {
        var scope = await scopes.GetAsync(ct);

        await ValidatePlacementAsync(request.OrganizationId, request.TeamId, scope, existingEmployeeId: null, ct);

        var code = request.EmployeeCode.Trim();
        var email = request.Email.Trim();
        await EnsureUniqueAsync(code, email, excludeId: null, ct);

        var employee = new Employee
        {
            EmployeeCode = code,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            JobTitle = request.JobTitle?.Trim(),
            OrganizationId = request.OrganizationId,
            TeamId = request.TeamId,
            IsActive = true
        };

        if (request.SecondaryOrganizationIds?.Any() == true)
        {
            var orgs = await db.Organizations.Where(o => request.SecondaryOrganizationIds.Contains(o.Id)).ToListAsync(ct);
            foreach (var org in orgs) employee.SecondaryOrganizations.Add(org);
        }

        if (request.SecondaryTeamIds?.Any() == true)
        {
            var teams = await db.Teams.Where(t => request.SecondaryTeamIds.Contains(t.Id)).ToListAsync(ct);
            foreach (var t in teams) employee.SecondaryTeams.Add(t);
        }

        db.Employees.Add(employee);

        if (request.RoleId is int roleId)
        {
            var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == roleId, ct)
                ?? throw new AppValidationException("Invalid role selected.");

            var user = new User
            {
                Email = email,
                PasswordHash = hasher.Hash("Welcome!123"), // Default password
                RoleId = role.Id,
                Employee = employee,
                IsActive = true
            };

            if (role.Name == Domain.Security.SystemRoles.Manager)
            {
                user.OrganizationAccess.Add(new UserOrganizationAccess { OrganizationId = employee.OrganizationId });
            }
            else if (role.Name == Domain.Security.SystemRoles.TeamLead && employee.TeamId.HasValue)
            {
                user.TeamAccess.Add(new UserTeamAccess { TeamId = employee.TeamId.Value });
            }

            db.Users.Add(user);
        }

        await db.SaveChangesAsync(ct);

        return await GetAsync(employee.Id, ct);
    }

    public async Task<EmployeeResponse> UpdateAsync(int id, SaveEmployeeRequest request, CancellationToken ct = default)
    {
        var scope = await scopes.GetAsync(ct);

        var employee = await scope
            .Filter(db.Employees
                .Include(e => e.SecondaryOrganizations)
                .Include(e => e.SecondaryTeams)
                .Where(e => e.Id == id && e.IsActive))
            .FirstOrDefaultAsync(ct);

        if (employee is null) throw new NotFoundException("Employee not found.");

        await ValidatePlacementAsync(request.OrganizationId, request.TeamId, scope, existingEmployeeId: id, ct);

        var code = request.EmployeeCode.Trim();
        var email = request.Email.Trim();
        await EnsureUniqueAsync(code, email, excludeId: id, ct);

        employee.EmployeeCode = code;
        employee.FirstName = request.FirstName.Trim();
        employee.LastName = request.LastName.Trim();
        employee.Email = email;
        employee.JobTitle = request.JobTitle?.Trim();
        employee.OrganizationId = request.OrganizationId;
        employee.TeamId = request.TeamId;

        employee.SecondaryOrganizations.Clear();
        if (request.SecondaryOrganizationIds?.Any() == true)
        {
            var orgs = await db.Organizations.Where(o => request.SecondaryOrganizationIds.Contains(o.Id)).ToListAsync(ct);
            foreach (var org in orgs) employee.SecondaryOrganizations.Add(org);
        }

        employee.SecondaryTeams.Clear();
        if (request.SecondaryTeamIds?.Any() == true)
        {
            var teams = await db.Teams.Where(t => request.SecondaryTeamIds.Contains(t.Id)).ToListAsync(ct);
            foreach (var t in teams) employee.SecondaryTeams.Add(t);
        }

        await db.SaveChangesAsync(ct);

        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var scope = await scopes.GetAsync(ct);

        var employee = await FindAccessibleAsync(id, scope, ct);

        if (scope.EmployeeId == employee.Id)
        {
            throw new ConflictException("You cannot delete your own employee record.");
        }

        employee.IsActive = false;

        // Deactivating an employee also disables their login.
        var user = await db.Users.FirstOrDefaultAsync(u => u.EmployeeId == id, ct);
        if (user is not null)
        {
            user.IsActive = false;
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task<Employee> FindAccessibleAsync(int id, DataScope scope, CancellationToken ct)
    {
        var employee = await scope
            .Filter(db.Employees.Where(e => e.Id == id && e.IsActive))
            .FirstOrDefaultAsync(ct);

        return employee ?? throw new NotFoundException("Employee not found.");
    }

    /// <summary>
    /// Never trusts the organizationId/teamId sent by the client: the caller must be allowed to place
    /// an employee there, both must exist, and the team must belong to the organization.
    /// </summary>
    private async Task ValidatePlacementAsync(
        int organizationId,
        int? teamId,
        DataScope scope,
        int? existingEmployeeId,
        CancellationToken ct)
    {
        if (!scope.CanAccessEmployee(organizationId, teamId, existingEmployeeId))
        {
            throw new NotFoundException("Organization or team not found.");
        }

        var organizationExists = await db.Organizations.AnyAsync(o => o.Id == organizationId && o.IsActive, ct);
        if (!organizationExists)
        {
            throw new NotFoundException("Organization not found.");
        }

        if (teamId is int requestedTeamId)
        {
            var teamMatches = await db.Teams.AnyAsync(
                t => t.Id == requestedTeamId && t.OrganizationId == organizationId && t.IsActive, ct);

            if (!teamMatches)
            {
                throw new AppValidationException("The selected team does not belong to the selected organization.");
            }
        }
    }

    private async Task EnsureUniqueAsync(string code, string email, int? excludeId, CancellationToken ct)
    {
        var id = excludeId ?? 0;

        if (await db.Employees.AnyAsync(e => e.Id != id && e.EmployeeCode == code, ct))
        {
            throw new ConflictException($"Employee code '{code}' is already in use.");
        }

        if (await db.Employees.AnyAsync(e => e.Id != id && e.Email == email, ct))
        {
            throw new ConflictException($"An employee with email '{email}' already exists.");
        }
    }
}
