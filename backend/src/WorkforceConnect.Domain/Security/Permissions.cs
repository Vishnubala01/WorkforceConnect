namespace WorkforceConnect.Domain.Security;

/// <summary>
/// Names of every permission the application knows about. Endpoints are protected with these
/// names; which role holds which permission is data (RolePermissions), not code.
/// Future modules (DailyStatus.*, Performance.*, Reports.*) are added here and in the database.
/// </summary>
public static class Permissions
{
    public static class Organization
    {
        public const string View = "Organization.View";
        public const string Create = "Organization.Create";
        public const string Edit = "Organization.Edit";
        public const string Delete = "Organization.Delete";
    }

    public static class Team
    {
        public const string View = "Team.View";
        public const string Create = "Team.Create";
        public const string Edit = "Team.Edit";
        public const string Delete = "Team.Delete";
    }

    public static class Employee
    {
        public const string View = "Employee.View";
        public const string Create = "Employee.Create";
        public const string Edit = "Employee.Edit";
        public const string Delete = "Employee.Delete";
    }

    public static class Role
    {
        public const string View = "Role.View";
        public const string Create = "Role.Create";
        public const string Edit = "Role.Edit";
        public const string Delete = "Role.Delete";
    }

    public static IReadOnlyList<string> All { get; } = new[]
    {
        Organization.View, Organization.Create, Organization.Edit, Organization.Delete,
        Team.View, Team.Create, Team.Edit, Team.Delete,
        Employee.View, Employee.Create, Employee.Edit, Employee.Delete,
        Role.View, Role.Create, Role.Edit, Role.Delete
    };
}
