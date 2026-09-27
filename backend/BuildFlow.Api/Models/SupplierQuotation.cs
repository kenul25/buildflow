namespace BuildFlow.Api.Models;

public class SupplierQuotation
{
    public int Id { get; set; }

    public int SupplierId { get; set; }

    public Supplier Supplier { get; set; } = null!;

    public string MaterialName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public DateTime DeliveryDate { get; set; }

    public decimal TotalPrice { get; set; }
}