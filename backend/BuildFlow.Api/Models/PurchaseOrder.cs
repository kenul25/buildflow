namespace BuildFlow.Api.Models;

public class PurchaseOrder
{
    public int Id { get; set; }

    public int PurchaseRequestId { get; set; }

    public int SupplierId { get; set; }

    public string MaterialName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalCost { get; set; }

    public DateTime DeliveryDate { get; set; }

    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}