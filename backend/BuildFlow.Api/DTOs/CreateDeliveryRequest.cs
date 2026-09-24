namespace BuildFlow.Api.DTOs;

public class CreateDeliveryRequest
{
    public int PurchaseOrderId { get; set; }

    public int Quantity { get; set; }

    public DateTime DeliveryDate { get; set; }

    public string EvidenceUrl { get; set; } = string.Empty;
}