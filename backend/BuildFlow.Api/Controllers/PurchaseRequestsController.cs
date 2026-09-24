using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PurchaseRequestsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PurchaseRequestsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreatePurchaseRequest(
        CreatePurchaseRequestRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.MaterialName))
        {
            return BadRequest(new
            {
                message = "Material name is required"
            });
        }

        if (request.Quantity <= 0)
        {
            return BadRequest(new
            {
                message = "Quantity must be greater than zero"
            });
        }

        if (request.RequiredByDate <= DateTime.UtcNow)
        {
            return BadRequest(new
            {
                message = "Required by date must be in the future"
            });
        }

        var purchaseRequest = new BuildFlow.Api.Models.PurchaseRequest
        {
            MaterialName = request.MaterialName,
            Quantity = request.Quantity,
            RequiredByDate = request.RequiredByDate
        };

        _context.PurchaseRequests.Add(purchaseRequest);

        await _context.SaveChangesAsync();

        return Ok(purchaseRequest);
    }

    [HttpGet]
    public async Task<IActionResult> GetPurchaseRequests()
    {
        var purchaseRequests = await _context.PurchaseRequests
            .ToListAsync();

        return Ok(purchaseRequests);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPurchaseRequest(int id)
    {
        var purchaseRequest = await _context.PurchaseRequests
            .FirstOrDefaultAsync(p => p.Id == id);

        if (purchaseRequest == null)
        {
            return NotFound(new
            {
                message = "Purchase request not found"
            });
        }

        return Ok(purchaseRequest);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePurchaseRequest(
        int id,
        UpdatePurchaseRequestRequest request)
    {
        var purchaseRequest = await _context.PurchaseRequests
            .FirstOrDefaultAsync(p => p.Id == id);

        if (purchaseRequest == null)
        {
            return NotFound(new
            {
                message = "Purchase request not found"
            });
        }

        if (string.IsNullOrWhiteSpace(request.MaterialName))
        {
            return BadRequest(new
            {
                message = "Material name is required"
            });
        }

        if (request.Quantity <= 0)
        {
           return BadRequest(new
            {
                message = "Quantity must be greater than zero"
            });
        }

        if (request.RequiredByDate <= DateTime.UtcNow)
        {
            return BadRequest(new
            {
                message = "Required by date must be in the future"
            });
        }

        purchaseRequest.MaterialName = request.MaterialName;
        purchaseRequest.Quantity = request.Quantity;
        purchaseRequest.RequiredByDate = request.RequiredByDate;

        await _context.SaveChangesAsync();

        return Ok(purchaseRequest);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> CancelPurchaseRequest(int id)
    {
        var purchaseRequest = await _context.PurchaseRequests
            .FirstOrDefaultAsync(p => p.Id == id);

        if (purchaseRequest == null)
        {
            return NotFound(new
            {
                message = "Purchase request not found"
            });
        }

        if (purchaseRequest.Status != "Pending")
        {
            return BadRequest(new
            {
                message = "Only pending purchase requests can be cancelled"
            });
        }

        purchaseRequest.Status = "Cancelled";

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Purchase request cancelled successfully",
            purchaseRequest
        });
    }

    [HttpPut("{id}/approve")]
    public async Task<IActionResult> ApprovePurchaseRequest(int id)
    {
        var purchaseRequest = await _context.PurchaseRequests
            .FirstOrDefaultAsync(p => p.Id == id);

        if (purchaseRequest == null)
        {
            return NotFound(new
            {
                message = "Purchase request not found"
            });
        }

        if (purchaseRequest.Status != "Pending")
        {
            return BadRequest(new
            {
                message = "Only pending purchase requests can be approved"
            });
        }

        purchaseRequest.Status = "Approved";

        await _context.SaveChangesAsync();

        return Ok(purchaseRequest);
    }
}