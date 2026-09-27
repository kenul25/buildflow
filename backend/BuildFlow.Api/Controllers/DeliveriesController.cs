using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
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

        if (purchaseOrder.Status != "Pending")
        {
            return BadRequest(new
            {
                message = "Only pending purchase orders can have deliveries created"
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

        if (request.DeliveryDate <= DateTime.UtcNow)
        {
            return BadRequest(new
            {
                message = "Delivery date must be in the future"
            });
        }

        if (request.DeliveryDate > purchaseOrder.DeliveryDate)
        {
            return BadRequest(new
            {
                message = "Delivery date cannot be after the purchase order delivery date"
            });
        }

        var delivery = new BuildFlow.Api.Models.Delivery
        {
            PurchaseOrderId = purchaseOrder.Id,
            MaterialName = purchaseOrder.MaterialName,
            Quantity = request.Quantity,
            DeliveryDate = request.DeliveryDate,
            Status = "Pending",
            EvidenceUrl = request.EvidenceUrl,
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

        var purchaseOrder = await _context.PurchaseOrders
            .FirstOrDefaultAsync(p => p.Id == delivery.PurchaseOrderId);

        if (purchaseOrder == null)
        {
            return NotFound(new
            {
                message = "Purchase order not found"
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

        if (request.DeliveryDate <= DateTime.UtcNow)
        {
            return BadRequest(new
            {
                message = "Delivery date must be in the future"
            });
        }

        if (request.DeliveryDate > purchaseOrder.DeliveryDate)
        {
            return BadRequest(new
            {
                message = "Delivery date cannot be after the purchase order delivery date"
            });
        }

        delivery.Quantity = request.Quantity;
        delivery.DeliveryDate = request.DeliveryDate;
        delivery.EvidenceUrl = request.EvidenceUrl;

        await _context.SaveChangesAsync();

        return Ok(delivery);
    }

    [HttpPut("{id}/confirm")]
    public async Task<IActionResult> ConfirmDelivery(int id)
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
                message = "Delivery is already completed"
            });
        }

        if (delivery.Status == "Cancelled")
        {
            return BadRequest(new
            {
                message = "Cancelled deliveries cannot be completed"
            });
        }

        delivery.Status = "Completed";

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Delivery confirmed successfully",
            delivery
        });
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

        if (delivery.Status == "Cancelled")
        {
            return BadRequest(new
            {
                message = "Delivery is already cancelled"
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