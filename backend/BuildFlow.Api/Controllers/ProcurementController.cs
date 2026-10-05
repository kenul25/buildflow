using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace BuildFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ProcurementOfficer,ProjectManager,Administrator")]
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
                q.Supplier.IsActive && q.IsActive && q.ValidUntil != null && q.ValidUntil >= DateTime.UtcNow && q.DeliveryDate >= DateTime.UtcNow &&
                _context.Set<BuildFlow.Api.Models.Material>().Any(m => m.Id == q.MaterialId && !m.IsArchived && m.Name == q.MaterialName && m.Unit == q.Unit) &&
                _context.SupplierMaterials.Any(s => s.MaterialId == q.MaterialId && s.SupplierId == q.SupplierId && s.IsActive && s.AvailableQuantity - _context.PurchaseOrders.Where(o => o.SupplierId == s.SupplierId && o.MaterialId == s.MaterialId && o.Status != "Cancelled" && o.Status != "Completed").Select(o => o.Quantity - _context.Deliveries.Where(d => d.PurchaseOrderId == o.Id && (d.Status == "Received" || d.Status == "Completed")).Sum(d => d.Quantity)).Sum() >= request.RequiredQuantity) &&
                (request.RequiredByDate == null || q.DeliveryDate.Date <= request.RequiredByDate.Value.Date) &&
                (request.BudgetLimit == null || q.UnitPrice * request.RequiredQuantity <= request.BudgetLimit))
            .OrderBy(q => q.UnitPrice * request.RequiredQuantity)
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
                totalPrice = q.UnitPrice * request.RequiredQuantity
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
