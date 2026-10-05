namespace BuildFlow.Api.Models;

public class PurchaseRequest
{
    public Guid? ProjectId { get; set; }
    public Guid? MaterialId { get; set; }
    public decimal? BudgetLimit { get; set; }
    public string Unit { get; set; } = "";
    public int Id { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedById { get; set; }
    public Guid? UpdatedById { get; set; }

    public string MaterialName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal EstimatedUnitPrice { get; set; }

    public decimal EstimatedTotalCost { get; set; }

    public DateTime RequiredByDate { get; set; }

    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
