using BuildFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Data;

public sealed class BuildFlowDbContext(DbContextOptions<BuildFlowDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<ConstructionPhase> ConstructionPhases => Set<ConstructionPhase>();
    public DbSet<ConstructionActivity> ConstructionActivities => Set<ConstructionActivity>();
    public DbSet<ProgressUpdate> ProgressUpdates => Set<ProgressUpdate>();
    public DbSet<SitePhoto> SitePhotos => Set<SitePhoto>();
    public DbSet<ResourceRequest> ResourceRequests => Set<ResourceRequest>();
    public DbSet<ResourceRequestItem> ResourceRequestItems => Set<ResourceRequestItem>();
    public DbSet<PlanningWorkflow> PlanningWorkflows => Set<PlanningWorkflow>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<InventoryReservation> InventoryReservations => Set<InventoryReservation>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

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

        foreach (var type in new[] { typeof(Project), typeof(Site), typeof(ConstructionPhase), typeof(ConstructionActivity) })
        {
            modelBuilder.Entity(type).Property(nameof(ConstructionRecord.Name)).HasMaxLength(160).IsRequired();
            modelBuilder.Entity(type).Property(nameof(ConstructionRecord.Description)).HasMaxLength(2000);
        }
        modelBuilder.Entity<Project>().HasIndex(p => p.Code).IsUnique();
        modelBuilder.Entity<Project>().Property(p => p.Code).HasMaxLength(32).IsRequired();
        modelBuilder.Entity<Project>().Property(p => p.Status).HasMaxLength(32).IsRequired();
        modelBuilder.Entity<Project>().HasOne(p => p.AssignedEngineer).WithMany().HasForeignKey(p => p.AssignedEngineerId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Site>().Property(s => s.Address).HasMaxLength(500).IsRequired();
        modelBuilder.Entity<Site>().HasOne(s => s.Project).WithMany(p => p.Sites).HasForeignKey(s => s.ProjectId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ConstructionPhase>().HasOne(p => p.Site).WithMany(s => s.Phases).HasForeignKey(p => p.SiteId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ConstructionActivity>().Property(a => a.Status).HasMaxLength(32).IsRequired();
        modelBuilder.Entity<ConstructionActivity>().HasOne(a => a.Phase).WithMany(p => p.Activities).HasForeignKey(a => a.PhaseId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ProgressUpdate>().HasOne(p => p.Activity).WithMany(a => a.ProgressUpdates).HasForeignKey(p => p.ActivityId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ProgressUpdate>().Property(p => p.WorkCompleted).HasMaxLength(2000).IsRequired();
        modelBuilder.Entity<ProgressUpdate>().Property(p => p.Blockers).HasMaxLength(2000);
        modelBuilder.Entity<SitePhoto>().HasOne(p => p.Activity).WithMany().HasForeignKey(p => p.ActivityId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SitePhoto>().Property(p => p.StorageName).HasMaxLength(100).IsRequired();
        modelBuilder.Entity<SitePhoto>().Property(p => p.OriginalName).HasMaxLength(255).IsRequired();
        modelBuilder.Entity<SitePhoto>().Property(p => p.ContentType).HasMaxLength(100).IsRequired();
        modelBuilder.Entity<ResourceRequest>().HasOne(r => r.Project).WithMany().HasForeignKey(r => r.ProjectId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ResourceRequest>().HasOne(r => r.Site).WithMany().HasForeignKey(r => r.SiteId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ResourceRequest>().HasOne(r => r.Activity).WithMany().HasForeignKey(r => r.ActivityId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ResourceRequest>().Property(r => r.Objective).HasMaxLength(2000).IsRequired();
        modelBuilder.Entity<ResourceRequest>().Property(r => r.Notes).HasMaxLength(2000);
        modelBuilder.Entity<ResourceRequest>().Property(r => r.BudgetLimit).HasPrecision(18, 2);
        modelBuilder.Entity<ResourceRequestItem>().HasOne(i => i.ResourceRequest).WithMany(r => r.Items).HasForeignKey(i => i.ResourceRequestId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ResourceRequestItem>().Property(i => i.Kind).HasMaxLength(32).IsRequired();
        modelBuilder.Entity<ResourceRequestItem>().Property(i => i.Name).HasMaxLength(160).IsRequired();
        modelBuilder.Entity<ResourceRequestItem>().Property(i => i.Unit).HasMaxLength(32).IsRequired();
        modelBuilder.Entity<ResourceRequestItem>().Property(i => i.Quantity).HasPrecision(18, 3);
        modelBuilder.Entity<PlanningWorkflow>().HasOne(w => w.ResourceRequest).WithMany().HasForeignKey(w => w.ResourceRequestId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PlanningWorkflow>().HasIndex(w => w.ResourceRequestId).IsUnique();

        var warehouse = modelBuilder.Entity<Warehouse>();
        warehouse.ToTable("Warehouses");
        warehouse.HasKey(w => w.Id);
        warehouse.Property(w => w.Name).HasMaxLength(160).IsRequired();
        warehouse.Property(w => w.Location).HasMaxLength(500);
        warehouse.HasIndex(w => w.Name).IsUnique();

        var material = modelBuilder.Entity<Material>();
        material.ToTable("Materials", table =>
        {
            table.HasCheckConstraint("CK_Materials_NonNegativeStock", "\"CurrentStock\" >= 0 AND \"ReservedStock\" >= 0");
            table.HasCheckConstraint("CK_Materials_ReservedStockWithinCurrent", "\"ReservedStock\" <= \"CurrentStock\"");
        });
        material.HasKey(m => m.Id);
        material.Property(m => m.Name).HasMaxLength(160).IsRequired();
        material.Property(m => m.Category).HasMaxLength(80).IsRequired();
        material.Property(m => m.Unit).HasMaxLength(32).IsRequired();
        material.Property(m => m.UnitPrice).HasPrecision(18, 2);
        material.Property(m => m.CurrentStock).HasPrecision(18, 3);
        material.Property(m => m.ReservedStock).HasPrecision(18, 3);
        material.HasIndex(m => new { m.Name, m.Unit, m.WarehouseId }).IsUnique();
        material.HasOne(m => m.Warehouse).WithMany(w => w.Materials).HasForeignKey(m => m.WarehouseId).OnDelete(DeleteBehavior.Restrict);

        var reservation = modelBuilder.Entity<InventoryReservation>();
        reservation.ToTable("InventoryReservations");
        reservation.HasKey(r => r.Id);
        reservation.Property(r => r.Status).HasMaxLength(32).IsRequired();
        reservation.Property(r => r.Quantity).HasPrecision(18, 3);
        reservation.HasIndex(r => new { r.MaterialId, r.Status });
        reservation.HasOne(r => r.Material).WithMany().HasForeignKey(r => r.MaterialId).OnDelete(DeleteBehavior.Restrict);

        var movement = modelBuilder.Entity<StockMovement>();
        movement.ToTable("StockMovements");
        movement.HasKey(m => m.Id);
        movement.Property(m => m.Type).HasMaxLength(32).IsRequired();
        movement.Property(m => m.Quantity).HasPrecision(18, 3);
        movement.Property(m => m.StockAfter).HasPrecision(18, 3);
        movement.Property(m => m.Reference).HasMaxLength(200);
        movement.HasIndex(m => new { m.MaterialId, m.CreatedAt });
        movement.HasOne(m => m.Material).WithMany().HasForeignKey(m => m.MaterialId).OnDelete(DeleteBehavior.Restrict);

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
        ValidateMaterials();
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

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateMaterials();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    private void ValidateMaterials()
    {
        foreach (var entry in ChangeTracker.Entries<Material>().Where(entry => entry.State is EntityState.Added or EntityState.Modified))
            entry.Entity.ValidateStock();
    }
}
