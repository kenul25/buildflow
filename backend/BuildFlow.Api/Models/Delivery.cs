namespace BuildFlow.Api.Models;

public class Delivery
{
    public int Id { get; set; }

    public int PurchaseOrderId { get; set; }

    public string MaterialName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public DateTime DeliveryDate { get; set; }

    public string Status { get; set; } = "Pending";

    public string EvidenceUrl { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}