using Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(user => user.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(user => user.IsSuperuser).IsRequired();
        builder.Property(user => user.IsStaff).IsRequired();
        builder.Property(user => user.IsActive).IsRequired();
        builder.Property(user => user.CreatedAt).IsRequired();
        builder.HasIndex(user => user.NormalizedEmail).IsUnique().HasDatabaseName("EmailIndex");
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.Property(role => role.GuardName)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("web");

        builder.Property(role => role.Description).HasMaxLength(300);
    }
}

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(permission => permission.Id);

        builder.Property(permission => permission.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(permission => permission.GuardName)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("web");

        builder.HasIndex(permission => new { permission.Name, permission.GuardName })
            .IsUnique();
    }
}

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(rolePermission => new { rolePermission.RoleId, rolePermission.PermissionId });

        builder.HasOne(rolePermission => rolePermission.Role)
            .WithMany(role => role.Permissions)
            .HasForeignKey(rolePermission => rolePermission.RoleId);

        builder.HasOne(rolePermission => rolePermission.Permission)
            .WithMany(permission => permission.Roles)
            .HasForeignKey(rolePermission => rolePermission.PermissionId);
    }
}

public sealed class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.ToTable("UserPermissions");
        builder.HasKey(userPermission => new { userPermission.UserId, userPermission.PermissionId });

        builder.HasOne(userPermission => userPermission.User)
            .WithMany(user => user.DirectPermissions)
            .HasForeignKey(userPermission => userPermission.UserId);

        builder.HasOne(userPermission => userPermission.Permission)
            .WithMany(permission => permission.Users)
            .HasForeignKey(userPermission => userPermission.PermissionId);
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(token => token.Id);

        builder.Property(token => token.Token)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(token => token.Token)
            .IsUnique();

        builder.Property(token => token.CreatedAt).IsRequired();
        builder.Property(token => token.ExpiresAt).IsRequired();

        builder.HasOne(token => token.User)
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(token => token.IsActive);
    }
}
