using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WorkforceConnect.Application.Interfaces;
using WorkforceConnect.Infrastructure.Data;
using WorkforceConnect.Infrastructure.Services;

namespace WorkforceConnect.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Default is not configured. Set it in user secrets or the ConnectionStrings__Default environment variable.");
        }

        // An explicit server version avoids connecting to MySQL while the app is still starting up
        // (and works before the database exists).
        var serverVersion = new MySqlServerVersion(Version.Parse(configuration["Database:ServerVersion"] ?? "8.0.36"));

        services.AddDbContext<AppDbContext>(options => options.UseMySql(connectionString, serverVersion));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));
        services.AddScoped<DbSeeder>();

        return services;
    }

    /// <summary>
    /// Creates the schema when Database:AutoCreate is true (development), then runs the idempotent seeder.
    /// In production, apply database/schema.sql and leave AutoCreate off.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (configuration.GetValue<bool>("Database:AutoCreate"))
        {
            await db.Database.EnsureCreatedAsync();

            const string createDailyStatusesSql = @"
CREATE TABLE IF NOT EXISTS DailyStatuses (
  Id INT NOT NULL AUTO_INCREMENT,
  Date DATE NOT NULL,
  EmployeeId INT NOT NULL,
  OrganizationId INT NOT NULL,
  TeamId INT NULL,
  ProjectOrTeamName VARCHAR(150) NULL,
  WorkItemId VARCHAR(100) NULL,
  TaskActivity VARCHAR(500) NOT NULL,
  PlannedWorkForToday VARCHAR(2000) NOT NULL,
  WorkCompletedProgress VARCHAR(2000) NOT NULL,
  Status VARCHAR(50) NOT NULL DEFAULT 'In Progress',
  HoursSpent DECIMAL(5,2) NOT NULL DEFAULT 0.00,
  BlockerDependency VARCHAR(1000) NULL,
  SupportRequired VARCHAR(1000) NULL,
  ReviewerLead VARCHAR(150) NULL,
  Remarks VARCHAR(1000) NULL,
  CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  UpdatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  PRIMARY KEY (Id),
  KEY IX_DailyStatuses_Date_EmployeeId (Date, EmployeeId),
  KEY IX_DailyStatuses_OrganizationId (OrganizationId),
  KEY IX_DailyStatuses_TeamId (TeamId),
  CONSTRAINT FK_DailyStatuses_Employees
    FOREIGN KEY (EmployeeId) REFERENCES Employees (Id) ON DELETE CASCADE,
  CONSTRAINT FK_DailyStatuses_Organizations
    FOREIGN KEY (OrganizationId) REFERENCES Organizations (Id) ON DELETE CASCADE,
  CONSTRAINT FK_DailyStatuses_Teams
    FOREIGN KEY (TeamId) REFERENCES Teams (Id) ON DELETE SET NULL
) ENGINE=InnoDB;";

            try
            {
                await db.Database.ExecuteSqlRawAsync(createDailyStatusesSql);
            }
            catch
            {
                // Ignore if table or constraints already exist
            }
        }

        var seedOptions = scope.ServiceProvider.GetRequiredService<IOptions<SeedOptions>>().Value;
        if (seedOptions.Enabled)
        {
            await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync();
        }

        // Ensure cross-functional teams (QA and DevOps) exist across all active organizations in the database
        await EnsureCrossFunctionalTeamsAsync(db);
    }

    private static async Task EnsureCrossFunctionalTeamsAsync(AppDbContext db)
    {
        var existingOrgs = await db.Organizations.Include(o => o.Teams).Where(o => o.IsActive).ToListAsync();
        var addedAny = false;

        foreach (var org in existingOrgs)
        {
            foreach (var teamName in new[] { "QA", "DevOps" })
            {
                if (!org.Teams.Any(t => t.Name.Equals(teamName, StringComparison.OrdinalIgnoreCase)))
                {
                    org.Teams.Add(new Domain.Entities.Team
                    {
                        Name = teamName,
                        Description = $"{teamName} cross-functional team",
                        IsActive = true
                    });
                    addedAny = true;
                }
            }
        }

        if (addedAny)
        {
            await db.SaveChangesAsync();
        }
    }
}
