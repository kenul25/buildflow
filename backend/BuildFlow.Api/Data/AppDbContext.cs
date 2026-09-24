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
    }
}