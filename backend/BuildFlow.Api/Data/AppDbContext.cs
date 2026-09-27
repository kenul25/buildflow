using Microsoft.EntityFrameworkCore;
using BuildFlow.Api.Models;

namespace BuildFlow.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<SupplierMaterial> SupplierMaterials { get; set; }
    public DbSet<SupplierQuotation> SupplierQuotations { get; set; }
    public DbSet<PurchaseRequest> PurchaseRequests { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<Delivery> Deliveries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
}
