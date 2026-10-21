using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorkforceConnect.Application.Interfaces;
using WorkforceConnect.Domain.Entities;
using WorkforceConnect.Domain.Enums;
using WorkforceConnect.Domain.Security;

namespace WorkforceConnect.Infrastructure.Data;

/// <summary>
/// Creates the reference data the application needs on a fresh database. Safe to run on every startup:
/// every step checks what already exists and never overwrites changes made through the application.
/// </summary>
public sealed class DbSeeder(
    AppDbContext db,
    IPasswordHasher hasher,
    IOptions<SeedOptions> options,
    ILogger<DbSeeder> logger)
{
    private sealed record RoleSeed(
        string Name,
        string Description,
        DataScopeLevel Scope,
        IReadOnlyList<string> Permissions);

    private static readonly RoleSeed[] RoleSeeds =
    [
        new(SystemRoles.SuperAdmin,
            "Full access to every organization, team, employee and role.",
            DataScopeLevel.All,
            Domain.Security.Permissions.All),

        new(SystemRoles.Manager,
            "Full management of teams, employees and roles within their organizations.",
            DataScopeLevel.Organization,
            [
                Domain.Security.Permissions.Organization.View,
                Domain.Security.Permissions.Team.View,
                Domain.Security.Permissions.Team.Create,
                Domain.Security.Permissions.Team.Edit,
                Domain.Security.Permissions.Team.Delete,
                Domain.Security.Permissions.Employee.View,
                Domain.Security.Permissions.Employee.Create,
                Domain.Security.Permissions.Employee.Edit,
                Domain.Security.Permissions.Role.View,
                Domain.Security.Permissions.Role.Create,
                Domain.Security.Permissions.Role.Edit,
                Domain.Security.Permissions.Role.Delete
            ]),

        new(SystemRoles.TeamLead,
            "Read-only access to their own team, members and organization.",
            DataScopeLevel.Team,
            [
                Domain.Security.Permissions.Organization.View,
                Domain.Security.Permissions.Team.View,
                Domain.Security.Permissions.Employee.View
            ]),

        new(SystemRoles.Employee,
            "Access to their own record only.",
            DataScopeLevel.Self,
            [
                Domain.Security.Permissions.Employee.View
            ])
    ];

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var seed = options.Value;

        await SeedPermissionsAsync(ct);
        await SeedRolesAsync(ct);
        await SeedOrganizationsAsync(ct);
        await SeedSuperAdminAsync(seed, ct);

        if (seed.DemoData)
        {
            await SeedDemoUsersAsync(seed, ct);
        }
    }

    private async Task SeedPermissionsAsync(CancellationToken ct)
    {
        var existing = await db.Permissions.Select(p => p.Name).ToListAsync(ct);
        var known = new HashSet<string>(existing, StringComparer.Ordinal);

        foreach (var name in Domain.Security.Permissions.All.Where(n => !known.Contains(n)))
        {
            var separator = name.IndexOf('.');
            db.Permissions.Add(new Permission
            {
                Name = name,
                Module = separator > 0 ? name[..separator] : name,
                Description = $"Allows {name.Replace('.', ' ').ToLowerInvariant()}."
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task SeedRolesAsync(CancellationToken ct)
    {
        var permissionsByName = await db.Permissions.ToDictionaryAsync(p => p.Name, ct);

        foreach (var seed in RoleSeeds)
        {
            var role = await db.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Name == seed.Name, ct);

            if (role is null)
            {
                // New role: grant its default permissions once.
                role = new Role
                {
                    Name = seed.Name,
                    Description = seed.Description,
                    DataScope = seed.Scope,
                    IsSystem = true
                };

                foreach (var permissionName in seed.Permissions)
                {
                    role.RolePermissions.Add(new RolePermission { Permission = permissionsByName[permissionName] });
                }

                db.Roles.Add(role);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task SeedOrganizationsAsync(CancellationToken ct)
    {
        // On a fresh database, create initial organizations
        if (!await db.Organizations.AnyAsync(ct))
        {
            var nnx = new Organization { Name = "NNX", Description = "NNX organization" };
            var sevenNx = new Organization { Name = "7NX", Description = "7NX organization" };
            var hr = new Organization { Name = "HR & Operations", Description = "Human resources and operations" };

            foreach (var teamName in new[] { "S1", "TME", "POD", "Capture" })
            {
                nnx.Teams.Add(new Team { Name = teamName });
            }

            // Added in this order so the ids are 1, 2, 3 as in the product spec.
            db.Organizations.Add(nnx);
            db.Organizations.Add(sevenNx);
            db.Organizations.Add(hr);
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task SeedSuperAdminAsync(SeedOptions seed, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(ct))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(seed.AdminPassword))
        {
            logger.LogWarning(
                "No users exist and Seed:AdminPassword is not set, so no Super Admin was created. " +
                "Set Seed__AdminPassword and restart to create the first administrator.");
            return;
        }

        var role = await db.Roles.FirstAsync(r => r.Name == SystemRoles.SuperAdmin, ct);
        // The seed admin belongs to HR & Operations; if that was renamed, fall back to the first organization.
        var organization = await db.Organizations.FirstOrDefaultAsync(o => o.Name == "HR & Operations", ct)
                           ?? await db.Organizations.OrderBy(o => o.Id).FirstOrDefaultAsync(ct);

        if (organization is null)
        {
            logger.LogWarning("No organization exists, so the Super Admin could not be created.");
            return;
        }

        var email = seed.AdminEmail.Trim();
        var employee = new Employee
        {
            EmployeeCode = "EMP-0001",
            FirstName = seed.AdminFirstName,
            LastName = seed.AdminLastName,
            Email = email,
            OrganizationId = organization.Id
        };

        db.Users.Add(new User
        {
            Employee = employee,
            Email = email,
            PasswordHash = hasher.Hash(seed.AdminPassword),
            RoleId = role.Id,
            IsActive = true
        });

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Created the initial Super Admin account for {Email}.", email);
    }

    private async Task SeedDemoUsersAsync(SeedOptions seed, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(seed.DemoPassword))
        {
            logger.LogWarning("Seed:DemoData is enabled but Seed:DemoPassword is empty; demo users were not created.");
            return;
        }

        const string managerEmail = "manager@workforceconnect.local";
        if (await db.Users.AnyAsync(u => u.Email == managerEmail, ct))
        {
            return;
        }

        var organization = await db.Organizations.FirstOrDefaultAsync(o => o.Name == "NNX", ct);
        var team = organization is null
            ? null
            : await db.Teams.FirstOrDefaultAsync(t => t.OrganizationId == organization.Id && t.Name == "S1", ct);

        if (organization is null || team is null)
        {
            logger.LogWarning("Demo users need the NNX organization and its S1 team; they were not created.");
            return;
        }

        var roles = await db.Roles.ToDictionaryAsync(r => r.Name, ct);
        var password = hasher.Hash(seed.DemoPassword);

        var manager = new User
        {
            Email = managerEmail,
            PasswordHash = password,
            RoleId = roles[SystemRoles.Manager].Id,
            Employee = new Employee
            {
                EmployeeCode = "EMP-1001",
                FirstName = "Maya",
                LastName = "Manager",
                Email = managerEmail,
                OrganizationId = organization.Id
            }
        };
        manager.OrganizationAccess.Add(new UserOrganizationAccess { OrganizationId = organization.Id });

        var teamLead = new User
        {
            Email = "teamlead@workforceconnect.local",
            PasswordHash = password,
            RoleId = roles[SystemRoles.TeamLead].Id,
            Employee = new Employee
            {
                EmployeeCode = "EMP-1002",
                FirstName = "Tom",
                LastName = "Lead",
                Email = "teamlead@workforceconnect.local",
                OrganizationId = organization.Id,
                TeamId = team.Id
            }
        };
        teamLead.TeamAccess.Add(new UserTeamAccess { TeamId = team.Id });

        var employee = new User
        {
            Email = "employee@workforceconnect.local",
            PasswordHash = password,
            RoleId = roles[SystemRoles.Employee].Id,
            Employee = new Employee
            {
                EmployeeCode = "EMP-1003",
                FirstName = "Erin",
                LastName = "Employee",
                Email = "employee@workforceconnect.local",
                OrganizationId = organization.Id,
                TeamId = team.Id
            }
        };

        db.Users.AddRange(manager, teamLead, employee);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Created demo Manager, TeamLead and Employee accounts.");
    }
}
