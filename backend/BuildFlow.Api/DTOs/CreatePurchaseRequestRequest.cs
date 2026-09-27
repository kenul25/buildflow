namespace BuildFlow.Api.DTOs;

public class CreatePurchaseRequestRequest
{
    public string MaterialName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public DateTime RequiredByDate { get; set; }
}