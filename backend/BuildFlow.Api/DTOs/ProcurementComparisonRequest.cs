namespace BuildFlow.Api.DTOs;

public class ProcurementComparisonRequest
{
    public string MaterialName { get; set; } = string.Empty;

    public int RequiredQuantity { get; set; }
}