namespace BuildFlow.Api.Models;

public class SupplierMaterial
{
    public int Id { get; set; }

    public int SupplierId { get; set; }

    public Supplier Supplier { get; set; } = null!;

    public Guid MaterialId { get; set; }

    public decimal AvailableQuantity { get; set; }

    public decimal UnitPrice { get; set; }

    public int LeadTimeDays { get; set; }

    public bool IsActive { get; set; } = true;
}
