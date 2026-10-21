using Microsoft.EntityFrameworkCore;
using WorkforceConnect.Application.Common;
using WorkforceConnect.Application.DTOs;
using WorkforceConnect.Domain.Enums;
using WorkforceConnect.Domain.Security;

namespace WorkforceConnect.Tests;

public class SeederAndPermissionTests
{
    [Fact]
    public async Task Seeds_every_permission_and_the_four_roles()
    {
        var env = await TestEnv.CreateAsync();

        Assert.Equal(Permissions.All.Count, await env.Db.Permissions.CountAsync());
        Assert.Equal(
            new[] { "Employee", "Manager", "SuperAdmin", "TeamLead" },
            await env.Db.Roles.OrderBy(r => r.Name).Select(r => r.Name).ToListAsync());
    }

    [Fact]
    public async Task Seeds_the_initial_organizations_and_NNX_teams_as_data()
    {
        var env = await TestEnv.CreateAsync();

        var organizations = await env.Db.Organizations.OrderBy(o => o.Id).Select(o => o.Name).ToListAsync();
        Assert.Equal(new[] { "NNX", "7NX", "HR & Operations" }, organizations);

        var nnxTeams = await env.Db.Teams
            .Where(t => t.OrganizationId == env.OrganizationId("NNX"))
            .OrderBy(t => t.Id)
            .Select(t => t.Name)
            .ToListAsync();
        Assert.Equal(new[] { "S1", "TME", "POD", "Capture" }, nnxTeams);
    }

    [Fact]
    public async Task Seeding_twice_creates_nothing_new()
    {
        var env = await TestEnv.CreateAsync();
        var before = await Snapshot(env);

        await env.NewSeeder().SeedAsync();

        Assert.Equal(before, await Snapshot(env));
    }

    [Fact]
    public async Task Role_scopes_and_permission_grants_match_the_design()
    {
        var env = await TestEnv.CreateAsync();

        async Task<(DataScopeLevel Scope, List<string> Grants)> Describe(string role)
        {
            var entity = await env.Db.Roles
                .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
                .SingleAsync(r => r.Name == role);
            return (entity.DataScope, entity.RolePermissions.Select(rp => rp.Permission.Name).OrderBy(n => n).ToList());
        }

        var superAdmin = await Describe(SystemRoles.SuperAdmin);
        Assert.Equal(DataScopeLevel.All, superAdmin.Scope);
        Assert.Equal(Permissions.All.OrderBy(n => n).ToList(), superAdmin.Grants);

        var manager = await Describe(SystemRoles.Manager);
        Assert.Equal(DataScopeLevel.Organization, manager.Scope);
        Assert.Contains(Permissions.Team.Create, manager.Grants);
        Assert.DoesNotContain(Permissions.Organization.Create, manager.Grants);
        Assert.DoesNotContain(Permissions.Employee.Delete, manager.Grants);

        var teamLead = await Describe(SystemRoles.TeamLead);
        Assert.Equal(DataScopeLevel.Team, teamLead.Scope);
        Assert.Equal(
            new[] { Permissions.Employee.View, Permissions.Organization.View, Permissions.Team.View },
            teamLead.Grants);

        var employee = await Describe(SystemRoles.Employee);
        Assert.Equal(DataScopeLevel.Self, employee.Scope);
        Assert.Equal(new[] { Permissions.Employee.View }, employee.Grants);
    }

    [Fact]
    public async Task No_admin_is_created_when_no_password_is_configured()
    {
        var env = await TestEnv.CreateAsync(seedAdmin: false, demoData: false);

        Assert.Equal(0, await env.Db.Users.CountAsync());
    }

    [Fact]
    public async Task A_permission_removed_from_a_role_in_the_database_is_not_restored_by_the_seeder()
    {
        var env = await TestEnv.CreateAsync();

        var managerRole = await env.Db.Roles.Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .SingleAsync(r => r.Name == SystemRoles.Manager);
        var grant = managerRole.RolePermissions.Single(rp => rp.Permission.Name == Permissions.Team.Edit);
        env.Db.RolePermissions.Remove(grant);
        await env.Db.SaveChangesAsync();

        await env.NewSeeder().SeedAsync();
        await env.SignInAsync(Emails.Manager);

        Assert.False(await env.Permissions.HasPermissionAsync(env.Current.UserId, Permissions.Team.Edit));
    }

