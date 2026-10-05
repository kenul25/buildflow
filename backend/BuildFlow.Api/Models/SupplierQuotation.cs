namespace BuildFlow.Api.Models;

public class SupplierQuotation
{
    public Guid? MaterialId { get; set; }
    public string Unit { get; set; } = "";
    public DateTime? ValidUntil { get; set; }
    public bool IsActive { get; set; } = true;
    public int Id { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedById { get; set; }
    public Guid? UpdatedById { get; set; }

    public int SupplierId { get; set; }

    public Supplier Supplier { get; set; } = null!;

    public string MaterialName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public DateTime DeliveryDate { get; set; }

    public decimal TotalPrice { get; set; }
}
