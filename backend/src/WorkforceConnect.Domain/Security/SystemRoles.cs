namespace WorkforceConnect.Domain.Security;

/// <summary>
/// Names of the roles created by the seeder. Used ONLY for bootstrapping the database.
/// Authorization never branches on a role name or a user id; it uses permissions and data scope.
/// </summary>
public static class SystemRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Manager = "Manager";
    public const string TeamLead = "TeamLead";
    public const string Employee = "Employee";
}
