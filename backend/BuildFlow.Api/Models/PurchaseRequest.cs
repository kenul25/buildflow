namespace BuildFlow.Api.Models;

public class PurchaseRequest
{
    public int Id { get; set; }

    public string MaterialName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal EstimatedUnitPrice { get; set; }

    public decimal EstimatedTotalCost { get; set; }

    public DateTime RequiredByDate { get; set; }

    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}