namespace BuildFlow.Api.Models;

public class PurchaseOrder
{
    public int? QuotationId { get; set; }
    public Guid? MaterialId { get; set; }
    public Guid? ProjectId { get; set; }
    public string Unit { get; set; } = "";
    public int Id { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedById { get; set; }
    public Guid? UpdatedById { get; set; }

    public int PurchaseRequestId { get; set; }

    public int SupplierId { get; set; }

    public string MaterialName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalCost { get; set; }

    public DateTime DeliveryDate { get; set; }

    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
