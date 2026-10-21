using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkforceConnect.Domain.Entities;

namespace WorkforceConnect.Infrastructure.Configurations;

// Column sizes and constraints mirror database/schema.sql.

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).IsRequired().HasMaxLength(50);
        builder.Property(r => r.Description).HasMaxLength(250);
        builder.Property(r => r.DataScope).HasConversion<string>().IsRequired().HasMaxLength(20);

        builder.HasIndex(r => r.Name).IsUnique();
    }
}

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Module).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Description).HasMaxLength(250);

        builder.HasIndex(p => p.Name).IsUnique();
    }
}

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

        builder.HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name).IsRequired().HasMaxLength(100);
        builder.Property(o => o.Description).HasMaxLength(500);

        builder.HasIndex(o => o.Name).IsUnique();
    }
}

public sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Teams");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.Property(t => t.Description).HasMaxLength(500);

        builder.HasIndex(t => new { t.OrganizationId, t.Name }).IsUnique();

        builder.HasOne(t => t.Organization)
            .WithMany(o => o.Teams)
            .HasForeignKey(t => t.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EmployeeCode).IsRequired().HasMaxLength(30);
        builder.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.LastName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Email).IsRequired().HasMaxLength(256);
        builder.Property(e => e.JobTitle).HasMaxLength(100);

        builder.Ignore(e => e.FullName);

        builder.HasIndex(e => e.EmployeeCode).IsUnique();
        builder.HasIndex(e => e.Email).IsUnique();

        builder.HasOne(e => e.Organization)
            .WithMany(o => o.Employees)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Team)
            .WithMany(t => t.Employees)
            .HasForeignKey(e => e.TeamId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.SecondaryOrganizations)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "EmployeeSecondaryOrganizations",
                j => j.HasOne<Organization>().WithMany().HasForeignKey("OrganizationId").OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne<Employee>().WithMany().HasForeignKey("EmployeeId").OnDelete(DeleteBehavior.Cascade)
            );

        builder.HasMany(e => e.SecondaryTeams)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "EmployeeSecondaryTeams",
                j => j.HasOne<Team>().WithMany().HasForeignKey("TeamId").OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne<Employee>().WithMany().HasForeignKey("EmployeeId").OnDelete(DeleteBehavior.Cascade)
            );
    }
}

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(100);

        builder.HasIndex(u => u.Email).IsUnique();

        builder.HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // One login per employee. The unique index on EmployeeId comes from the one-to-one mapping.
        builder.HasOne(u => u.Employee)
            .WithOne(e => e.User)
            .HasForeignKey<User>(u => u.EmployeeId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class UserOrganizationAccessConfiguration : IEntityTypeConfiguration<UserOrganizationAccess>
{
    public void Configure(EntityTypeBuilder<UserOrganizationAccess> builder)
    {
        builder.ToTable("UserOrganizationAccess");
        builder.HasKey(a => new { a.UserId, a.OrganizationId });

        builder.HasOne(a => a.User)
            .WithMany(u => u.OrganizationAccess)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Organization)
            .WithMany()
            .HasForeignKey(a => a.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class UserTeamAccessConfiguration : IEntityTypeConfiguration<UserTeamAccess>
{
    public void Configure(EntityTypeBuilder<UserTeamAccess> builder)
    {
        builder.ToTable("UserTeamAccess");
        builder.HasKey(a => new { a.UserId, a.TeamId });

        builder.HasOne(a => a.User)
            .WithMany(u => u.TeamAccess)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Team)
            .WithMany()
            .HasForeignKey(a => a.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
