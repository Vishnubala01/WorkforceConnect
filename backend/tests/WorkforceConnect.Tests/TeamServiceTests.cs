using WorkforceConnect.Application.Common;
using WorkforceConnect.Application.DTOs;

namespace WorkforceConnect.Tests;

public class TeamServiceTests
{
    [Fact]
    public async Task Adding_a_team_to_an_organization_makes_it_show_up_in_the_list()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.Admin);
        var nnx = env.OrganizationId("NNX");

        await env.Teams.CreateAsync(new CreateTeamRequest { OrganizationId = nnx, Name = "QA" });

        var names = (await env.Teams.ListByOrganizationAsync(nnx)).Select(t => t.Name);
        Assert.Equal(new[] { "S1", "TME", "POD", "Capture", "QA" }, names);
    }

    [Fact]
    public async Task Manager_can_add_a_team_inside_their_organization()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.Manager);

        var created = await env.Teams.CreateAsync(new CreateTeamRequest { OrganizationId = env.OrganizationId("NNX"), Name = "QA" });

        Assert.Equal("QA", created.Name);
        Assert.Equal("NNX", created.OrganizationName);
    }

    [Fact]
    public async Task Manager_cannot_add_a_team_to_another_organization_by_sending_its_id()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.Manager);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            env.Teams.CreateAsync(new CreateTeamRequest { OrganizationId = env.OrganizationId("7NX"), Name = "Intruder" }));
    }

    [Fact]
    public async Task Team_lead_sees_only_their_own_team_in_the_organization()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.TeamLead);

        var teams = await env.Teams.ListByOrganizationAsync(env.OrganizationId("NNX"));

        Assert.Equal(new[] { "S1" }, teams.Select(t => t.Name));
    }

    [Fact]
    public async Task Team_lead_cannot_open_a_sibling_team_by_id()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.TeamLead);

        await Assert.ThrowsAsync<NotFoundException>(() => env.Teams.GetAsync(env.TeamId("TME")));
        await Assert.ThrowsAsync<NotFoundException>(() => env.Teams.GetAsync(env.TeamId("POD")));
        Assert.Equal("S1", (await env.Teams.GetAsync(env.TeamId("S1"))).Name);
    }

    [Fact]
    public async Task Listing_teams_of_an_organization_outside_the_scope_is_not_found()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.Manager);

        await Assert.ThrowsAsync<NotFoundException>(() => env.Teams.ListByOrganizationAsync(env.OrganizationId("7NX")));
    }

    [Fact]
    public async Task Team_names_are_unique_within_an_organization_but_not_across_organizations()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.Admin);

        await Assert.ThrowsAsync<ConflictException>(() =>
            env.Teams.CreateAsync(new CreateTeamRequest { OrganizationId = env.OrganizationId("NNX"), Name = "S1" }));

        var other = await env.Teams.CreateAsync(new CreateTeamRequest { OrganizationId = env.OrganizationId("7NX"), Name = "S1" });
        Assert.Equal("7NX", other.OrganizationName);
    }

    [Fact]
    public async Task A_team_with_active_employees_cannot_be_deleted_until_it_is_empty()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.Admin);

        await Assert.ThrowsAsync<ConflictException>(() => env.Teams.DeleteAsync(env.TeamId("S1")));

        var emptyTeam = env.TeamId("Capture");
        await env.Teams.DeleteAsync(emptyTeam);
        await Assert.ThrowsAsync<NotFoundException>(() => env.Teams.GetAsync(emptyTeam));
    }

    [Fact]
    public async Task Team_response_counts_the_active_members()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.Admin);

        var s1 = await env.Teams.GetAsync(env.TeamId("S1"));

        Assert.Equal(2, s1.EmployeeCount);
    }
}