    [Fact]
    public async Task Permissions_come_from_the_users_role_in_the_database()
    {
        var env = await TestEnv.CreateAsync();

        Assert.True(await env.Permissions.HasPermissionAsync(env.UserId(Emails.Admin), Permissions.Organization.Create));
        Assert.True(await env.Permissions.HasPermissionAsync(env.UserId(Emails.Manager), Permissions.Team.Create));
        Assert.False(await env.Permissions.HasPermissionAsync(env.UserId(Emails.Manager), Permissions.Organization.Create));
        Assert.False(await env.Permissions.HasPermissionAsync(env.UserId(Emails.TeamLead), Permissions.Team.Create));
        Assert.False(await env.Permissions.HasPermissionAsync(env.UserId(Emails.Employee), Permissions.Role.View));
    }

    [Fact]
    public async Task A_deactivated_user_has_no_permissions()
    {
        var env = await TestEnv.CreateAsync();
        var admin = await env.Db.Users.SingleAsync(u => u.Email == Emails.Admin);
        admin.IsActive = false;
        await env.Db.SaveChangesAsync();

        Assert.False(await env.Permissions.HasPermissionAsync(admin.Id, Permissions.Organization.View));
    }

    [Fact]
    public async Task Login_returns_the_user_with_role_permissions_and_scope()
    {
        var env = await TestEnv.CreateAsync();

        var response = await env.Auth.LoginAsync(new LoginRequest { Email = Emails.Admin, Password = TestEnv.AdminPassword });

        Assert.Equal("Bearer", response.TokenType);
        Assert.False(string.IsNullOrEmpty(response.AccessToken));
        Assert.Equal("SuperAdmin", response.User.Role);
        Assert.Equal("Admin User", response.User.Name);
        Assert.Equal("All", response.User.DataScope.Level);
        Assert.Equal(Permissions.All.OrderBy(n => n, StringComparer.Ordinal).ToList(), response.User.Permissions);
        Assert.Equal("HR & Operations", response.User.Organization?.Name);
    }

    [Fact]
    public async Task Login_for_a_team_lead_reports_their_team_and_assigned_scope()
    {
        var env = await TestEnv.CreateAsync();

        var response = await env.Auth.LoginAsync(new LoginRequest { Email = Emails.TeamLead, Password = TestEnv.DemoPassword });

        Assert.Equal("TeamLead", response.User.Role);
        Assert.Equal("Team", response.User.DataScope.Level);
        Assert.Equal(new[] { env.TeamId("S1") }, response.User.DataScope.TeamIds);
        Assert.Equal("S1", response.User.Team?.Name);
        Assert.Equal("NNX", response.User.Organization?.Name);
        Assert.DoesNotContain(Permissions.Team.Create, response.User.Permissions);
    }

    [Fact]
    public async Task Login_fails_the_same_way_for_a_wrong_password_and_an_unknown_email()
    {
        var env = await TestEnv.CreateAsync();

        var wrongPassword = await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            env.Auth.LoginAsync(new LoginRequest { Email = Emails.Admin, Password = "nope" }));
        var unknownEmail = await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            env.Auth.LoginAsync(new LoginRequest { Email = "nobody@example.com", Password = "nope" }));

        Assert.Equal(wrongPassword.Message, unknownEmail.Message);
    }

    [Fact]
    public async Task An_inactive_account_cannot_log_in_even_with_the_right_password()
    {
        var env = await TestEnv.CreateAsync();
        var user = await env.Db.Users.SingleAsync(u => u.Email == Emails.Manager);
        user.IsActive = false;
        await env.Db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            env.Auth.LoginAsync(new LoginRequest { Email = Emails.Manager, Password = TestEnv.DemoPassword }));
    }

    private static async Task<(int Permissions, int Roles, int RolePermissions, int Organizations, int Teams, int Users, int Employees)> Snapshot(TestEnv env) => (
        await env.Db.Permissions.CountAsync(),
        await env.Db.Roles.CountAsync(),
        await env.Db.RolePermissions.CountAsync(),
        await env.Db.Organizations.CountAsync(),
        await env.Db.Teams.CountAsync(),
        await env.Db.Users.CountAsync(),
        await env.Db.Employees.CountAsync());
}
