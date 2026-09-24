using BuildFlow.Api.Data;
using BuildFlow.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Services;

public class SchedulingValidationAgent : ISchedulingValidationAgent
{
    private readonly AppDbContext _context;

    public SchedulingValidationAgent(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string> ValidateDeliveryAsync(int deliveryId)
    {
        var delivery = await _context.Deliveries
            .FirstOrDefaultAsync(d => d.Id == deliveryId);

        if (delivery == null)
        {
            return "Delivery not found";
        }

        if (delivery.Status == "Completed")
        {
            return "Delivery is already completed";
        }

        if (delivery.DeliveryDate < DateTime.UtcNow)
        {
            return "Delivery date has passed";
        }

        if (delivery.Quantity <= 0)
        {
            return "Invalid delivery quantity";
        }

        var purchaseOrder = await _context.PurchaseOrders
            .FirstOrDefaultAsync(po => po.Id == delivery.PurchaseOrderId);

        if (purchaseOrder == null)
        {
            return "Purchase order not found";
        }

        if (delivery.Quantity > purchaseOrder.Quantity)
        {
            return "Delivery quantity exceeds purchase order quantity";
        }

        return "Delivery is valid";
    }
}