namespace WorkforceConnect.Infrastructure.Data;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>Run the idempotent seeder at startup.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Email of the first Super Admin, created only when no users exist yet.</summary>
    public string AdminEmail { get; set; } = "admin@workforceconnect.local";

    /// <summary>
    /// Initial Super Admin password. Supply it through configuration (user secrets / environment variable
    /// Seed__AdminPassword). When empty, no admin is created.
    /// </summary>
    public string AdminPassword { get; set; } = string.Empty;

    public string AdminFirstName { get; set; } = "Admin";

    public string AdminLastName { get; set; } = "User";

    /// <summary>Development convenience: also create one demo Manager, TeamLead and Employee account.</summary>
    public bool DemoData { get; set; }

    public string DemoPassword { get; set; } = string.Empty;
}
