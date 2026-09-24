namespace BuildFlow.Api.DTOs;

public class ProcurementComparisonResponse
{
    public string MaterialName { get; set; } = string.Empty;

    public int RequiredQuantity { get; set; }

    public int AvailableQuantity { get; set; }

    public decimal TotalPrice { get; set; }

    public decimal UnitPrice { get; set; }

    public DateTime DeliveryDate { get; set; }

    public int SupplierId { get; set; }

    public string SupplierName { get; set; } = string.Empty;
}