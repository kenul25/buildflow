namespace BuildFlow.Api.DTOs;

public class CreatePurchaseOrderRequest
{
    public int? QuotationId { get; set; }
    public int PurchaseRequestId { get; set; }

    public int SupplierId { get; set; }

    public decimal UnitPrice { get; set; }

    public DateTime DeliveryDate { get; set; }
}
