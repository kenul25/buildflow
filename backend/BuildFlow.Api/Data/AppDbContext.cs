using Microsoft.EntityFrameworkCore;
using BuildFlow.Api.Models;

namespace BuildFlow.Api.Data;

public class AppDbContext : DbContext
{
    private readonly IHttpContextAccessor? _http;
    public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor? http = null)
        : base(options)
    {
        _http = http;
    }

    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<SupplierMaterial> SupplierMaterials { get; set; }
    public DbSet<SupplierQuotation> SupplierQuotations { get; set; }
    public DbSet<PurchaseRequest> PurchaseRequests { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<Delivery> Deliveries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Material>().ToTable("Materials", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<Warehouse>().ToTable("Warehouses", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<Project>().ToTable("Projects", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<StockMovement>().ToTable("StockMovements", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<StockMovement>().HasOne(x => x.Material).WithMany().HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict);
        // External tables are owned and migrated exclusively by BuildFlowDbContext.
        modelBuilder.Entity<Project>().Ignore(x => x.Sites).Ignore(x => x.AssignedEngineer);
        modelBuilder.Entity<Material>().HasOne(x => x.Warehouse).WithMany(x => x.Materials).HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierMaterial>().HasOne<Material>().WithMany().HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierQuotation>().HasOne<Material>().WithMany().HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PurchaseRequest>().HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PurchaseRequest>().HasOne<Material>().WithMany().HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PurchaseOrder>().HasOne<PurchaseRequest>().WithMany().HasForeignKey(x => x.PurchaseRequestId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PurchaseOrder>().HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PurchaseOrder>().HasOne<SupplierQuotation>().WithMany().HasForeignKey(x => x.QuotationId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PurchaseOrder>().HasIndex(x => x.PurchaseRequestId).IsUnique().HasFilter("\"Status\" <> 'Cancelled'");
        modelBuilder.Entity<Delivery>().HasOne<PurchaseOrder>().WithMany().HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
        foreach (var type in new[] { typeof(PurchaseRequest), typeof(PurchaseOrder), typeof(Delivery), typeof(SupplierQuotation) })
            modelBuilder.Entity(type).Property("Quantity").HasPrecision(18, 3);
        modelBuilder.Entity<SupplierQuotation>()
            .HasOne(q => q.Supplier)
            .WithMany()
            .HasForeignKey(q => q.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SupplierMaterial>()
            .HasOne(sm => sm.Supplier)
            .WithMany()
            .HasForeignKey(sm => sm.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SupplierMaterial>()
            .Property(sm => sm.AvailableQuantity)
            .HasPrecision(18, 3);

        modelBuilder.Entity<SupplierMaterial>()
            .Property(sm => sm.UnitPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<SupplierMaterial>()
            .HasIndex(sm => new { sm.SupplierId, sm.MaterialId })
            .IsUnique();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries().Where(x => x.State is EntityState.Added or EntityState.Modified))
        {
            var property = entry.Metadata.FindProperty("UpdatedAt");
            if (property != null) entry.Property("UpdatedAt").CurrentValue = DateTimeOffset.UtcNow;
            var actorText = _http?.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(actorText, out var actor))
            {
                if (entry.Metadata.FindProperty("UpdatedById") != null) entry.Property("UpdatedById").CurrentValue = actor;
                if (entry.State == EntityState.Added && entry.Metadata.FindProperty("CreatedById") != null) entry.Property("CreatedById").CurrentValue = actor;
            }
            foreach (var date in entry.Properties.Where(x => x.Metadata.ClrType == typeof(DateTime) || x.Metadata.ClrType == typeof(DateTime?)))
                if (date.CurrentValue is DateTime value) date.CurrentValue = value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
