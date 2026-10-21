using WorkforceConnect.Application.Common;
using WorkforceConnect.Application.DTOs;

namespace WorkforceConnect.Tests;

public class OrganizationServiceTests
{
    [Fact]
    public async Task Super_admin_sees_every_organization_with_counts()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.Admin);

        var list = await env.Organizations.ListAsync();

        Assert.Equal(new[] { "NNX", "7NX", "HR & Operations" }, list.Select(o => o.Name));
        Assert.Equal(4, list.Single(o => o.Name == "NNX").TeamCount);
        Assert.Equal(3, list.Single(o => o.Name == "NNX").EmployeeCount);
    }

    [Fact]
    public async Task A_new_organization_appears_in_the_list_without_any_other_change()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.Admin);

        await env.Organizations.CreateAsync(new SaveOrganizationRequest { Name = "ABC" });

        var names = (await env.Organizations.ListAsync()).Select(o => o.Name);
        Assert.Equal(new[] { "NNX", "7NX", "HR & Operations", "ABC" }, names);
    }

    [Fact]
    public async Task Manager_only_sees_the_organization_they_are_assigned_to()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.Manager);

        var list = await env.Organizations.ListAsync();

        Assert.Equal(new[] { "NNX" }, list.Select(o => o.Name));
    }

    [Fact]
    public async Task Requesting_an_organization_outside_the_scope_looks_like_it_does_not_exist()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.Manager);

        await Assert.ThrowsAsync<NotFoundException>(() => env.Organizations.GetAsync(env.OrganizationId("7NX")));
    }

    [Fact]
    public async Task Manager_cannot_create_an_organization_even_if_the_request_reaches_the_service()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.Manager);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            env.Organizations.CreateAsync(new SaveOrganizationRequest { Name = "Sneaky" }));
    }

    [Fact]
    public async Task Manager_cannot_edit_or_delete_an_organization_outside_the_scope()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.Manager);
        var other = env.OrganizationId("7NX");

        await Assert.ThrowsAsync<NotFoundException>(() =>
            env.Organizations.UpdateAsync(other, new SaveOrganizationRequest { Name = "Renamed" }));
        await Assert.ThrowsAsync<NotFoundException>(() => env.Organizations.DeleteAsync(other));
    }

    [Fact]
    public async Task Team_lead_sees_the_parent_organization_but_only_their_own_team_in_the_counts()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.TeamLead);

        var list = await env.Organizations.ListAsync();

        var nnx = Assert.Single(list);
        Assert.Equal("NNX", nnx.Name);
        Assert.Equal(1, nnx.TeamCount);
        Assert.Equal(2, nnx.EmployeeCount);
    }

    [Fact]
    public async Task Team_lead_cannot_edit_the_organization_they_can_view()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.TeamLead);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            env.Organizations.UpdateAsync(env.OrganizationId("NNX"), new SaveOrganizationRequest { Name = "Renamed" }));
    }

    [Fact]
    public async Task Duplicate_names_are_rejected()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.Admin);

        await Assert.ThrowsAsync<ConflictException>(() =>
            env.Organizations.CreateAsync(new SaveOrganizationRequest { Name = "NNX" }));
    }

    [Fact]
    public async Task An_organization_with_active_teams_cannot_be_deleted()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.Admin);

        await Assert.ThrowsAsync<ConflictException>(() => env.Organizations.DeleteAsync(env.OrganizationId("NNX")));
    }

    [Fact]
    public async Task A_deleted_organization_disappears_and_creating_the_same_name_restores_it()
    {
        var env = await TestEnv.CreateAsync();
        await env.SignInAsync(Emails.Admin);

        var created = await env.Organizations.CreateAsync(new SaveOrganizationRequest { Name = "ABC" });
        await env.Organizations.DeleteAsync(created.Id);

        Assert.DoesNotContain(await env.Organizations.ListAsync(), o => o.Name == "ABC");
        await Assert.ThrowsAsync<NotFoundException>(() => env.Organizations.GetAsync(created.Id));

        var restored = await env.Organizations.CreateAsync(new SaveOrganizationRequest { Name = "ABC", Description = "Back again" });
        Assert.Equal(created.Id, restored.Id);
        Assert.Equal("Back again", restored.Description);
    }
}
