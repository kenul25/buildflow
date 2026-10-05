namespace BuildFlow.Api.DTOs;

public sealed record LegacyProcurementLinksRequest(Guid ProjectId, Guid MaterialId);

public class CreatePurchaseRequestRequest
{
    public Guid? ProjectId { get; set; }
    public Guid? MaterialId { get; set; }
    public decimal? BudgetLimit { get; set; }
    public string MaterialName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public DateTime RequiredByDate { get; set; }
}
