namespace BuildFlow.Api.DTOs;

public class UpdateDeliveryRequest
{
    public int Quantity { get; set; }

    public DateTime DeliveryDate { get; set; }

    public string EvidenceUrl { get; set; } = string.Empty;
}