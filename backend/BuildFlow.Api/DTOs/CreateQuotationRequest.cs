namespace BuildFlow.Api.DTOs;

public class CreateQuotationRequest
{
    public int SupplierId { get; set; }

    public string MaterialName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public DateTime DeliveryDate { get; set; }
}