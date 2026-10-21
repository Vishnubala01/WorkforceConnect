using WorkforceConnect.Application.Interfaces;

namespace WorkforceConnect.Infrastructure.Services;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 11;

    public BcryptPasswordHasher()
    {
        DummyHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N"), WorkFactor);
    }

    public string DummyHash { get; }

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (Exception)
        {
            // A malformed stored hash must never authenticate: fail closed.
            return false;
        }
    }
}
