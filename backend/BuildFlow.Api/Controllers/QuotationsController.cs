using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuotationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public QuotationsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreateQuotation(
        CreateQuotationRequest request)
    {
        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == request.SupplierId && s.IsActive);

        if (supplier == null)
        {
            return NotFound(new
            {
                message = "Supplier not found"
            });
        }

        var quotation = new SupplierQuotation
        {
            SupplierId = request.SupplierId,
            MaterialName = request.MaterialName,
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            DeliveryDate = request.DeliveryDate,
            TotalPrice = request.Quantity * request.UnitPrice
        };

        _context.SupplierQuotations.Add(quotation);
        await _context.SaveChangesAsync();

        return Ok(quotation);
    }

    [HttpGet]
    public async Task<IActionResult> GetQuotations()
    {
        var quotations = await _context.SupplierQuotations
            .ToListAsync();

        return Ok(quotations);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetQuotation(int id)
    {
        var quotation = await _context.SupplierQuotations
            .Include(q => q.Supplier)
            .FirstOrDefaultAsync(q => q.Id == id);
        
        if (quotation == null)
        {
            return NotFound(new
            {
                message = "Quotation not found"
            });
        }

        return Ok(quotation);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateQuotation(
        int id,
        CreateQuotationRequest request)
    {
        var quotation = await _context.SupplierQuotations
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quotation == null)
        {
            return NotFound(new
            {
                message = "Quotation not found"
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
                message = "Supplier is inactive"
            });
        }

        if (request.Quantity <= 0)
        {
            return BadRequest(new
            {
                message = "Quantity must be greater than zero"
            });
        }

        if (request.UnitPrice < 0)
        {
            return BadRequest(new
            {
                message = "Unit price cannot be negative"
            });
        }

        if (request.DeliveryDate <= DateTime.UtcNow)
        {
            return BadRequest(new
            {
                message = "Delivery date must be in the future"
            });
        }

        quotation.SupplierId = request.SupplierId;
        quotation.MaterialName = request.MaterialName;
        quotation.Quantity = request.Quantity;
        quotation.UnitPrice = request.UnitPrice;
        quotation.DeliveryDate = request.DeliveryDate;

        // Server-side calculation
        quotation.TotalPrice =
            request.Quantity * request.UnitPrice;

        await _context.SaveChangesAsync();

        return Ok(quotation);
    }
}