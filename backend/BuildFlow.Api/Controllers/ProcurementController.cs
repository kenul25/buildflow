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
        var quotations = await _context.SupplierQuotations
            .Include(q => q.Supplier)
            .Where(q =>
                q.MaterialName.ToLower() == request.MaterialName.ToLower() &&
                q.Quantity >= request.RequiredQuantity &&
                q.Supplier.IsActive)
            .OrderBy(q => q.TotalPrice)
            .ToListAsync();

        return Ok(quotations);
    }
}