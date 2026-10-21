using Microsoft.EntityFrameworkCore;
using WorkforceConnect.Domain.Entities;

namespace WorkforceConnect.Application.Interfaces;

/// <summary>The slice of the database the application services need. Implemented by Infrastructure.</summary>
public interface IApplicationDbContext
{
    DbSet<Role> Roles { get; }

    DbSet<Permission> Permissions { get; }

    DbSet<RolePermission> RolePermissions { get; }

    DbSet<Organization> Organizations { get; }

    DbSet<Team> Teams { get; }

    DbSet<Employee> Employees { get; }

    DbSet<User> Users { get; }

    DbSet<UserOrganizationAccess> UserOrganizationAccess { get; }

    DbSet<UserTeamAccess> UserTeamAccess { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
