using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WorkforceConnect.Application.Common;
using WorkforceConnect.Application.Interfaces;
using WorkforceConnect.Application.Security;
using WorkforceConnect.Application.Services;
using WorkforceConnect.Infrastructure.Data;

namespace WorkforceConnect.Tests;

public static class Emails
{
    public const string Admin = "admin@workforceconnect.local";
    public const string Manager = "manager@workforceconnect.local";
    public const string TeamLead = "teamlead@workforceconnect.local";
    public const string Employee = "employee@workforceconnect.local";
}

public sealed class FakeCurrentUser : ICurrentUser
{
    private int? _userId;

    public bool IsAuthenticated => _userId.HasValue;

    public int UserId => _userId ?? throw new UnauthenticatedException();

    public void SignIn(int userId) => _userId = userId;
}

public sealed class FakePasswordHasher : IPasswordHasher
{
    public string DummyHash => "hash:dummy-value";

    public string Hash(string password) => "hash:" + password;

    public bool Verify(string password, string hash) => hash == "hash:" + password;
}

public sealed class FakeJwtTokenService : IJwtTokenService
{
    public AccessToken CreateToken(int userId, string email, string roleName) =>
        new($"token-for-{userId}", DateTime.UtcNow.AddHours(1));
}

/// <summary>
/// A seeded in-memory database plus factories for the services, so each test reads like
/// "sign in as X, call the service, check the result".
/// Seeded users: admin (SuperAdmin), manager (NNX), teamlead (NNX/S1), employee (NNX/S1).
/// </summary>
public sealed class TestEnv
{
    public const string AdminPassword = "Admin!12345";
    public const string DemoPassword = "Demo!12345";

    private TestEnv(AppDbContext db)
    {
        Db = db;
    }

    public AppDbContext Db { get; }

    public FakeCurrentUser Current { get; } = new();

    public static async Task<TestEnv> CreateAsync(bool seedAdmin = true, bool demoData = true)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AppDbContext(options);
        var env = new TestEnv(db);

        await env.NewSeeder(seedAdmin, demoData).SeedAsync();
        return env;
    }

    public DbSeeder NewSeeder(bool seedAdmin = true, bool demoData = true) => new(
        Db,
        new FakePasswordHasher(),
        Options.Create(new SeedOptions
        {
            AdminPassword = seedAdmin ? AdminPassword : string.Empty,
            DemoData = demoData,
            DemoPassword = DemoPassword
        }),
        NullLogger<DbSeeder>.Instance);

    public async Task<TestEnv> SignInAsync(string email)
    {
        var id = await Db.Users.Where(u => u.Email == email).Select(u => u.Id).SingleAsync();
        Current.SignIn(id);
        return this;
    }

    // Services are created per call so each one starts with a fresh per-request scope cache.
    public IDataScopeService Scopes => new DataScopeService(Db, Current);

    public IPermissionChecker Permissions => new PermissionChecker(Db);

    public OrganizationService Organizations => new(Db, Scopes);

    public TeamService Teams => new(Db, Scopes);

    public EmployeeService Employees => new(Db, Scopes, new FakePasswordHasher());

    public AuthService Auth => new(Db, new FakePasswordHasher(), new FakeJwtTokenService(), Permissions, Scopes, TimeProvider.System);

    public int OrganizationId(string name) => Db.Organizations.Where(o => o.Name == name).Select(o => o.Id).Single();

    public int TeamId(string name) => Db.Teams.Where(t => t.Name == name).Select(t => t.Id).Single();

    public int EmployeeId(string email) => Db.Employees.Where(e => e.Email == email).Select(e => e.Id).Single();

    public int UserId(string email) => Db.Users.Where(u => u.Email == email).Select(u => u.Id).Single();
}
