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
public class QuotationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public QuotationsController(AppDbContext context)
    {
        _context = context;
    }

    // CREATE
    [HttpPost]
    public async Task<IActionResult> CreateQuotation(
        CreateQuotationRequest request)
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

        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s =>
                s.Id == request.SupplierId &&
                s.IsActive);

        if (supplier == null)
        {
            return NotFound(new
            {
                message = "Active supplier not found"
            });
        }

        // Server-side calculation
        var totalPrice = request.Quantity * request.UnitPrice;

        var quotation = new SupplierQuotation
        {
            SupplierId = request.SupplierId,
            MaterialName = request.MaterialName.Trim(),
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            DeliveryDate = request.DeliveryDate,
            TotalPrice = totalPrice
        };

        _context.SupplierQuotations.Add(quotation);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetQuotation),
            new { id = quotation.Id },
            quotation);
    }

    // GET ALL
    [HttpGet]
    public async Task<IActionResult> GetQuotations(
        string? search = null,
        int? supplierId = null,
        string sort = "deliveryDate",
        bool desc = false)
    {
        var query = _context.SupplierQuotations
            .Include(q => q.Supplier)
            .AsQueryable();

        // Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLower();

            query = query.Where(q =>
                q.MaterialName.ToLower().Contains(search) ||
                q.Supplier.Name.ToLower().Contains(search));
        }

        // Supplier filter
        if (supplierId.HasValue)
        {
            query = query.Where(q =>
                q.SupplierId == supplierId.Value);
        }

        // Sorting
        query = sort.ToLower() switch
        {
            "material" =>
                desc
                    ? query.OrderByDescending(q => q.MaterialName)
                    : query.OrderBy(q => q.MaterialName),

            "quantity" =>
                desc
                    ? query.OrderByDescending(q => q.Quantity)
                    : query.OrderBy(q => q.Quantity),

            "unitprice" =>
                desc
                    ? query.OrderByDescending(q => q.UnitPrice)
                    : query.OrderBy(q => q.UnitPrice),

            "totalprice" =>
                desc
                    ? query.OrderByDescending(q => q.TotalPrice)
                    : query.OrderBy(q => q.TotalPrice),

            "supplier" =>
                desc
                    ? query.OrderByDescending(q => q.Supplier.Name)
                    : query.OrderBy(q => q.Supplier.Name),

            _ =>
                desc
                    ? query.OrderByDescending(q => q.DeliveryDate)
                    : query.OrderBy(q => q.DeliveryDate)
        };

        var quotations = await query
            .Select(q => new
            {
                q.Id,
                q.SupplierId,
                SupplierName = q.Supplier.Name,
                q.MaterialName,
                q.Quantity,
                q.UnitPrice,
                q.DeliveryDate,
                q.TotalPrice
            })
            .ToListAsync();

        return Ok(quotations);
    }

    // GET BY ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetQuotation(int id)
    {
        var quotation = await _context.SupplierQuotations
            .Include(q => q.Supplier)
            .Where(q => q.Id == id)
            .Select(q => new
            {
                q.Id,
                q.SupplierId,
                SupplierName = q.Supplier.Name,
                q.MaterialName,
                q.Quantity,
                q.UnitPrice,
                q.DeliveryDate,
                q.TotalPrice
            })
            .FirstOrDefaultAsync();

        if (quotation == null)
        {
            return NotFound(new
            {
                message = "Quotation not found"
            });
        }

        return Ok(quotation);
    }

    // UPDATE
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

        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s =>
                s.Id == request.SupplierId &&
                s.IsActive);

        if (supplier == null)
        {
            return NotFound(new
            {
                message = "Active supplier not found"
            });
        }

        quotation.SupplierId = request.SupplierId;
        quotation.MaterialName = request.MaterialName.Trim();
        quotation.Quantity = request.Quantity;
        quotation.UnitPrice = request.UnitPrice;
        quotation.DeliveryDate = request.DeliveryDate;

        // Always calculate on server
        quotation.TotalPrice =
            request.Quantity * request.UnitPrice;

        await _context.SaveChangesAsync();

        return Ok(quotation);
    }

    // SAFE DELETE
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteQuotation(int id)
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

        // SupplierQuotation model currently has no IsActive field.
        // Therefore we cannot safely deactivate it without changing
        // the existing model/database structure.

        _context.SupplierQuotations.Remove(quotation);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Quotation deleted successfully"
        });
    }
}