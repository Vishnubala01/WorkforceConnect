using WorkforceConnect.Domain.Entities;
using WorkforceConnect.Domain.Enums;

namespace WorkforceConnect.Application.Security;

/// <summary>
/// "What data can this user access?" Computed from the user's role scope level and assignments.
/// Kept separate from permissions ("what can this user do?"): every service checks both.
/// </summary>
public sealed class DataScope
{
    public DataScope(
        DataScopeLevel level,
        int? employeeId,
        IEnumerable<int> organizationIds,
        IEnumerable<int> teamIds,
        IEnumerable<int> organizationIdsOfAssignedTeams)
    {
        Level = level;
        EmployeeId = employeeId;
        OrganizationIds = new HashSet<int>(organizationIds);
        TeamIds = new HashSet<int>(teamIds);

        var viewable = new HashSet<int>(OrganizationIds);
        viewable.UnionWith(organizationIdsOfAssignedTeams);
        ViewableOrganizationIds = viewable;
    }

    public DataScopeLevel Level { get; }

    /// <summary>The caller's own employee record, if the account is linked to one.</summary>
    public int? EmployeeId { get; }

    /// <summary>Organizations the caller covers entirely (all their teams and employees).</summary>
    public IReadOnlySet<int> OrganizationIds { get; }

    /// <summary>Individual teams the caller covers.</summary>
    public IReadOnlySet<int> TeamIds { get; }

    /// <summary>Organizations the caller can see: covered ones plus the parents of covered teams.</summary>
    public IReadOnlySet<int> ViewableOrganizationIds { get; }

    public bool IsAll => Level == DataScopeLevel.All;

    // ----- Point checks (used for a single resource, e.g. GET /api/organizations/5) -----

    public bool CanViewOrganization(int organizationId) =>
        IsAll || ViewableOrganizationIds.Contains(organizationId);

    /// <summary>Whether the caller may add/change things inside the organization (for example create a team in it).</summary>
    public bool CanAdministerOrganization(int organizationId) =>
        IsAll || OrganizationIds.Contains(organizationId);

    public bool CanAccessTeam(int teamId, int organizationId) =>
        IsAll || OrganizationIds.Contains(organizationId) || TeamIds.Contains(teamId);

    /// <summary>
    /// Whether the caller may access an employee placed in the given organization/team.
    /// Pass <paramref name="employeeId"/> for an existing employee so the caller's own record is always reachable.
    /// </summary>
    public bool CanAccessEmployee(int organizationId, int? teamId, int? employeeId = null) =>
        IsAll
        || OrganizationIds.Contains(organizationId)
        || (teamId.HasValue && TeamIds.Contains(teamId.Value))
        || (employeeId.HasValue && EmployeeId == employeeId);

    // ----- Query filters (used for lists; translated to SQL) -----

    public IQueryable<Organization> Filter(IQueryable<Organization> query)
    {
        if (IsAll)
        {
            return query;
        }

        var ids = ViewableOrganizationIds.ToList();
        return query.Where(o => ids.Contains(o.Id));
    }

    public IQueryable<Team> Filter(IQueryable<Team> query)
    {
        if (IsAll)
        {
            return query;
        }

        var organizationIds = OrganizationIds.ToList();
        var teamIds = TeamIds.ToList();
        return query.Where(t => organizationIds.Contains(t.OrganizationId) || teamIds.Contains(t.Id));
    }

    public IQueryable<Employee> Filter(IQueryable<Employee> query)
    {
        if (IsAll)
        {
            return query;
        }

        var organizationIds = OrganizationIds.ToList();
        var teamIds = TeamIds.ToList();
        var self = EmployeeId ?? -1;

        return query.Where(e =>
            organizationIds.Contains(e.OrganizationId)
            || (e.TeamId.HasValue && teamIds.Contains(e.TeamId.Value))
            || e.Id == self);
    }
}
