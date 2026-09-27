using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProcurementController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProcurementController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("compare")]
    public async Task<IActionResult> CompareQuotations(
        ProcurementComparisonRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.MaterialName))
        {
            return BadRequest(new
            {
                error = "MaterialName is required."
            });
        }

        if (request.RequiredQuantity <= 0)
        {
            return BadRequest(new
            {
                error = "RequiredQuantity must be greater than zero."
            });
        }

        var materialName = request.MaterialName.Trim().ToLower();

        var quotations = await _context.SupplierQuotations
            .Include(q => q.Supplier)
            .Where(q =>
                q.MaterialName.ToLower() == materialName &&
                q.Quantity >= request.RequiredQuantity &&
                q.Supplier.IsActive)
            .OrderBy(q => q.TotalPrice)
            .ThenBy(q => q.DeliveryDate)
            .ThenBy(q => q.Id)
            .Select(q => new
            {
                quotationId = q.Id,
                supplierId = q.SupplierId,
                supplierName = q.Supplier.Name,
                materialName = q.MaterialName,
                quantity = q.Quantity,
                unitPrice = q.UnitPrice,
                deliveryDate = q.DeliveryDate,
                totalPrice = q.TotalPrice
            })
            .ToListAsync();

        if (quotations.Count == 0)
        {
            return Ok(new
            {
                materialName = request.MaterialName.Trim(),
                requiredQuantity = request.RequiredQuantity,
                quotationCount = 0,
                status = "NoQuotationAvailable",
                recommendedQuotation = (object?)null,
                alternatives = Array.Empty<object>(),
                approvalRequired = true,
                sideEffects = Array.Empty<object>()
            });
        }

        var recommended = quotations.First();

        var alternatives = quotations
            .Skip(1)
            .ToList();

        return Ok(new
        {
            materialName = request.MaterialName.Trim(),
            requiredQuantity = request.RequiredQuantity,
            quotationCount = quotations.Count,
            status = "QuotationAvailable",
            recommendedQuotation = recommended,
            alternatives = alternatives,
            approvalRequired = true,
            sideEffects = Array.Empty<object>()
        });
    }
}
