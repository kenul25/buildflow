using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ProcurementOfficer,ProjectManager,Administrator")]
public class PurchaseRequestsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PurchaseRequestsController(AppDbContext context)
    {
        _context = context;
    }

    // CREATE
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

        var purchaseRequest = new PurchaseRequest
        {
            MaterialName = request.MaterialName.Trim(),
            Quantity = request.Quantity,
            RequiredByDate = request.RequiredByDate,
            Status = "Pending"
        };

        _context.PurchaseRequests.Add(purchaseRequest);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetPurchaseRequest),
            new { id = purchaseRequest.Id },
            purchaseRequest);
    }

    // GET ALL
    [HttpGet]
    public async Task<IActionResult> GetPurchaseRequests(
        string? search = null,
        string? status = null,
        string sort = "createdAt",
        bool desc = true)
    {
        var query = _context.PurchaseRequests
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLower();

            query = query.Where(p =>
                p.MaterialName.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(p =>
                p.Status.ToLower() == status.Trim().ToLower());
        }

        query = sort.ToLower() switch
        {
            "material" =>
                desc
                    ? query.OrderByDescending(p => p.MaterialName)
                    : query.OrderBy(p => p.MaterialName),

            "quantity" =>
                desc
                    ? query.OrderByDescending(p => p.Quantity)
                    : query.OrderBy(p => p.Quantity),

            "requiredbydate" =>
                desc
                    ? query.OrderByDescending(p => p.RequiredByDate)
                    : query.OrderBy(p => p.RequiredByDate),

            "status" =>
                desc
                    ? query.OrderByDescending(p => p.Status)
                    : query.OrderBy(p => p.Status),

            _ =>
                desc
                    ? query.OrderByDescending(p => p.CreatedAt)
                    : query.OrderBy(p => p.CreatedAt)
        };

        var purchaseRequests = await query.ToListAsync();

        return Ok(purchaseRequests);
    }

    // GET BY ID
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

    // UPDATE
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

        if (purchaseRequest.Status != "Pending")
        {
            return BadRequest(new
            {
                message = "Only pending purchase requests can be updated"
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

        purchaseRequest.MaterialName = request.MaterialName.Trim();
        purchaseRequest.Quantity = request.Quantity;
        purchaseRequest.RequiredByDate = request.RequiredByDate;

        await _context.SaveChangesAsync();

        return Ok(purchaseRequest);
    }

    // CANCEL
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

    // APPROVE
    [HttpPut("{id}/approve")]
    [Authorize(Roles = "ProjectManager,Administrator")]
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

        return Ok(new
        {
            message = "Purchase request approved successfully",
            purchaseRequest
        });
    }
}