namespace BuildFlow.Api.DTOs;

public class CreateQuotationRequest
{
    public Guid? MaterialId { get; set; }
    public DateTime? ValidUntil { get; set; }
    public int SupplierId { get; set; }

    public string MaterialName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public DateTime DeliveryDate { get; set; }
}
