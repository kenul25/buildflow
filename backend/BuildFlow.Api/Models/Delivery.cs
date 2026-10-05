namespace BuildFlow.Api.Models;

public class Delivery
{
    public DateTimeOffset? StockReceivedAt { get; set; }
    public int Id { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedById { get; set; }
    public Guid? UpdatedById { get; set; }

    public int PurchaseOrderId { get; set; }

    public string MaterialName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public DateTime DeliveryDate { get; set; }

    public string Status { get; set; } = "Pending";

    public string EvidenceUrl { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
