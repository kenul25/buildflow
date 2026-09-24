namespace BuildFlow.Api.Interfaces;

public interface ISchedulingValidationAgent
{
    Task<string> ValidateDeliveryAsync(int deliveryId);
}
