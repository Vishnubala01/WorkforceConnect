using Microsoft.EntityFrameworkCore;
using WorkforceConnect.Application.Common;
using WorkforceConnect.Application.DTOs;

namespace WorkforceConnect.Tests;

public class EmployeeServiceTests
{
    private static SaveEmployeeRequest NewEmployee(int organizationId, int? teamId, string code = "EMP-2001", string email = "new.hire@example.com") => new()
    {
        EmployeeCode = code,
        FirstName = "New",
        LastName = "Hire",
        Email = email,
        OrganizationId = organizationId,
        TeamId = teamId
    };

    [Fact]
    public async Task Super_admin_lists_everyone()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.Admin);

        var page = await env.Employees.ListAsync(new EmployeeListQuery());

        Assert.Equal(4, page.Total);
    }

    [Fact]
    public async Task Manager_lists_only_employees_of_their_organization()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.Manager);

        var page = await env.Employees.ListAsync(new EmployeeListQuery());

        Assert.Equal(3, page.Total);
        Assert.DoesNotContain(page.Items, e => e.Email == Emails.Admin);
    }

    [Fact]
    public async Task Team_lead_lists_only_their_team_members()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.TeamLead);

        var page = await env.Employees.ListAsync(new EmployeeListQuery());

        Assert.Equal(
            new[] { Emails.Employee, Emails.TeamLead },
            page.Items.Select(e => e.Email).OrderBy(e => e));
    }

    [Fact]
    public async Task Employee_sees_only_their_own_record()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.Employee);

        var page = await env.Employees.ListAsync(new EmployeeListQuery());

        var own = Assert.Single(page.Items);
        Assert.Equal(Emails.Employee, own.Email);

        await Assert.ThrowsAsync<NotFoundException>(() => env.Employees.GetAsync(env.EmployeeId(Emails.Manager)));
        Assert.Equal(own.Id, (await env.Employees.GetAsync(own.Id)).Id);
    }

    [Fact]
    public async Task Asking_for_an_organization_outside_the_scope_returns_nothing_rather_than_its_data()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.Manager);

        var page = await env.Employees.ListAsync(new EmployeeListQuery { OrganizationId = env.OrganizationId("HR & Operations") });

        Assert.Equal(0, page.Total);
    }

    [Fact]
    public async Task List_can_be_narrowed_by_team_and_search_text()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.Admin);

        var byTeam = await env.Employees.ListAsync(new EmployeeListQuery { TeamId = env.TeamId("S1") });
        Assert.Equal(2, byTeam.Total);

        var bySearch = await env.Employees.ListAsync(new EmployeeListQuery { Search = "Maya" });
        Assert.Equal(Emails.Manager, Assert.Single(bySearch.Items).Email);
    }

    [Fact]
    public async Task Paging_limits_the_page_size_and_reports_the_total()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.Admin);

        var page = await env.Employees.ListAsync(new EmployeeListQuery { Page = 2, PageSize = 3 });

        Assert.Equal(4, page.Total);
        Assert.Single(page.Items);
        Assert.Equal(2, page.Page);
    }

    [Fact]
    public async Task Manager_can_create_an_employee_in_their_organization_and_team()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.Manager);

        var created = await env.Employees.CreateAsync(NewEmployee(env.OrganizationId("NNX"), env.TeamId("TME")));

        Assert.Equal("New Hire", created.FullName);
        Assert.Equal("NNX", created.OrganizationName);
        Assert.Equal("TME", created.TeamName);
    }

    [Fact]
    public async Task Manager_cannot_place_an_employee_in_an_organization_they_do_not_manage()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.Manager);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            env.Employees.CreateAsync(NewEmployee(env.OrganizationId("7NX"), null)));
    }

    [Fact]
    public async Task A_team_from_a_different_organization_is_rejected()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.Admin);
        var sevenNx = env.OrganizationId("7NX");

        await Assert.ThrowsAsync<AppValidationException>(() =>
            env.Employees.CreateAsync(NewEmployee(sevenNx, env.TeamId("S1"))));
    }

    [Fact]
    public async Task Duplicate_employee_codes_and_emails_are_rejected()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.Admin);
        var nnx = env.OrganizationId("NNX");

        await Assert.ThrowsAsync<ConflictException>(() =>
            env.Employees.CreateAsync(NewEmployee(nnx, null, code: "EMP-1001")));
        await Assert.ThrowsAsync<ConflictException>(() =>
            env.Employees.CreateAsync(NewEmployee(nnx, null, email: Emails.Manager)));
    }

    [Fact]
    public async Task Moving_an_employee_updates_their_placement()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.Admin);
        var employeeId = env.EmployeeId(Emails.Employee);

        var updated = await env.Employees.UpdateAsync(employeeId, new SaveEmployeeRequest
        {
            EmployeeCode = "EMP-1003",
            FirstName = "Erin",
            LastName = "Employee",
            Email = Emails.Employee,
            OrganizationId = env.OrganizationId("NNX"),
            TeamId = env.TeamId("POD")
        });

        Assert.Equal("POD", updated.TeamName);
    }

    [Fact]
    public async Task Deleting_an_employee_also_disables_their_login()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.Admin);

        await env.Employees.DeleteAsync(env.EmployeeId(Emails.Employee));

        var user = await env.Db.Users.SingleAsync(u => u.Email == Emails.Employee);
        Assert.False(user.IsActive);
        await Assert.ThrowsAsync<NotFoundException>(() => env.Employees.GetAsync(env.EmployeeId(Emails.Employee)));
    }

    [Fact]
    public async Task Nobody_can_delete_their_own_employee_record()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.Admin);

        await Assert.ThrowsAsync<ConflictException>(() => env.Employees.DeleteAsync(env.EmployeeId(Emails.Admin)));
    }

    [Fact]
    public async Task Team_lead_cannot_reach_a_manager_record_even_with_its_id()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.TeamLead);

        await Assert.ThrowsAsync<NotFoundException>(() => env.Employees.GetAsync(env.EmployeeId(Emails.Manager)));
    }
}
