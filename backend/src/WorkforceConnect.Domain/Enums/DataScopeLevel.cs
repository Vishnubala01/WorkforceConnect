namespace WorkforceConnect.Domain.Enums;

/// <summary>
/// How much data a role may reach. This is separate from permissions:
/// permissions answer "what can I do?", data scope answers "what can I access?".
/// </summary>
public enum DataScopeLevel
{
    /// <summary>Every organization, team and employee.</summary>
    All = 0,

    /// <summary>Only the organizations assigned to the user (plus the user's own organization).</summary>
    Organization = 1,

    /// <summary>Only the teams assigned to the user (plus the user's own team).</summary>
    Team = 2,

    /// <summary>Only the user's own employee record.</summary>
    Self = 3
}
