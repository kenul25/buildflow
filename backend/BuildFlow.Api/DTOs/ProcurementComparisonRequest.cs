namespace BuildFlow.Api.DTOs;

public class ProcurementComparisonRequest
{
    public string MaterialName { get; set; } = string.Empty;

    public decimal RequiredQuantity { get; set; }
    public DateTime? RequiredByDate { get; set; }
    public decimal? BudgetLimit { get; set; }
}
