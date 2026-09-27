using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly AppDbContext _context;

    public PurchaseOrdersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreatePurchaseOrder(
        CreatePurchaseOrderRequest request)
    {
        var purchaseRequest = await _context.PurchaseRequests
            .FirstOrDefaultAsync(p => p.Id == request.PurchaseRequestId);

        if (purchaseRequest == null)
        {
            return NotFound(new
            {
                message = "Purchase request not found"
            });
        }

        if (purchaseRequest.Status != "Approved")
        {
            return BadRequest(new
            {
                message = "Only approved purchase requests can be converted to purchase orders"
            });
        }

        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == request.SupplierId);

        if (supplier == null)
        {
            return NotFound(new
            {
                message = "Supplier not found"
            });
        }

        if (!supplier.IsActive)
        {
            return BadRequest(new
            {
                message = "Supplier is not active"
            });
        }

        if (request.UnitPrice <= 0)
        {
            return BadRequest(new
            {
                message = "Unit price must be greater than zero"
            });
        }

        if (request.DeliveryDate <= DateTime.UtcNow)
        {
            return BadRequest(new
            {
                message = "Delivery date must be in the future"
            });
        }

        if (request.DeliveryDate > purchaseRequest.RequiredByDate)
        {
            return BadRequest(new
            {
                message = "Delivery date cannot be after the required by date"
            });
        }

        var existingOrder = await _context.PurchaseOrders
            .FirstOrDefaultAsync(p => p.PurchaseRequestId == request.PurchaseRequestId);

        if (existingOrder != null)
        {
            return BadRequest(new
            {
                message = "A purchase order already exists for this purchase request"
            });
        }

        var totalCost = purchaseRequest.Quantity * request.UnitPrice;

        var purchaseOrder = new BuildFlow.Api.Models.PurchaseOrder
        {
            PurchaseRequestId = purchaseRequest.Id,
            SupplierId = supplier.Id,
            MaterialName = purchaseRequest.MaterialName,
            Quantity = purchaseRequest.Quantity,
            UnitPrice = request.UnitPrice,
            TotalCost = totalCost,
            DeliveryDate = request.DeliveryDate,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.PurchaseOrders.Add(purchaseOrder);

        purchaseRequest.Status = "ConvertedToOrder";

        await _context.SaveChangesAsync();

        return Ok(purchaseOrder);
    }

    [HttpGet]
    public async Task<IActionResult> GetPurchaseOrders()
    {
        var purchaseOrders = await _context.PurchaseOrders
            .ToListAsync();

        return Ok(purchaseOrders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPurchaseOrder(int id)
    {
        var purchaseOrder = await _context.PurchaseOrders
            .FirstOrDefaultAsync(p => p.Id == id);

        if (purchaseOrder == null)
        {
            return NotFound(new
            {
                message = "Purchase order not found"
            });
        }

        return Ok(purchaseOrder);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePurchaseOrder(
        int id,
        CreatePurchaseOrderRequest request)
    {
        var purchaseOrder = await _context.PurchaseOrders
            .FirstOrDefaultAsync(p => p.Id == id);

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
                message = "Only pending purchase orders can be updated"
            });
        }

        var purchaseRequest = await _context.PurchaseRequests
            .FirstOrDefaultAsync(p => p.Id == request.PurchaseRequestId);

         if (purchaseRequest == null)
        {
            return NotFound(new
            {
                message = "Purchase request not found"
            });
        }

        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == request.SupplierId);

        if (supplier == null)
        {
            return NotFound(new
            {
                message = "Supplier not found"
            });
        }
 
        if (!supplier.IsActive)
        {
            return BadRequest(new
            {
                message = "Supplier is not active"
            });
        }

        if (request.UnitPrice <= 0)
        {
            return BadRequest(new
            {
                message = "Unit price must be greater than zero"
            });
        }

        if (request.DeliveryDate <= DateTime.UtcNow)
        {
            return BadRequest(new
            {
                message = "Delivery date must be in the future"
            });
        }

        if (request.DeliveryDate > purchaseRequest.RequiredByDate)
        {
            return BadRequest(new
            {
                message = "Delivery date cannot be after the required by date"
            });
        }

        purchaseOrder.PurchaseRequestId = purchaseRequest.Id;
        purchaseOrder.SupplierId = supplier.Id;
        purchaseOrder.MaterialName = purchaseRequest.MaterialName;
        purchaseOrder.Quantity = purchaseRequest.Quantity;
        purchaseOrder.UnitPrice = request.UnitPrice;
        purchaseOrder.TotalCost = purchaseRequest.Quantity * request.UnitPrice;
        purchaseOrder.DeliveryDate = request.DeliveryDate;

        await _context.SaveChangesAsync();

        return Ok(purchaseOrder);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> CancelPurchaseOrder(int id)
    {
        var purchaseOrder = await _context.PurchaseOrders
            .FirstOrDefaultAsync(p => p.Id == id);

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
                message = "Only pending purchase orders can be cancelled"
            });
        }

       purchaseOrder.Status = "Cancelled";

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Purchase order cancelled successfully",
            purchaseOrder
        });
    }
}