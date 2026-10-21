using WorkforceConnect.Application.Security;

namespace WorkforceConnect.Application.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);

    /// <summary>A valid hash of a random value, verified against when the email is unknown so timing does not reveal it.</summary>
    string DummyHash { get; }
}

public interface IJwtTokenService
{
    /// <summary>Creates a signed access token. Permissions are intentionally NOT embedded in it.</summary>
    AccessToken CreateToken(int userId, string email, string roleName);
}

public sealed record AccessToken(string Value, DateTime ExpiresAt);

/// <summary>Answers "what can this user do?" from the database (cached for the current request).</summary>
public interface IPermissionChecker
{
    Task<IReadOnlySet<string>> GetPermissionsAsync(int userId, CancellationToken ct = default);

    Task<bool> HasPermissionAsync(int userId, string permission, CancellationToken ct = default);
}

/// <summary>Answers "what data can this user access?".</summary>
public interface IDataScopeService
{
    /// <summary>The scope of the authenticated caller (cached for the current request).</summary>
    Task<DataScope> GetAsync(CancellationToken ct = default);

    /// <summary>The scope of a specific user, used while building the login response.</summary>
    Task<DataScope> GetForUserAsync(int userId, CancellationToken ct = default);
}
