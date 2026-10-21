using Microsoft.EntityFrameworkCore;
using WorkforceConnect.Application.Common;
using WorkforceConnect.Application.DTOs;
using WorkforceConnect.Application.Interfaces;

namespace WorkforceConnect.Application.Services;

public sealed class AuthService(
    IApplicationDbContext db,
    IPasswordHasher hasher,
    IJwtTokenService tokens,
    IPermissionChecker permissions,
    IDataScopeService scopes,
    TimeProvider clock) : IAuthService
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim();

        var user = await db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email, ct);

        // Always verify a hash, even for unknown emails, so response time does not reveal which emails exist.
        var passwordMatches = hasher.Verify(request.Password, user?.PasswordHash ?? hasher.DummyHash);

        if (user is null || !passwordMatches || !user.IsActive)
        {
            throw new InvalidCredentialsException();
        }

        user.LastLoginAt = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        var token = tokens.CreateToken(user.Id, user.Email, user.Role.Name);
        var currentUser = await GetCurrentUserAsync(user.Id, ct);

        return new LoginResponse(token.Value, "Bearer", token.ExpiresAt, currentUser);
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync(int userId, CancellationToken ct = default)
    {
        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, ct)
            ?? throw new UnauthenticatedException();

        var permissionNames = (await permissions.GetPermissionsAsync(userId, ct))
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        var scope = await scopes.GetForUserAsync(userId, ct);

        NamedRef? organization = null;
        NamedRef? team = null;

        if (user.Employee is { } employee)
        {
            var organizationId = employee.OrganizationId;
            organization = await db.Organizations
                .AsNoTracking()
                .Where(o => o.Id == organizationId)
                .Select(o => new NamedRef(o.Id, o.Name))
                .FirstOrDefaultAsync(ct);

            if (employee.TeamId is int teamId)
            {
                team = await db.Teams
                    .AsNoTracking()
                    .Where(t => t.Id == teamId)
                    .Select(t => new NamedRef(t.Id, t.Name))
                    .FirstOrDefaultAsync(ct);
            }
        }

        return new CurrentUserResponse(
            user.Id,
            user.Employee?.FullName ?? user.Email,
            user.Email,
            user.Role.Name,
            permissionNames,
            new DataScopeResponse(
                scope.Level.ToString(),
                scope.OrganizationIds.OrderBy(i => i).ToList(),
                scope.TeamIds.OrderBy(i => i).ToList()),
            user.EmployeeId,
            organization,
            team);
    }
}
