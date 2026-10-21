using WorkforceConnect.Application.Security;
using WorkforceConnect.Domain.Enums;

namespace WorkforceConnect.Tests;

/// <summary>Pure tests of the "what data can I access?" rules, independent of any database.</summary>
public class DataScopeTests
{
    [Fact]
    public void All_scope_reaches_everything()
    {
        var scope = new DataScope(DataScopeLevel.All, null, [], [], []);

        Assert.True(scope.IsAll);
        Assert.True(scope.CanViewOrganization(99));
        Assert.True(scope.CanAdministerOrganization(99));
        Assert.True(scope.CanAccessTeam(5, 99));
        Assert.True(scope.CanAccessEmployee(99, null));
    }

    [Fact]
    public void Organization_scope_covers_the_assigned_organization_only()
    {
        var scope = new DataScope(DataScopeLevel.Organization, 7, organizationIds: [1], teamIds: [], organizationIdsOfAssignedTeams: []);

        Assert.True(scope.CanViewOrganization(1));
        Assert.True(scope.CanAdministerOrganization(1));
        Assert.True(scope.CanAccessTeam(10, 1));
        Assert.True(scope.CanAccessEmployee(1, null));
        Assert.True(scope.CanAccessEmployee(1, 10));

        Assert.False(scope.CanViewOrganization(2));
        Assert.False(scope.CanAdministerOrganization(2));
        Assert.False(scope.CanAccessTeam(20, 2));
        Assert.False(scope.CanAccessEmployee(2, 20));
    }

    [Fact]
    public void Team_scope_can_see_the_parent_organization_but_not_administer_it()
    {
        var scope = new DataScope(DataScopeLevel.Team, 7, organizationIds: [], teamIds: [10], organizationIdsOfAssignedTeams: [1]);

        Assert.True(scope.CanViewOrganization(1));
        Assert.False(scope.CanAdministerOrganization(1));

        Assert.True(scope.CanAccessTeam(10, 1));
        Assert.False(scope.CanAccessTeam(11, 1));

        Assert.True(scope.CanAccessEmployee(1, 10));
        Assert.False(scope.CanAccessEmployee(1, 11));
        Assert.False(scope.CanAccessEmployee(1, null));
    }

    [Fact]
    public void Self_scope_reaches_only_the_callers_own_employee_record()
    {
        var scope = new DataScope(DataScopeLevel.Self, 5, [], [], []);

        Assert.True(scope.CanAccessEmployee(1, 10, employeeId: 5));
        Assert.False(scope.CanAccessEmployee(1, 10, employeeId: 6));
        Assert.False(scope.CanAccessEmployee(1, 10));
        Assert.False(scope.CanViewOrganization(1));
        Assert.False(scope.CanAccessTeam(10, 1));
    }

    [Fact]
    public void A_nonexistent_own_employee_never_matches_by_accident()
    {
        var scope = new DataScope(DataScopeLevel.Self, null, [], [], []);

        Assert.False(scope.CanAccessEmployee(1, null, employeeId: 0));
        Assert.False(scope.CanAccessEmployee(1, null, employeeId: null));
    }
}
