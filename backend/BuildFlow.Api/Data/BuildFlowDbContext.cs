using BuildFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Data;

public sealed class BuildFlowDbContext(DbContextOptions<BuildFlowDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<AppUser>();
        user.ToTable("Users");
        user.HasKey(item => item.Id);
        user.Property(item => item.FullName).HasMaxLength(120).IsRequired();
        user.Property(item => item.Email).HasMaxLength(254).IsRequired();
        user.Property(item => item.NormalizedEmail).HasMaxLength(254).IsRequired();
        user.Property(item => item.PasswordHash).HasMaxLength(512).IsRequired();
        user.HasIndex(item => item.NormalizedEmail).IsUnique();

        var role = modelBuilder.Entity<Role>();
        role.ToTable("Roles");
        role.HasKey(item => item.Id);
        role.Property(item => item.Name).HasMaxLength(64).IsRequired();
        role.Property(item => item.NormalizedName).HasMaxLength(64).IsRequired();
        role.HasIndex(item => item.NormalizedName).IsUnique();

        var userRole = modelBuilder.Entity<UserRole>();
        userRole.ToTable("UserRoles");
        userRole.HasKey(item => new { item.UserId, item.RoleId });
        userRole.HasOne(item => item.User).WithMany(item => item.UserRoles).HasForeignKey(item => item.UserId);
        userRole.HasOne(item => item.Role).WithMany(item => item.UserRoles).HasForeignKey(item => item.RoleId);

        var refreshToken = modelBuilder.Entity<RefreshToken>();
        refreshToken.ToTable("RefreshTokens");
        refreshToken.HasKey(item => item.Id);
        refreshToken.Property(item => item.TokenHash).HasMaxLength(128).IsRequired();
        refreshToken.Property(item => item.ReplacedByTokenHash).HasMaxLength(128);
        refreshToken.Property(item => item.CreatedByIp).HasMaxLength(64).IsRequired();
        refreshToken.HasIndex(item => item.TokenHash).IsUnique();
        refreshToken.HasOne(item => item.User).WithMany(item => item.RefreshTokens).HasForeignKey(item => item.UserId);

        var seededAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var roleIds = new[]
        {
            Guid.Parse("51d6b708-44c3-4f67-9037-a49215525701"),
            Guid.Parse("51d6b708-44c3-4f67-9037-a49215525702"),
            Guid.Parse("51d6b708-44c3-4f67-9037-a49215525703"),
            Guid.Parse("51d6b708-44c3-4f67-9037-a49215525704"),
            Guid.Parse("51d6b708-44c3-4f67-9037-a49215525705")
        };
        role.HasData(SystemRoles.All.Select((name, index) => new Role
        {
            Id = roleIds[index],
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            CreatedAt = seededAt,
            UpdatedAt = seededAt
        }));
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
