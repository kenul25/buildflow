using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeliveriesController : ControllerBase
{
    private readonly AppDbContext _context;

    public DeliveriesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreateDelivery(
        CreateDeliveryRequest request)
    {
        var purchaseOrder = await _context.PurchaseOrders
            .FirstOrDefaultAsync(p => p.Id == request.PurchaseOrderId);

        if (purchaseOrder == null)
        {
            return NotFound(new
            {
                message = "Purchase order not found"
            });
        }

        if (purchaseOrder.Status == "Cancelled")
        {
            return BadRequest(new
            {
                message = "Cannot create delivery for a cancelled purchase order"
            });
        }

        if (request.Quantity <= 0)
        {
            return BadRequest(new
            {
                message = "Quantity must be greater than zero"
            });
        }

        if (request.Quantity > purchaseOrder.Quantity)
        {
            return BadRequest(new
            {
                message = "Delivery quantity cannot exceed purchase order quantity"
            });
        }

        if (request.DeliveryDate < purchaseOrder.CreatedAt)
        {
            return BadRequest(new
            {
                message = "Delivery date cannot be before purchase order creation date"
            });
        }

        var delivery = new Delivery
        {
            PurchaseOrderId = purchaseOrder.Id,
            MaterialName = purchaseOrder.MaterialName,
            Quantity = request.Quantity,
            DeliveryDate = request.DeliveryDate,
            Status = "Pending",
            EvidenceUrl = request.EvidenceUrl ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        _context.Deliveries.Add(delivery);

        await _context.SaveChangesAsync();

        return Ok(delivery);
    }

    [HttpGet]
    public async Task<IActionResult> GetDeliveries()
    {
        var deliveries = await _context.Deliveries
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return Ok(deliveries);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDelivery(int id)
    {
        var delivery = await _context.Deliveries
            .FirstOrDefaultAsync(d => d.Id == id);

        if (delivery == null)
        {
            return NotFound(new
            {
                message = "Delivery not found"
            });
        }

        return Ok(delivery);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDelivery(
        int id,
        UpdateDeliveryRequest request)
    {
        var delivery = await _context.Deliveries
            .FirstOrDefaultAsync(d => d.Id == id);

        if (delivery == null)
        {
            return NotFound(new
            {
                message = "Delivery not found"
            });
        }

        if (delivery.Status == "Completed")
        {
            return BadRequest(new
            {
                message = "Completed deliveries cannot be updated"
            });
        }

        if (request.Quantity <= 0)
        {
            return BadRequest(new
            {
                message = "Quantity must be greater than zero"
            });
        }

        var purchaseOrder = await _context.PurchaseOrders
            .FirstOrDefaultAsync(p => p.Id == delivery.PurchaseOrderId);

        if (purchaseOrder == null)
        {
            return NotFound(new
            {
                message = "Purchase order not found"
            });
        }

        if (request.Quantity > purchaseOrder.Quantity)
        {
            return BadRequest(new
            {
                message = "Delivery quantity cannot exceed purchase order quantity"
            });
        }

        delivery.Quantity = request.Quantity;
        delivery.DeliveryDate = request.DeliveryDate;
        delivery.EvidenceUrl = request.EvidenceUrl ?? string.Empty;

        await _context.SaveChangesAsync();

        return Ok(delivery);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateDeliveryStatus(
        int id,
        [FromQuery] string status)
    {
        var delivery = await _context.Deliveries
            .FirstOrDefaultAsync(d => d.Id == id);

        if (delivery == null)
        {
            return NotFound(new
            {
                message = "Delivery not found"
            });
        }

        var allowedStatuses = new[]
        {
            "Pending",
            "Received",
            "Completed",
            "Rejected"
        };

        if (!allowedStatuses.Contains(status))
        {
            return BadRequest(new
            {
                message = "Invalid delivery status"
            });
        }

        delivery.Status = status;

        await _context.SaveChangesAsync();

        return Ok(delivery);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> CancelDelivery(int id)
    {
        var delivery = await _context.Deliveries
            .FirstOrDefaultAsync(d => d.Id == id);

        if (delivery == null)
        {
            return NotFound(new
            {
                message = "Delivery not found"
            });
        }

        if (delivery.Status == "Completed")
        {
            return BadRequest(new
            {
                message = "Completed deliveries cannot be cancelled"
            });
        }

        delivery.Status = "Cancelled";

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Delivery cancelled successfully",
            delivery
        });
    }
}
