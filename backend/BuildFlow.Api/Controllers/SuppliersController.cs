using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace BuildFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ProcurementOfficer,ProjectManager,Administrator")]
public class SuppliersController : ControllerBase
{
    private readonly AppDbContext _context;

    public SuppliersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreateSupplier(CreateSupplierRequest request)
    {
        var supplier = new Supplier
        {
            Name = request.Name,
            ContactPerson = request.ContactPerson,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        return Ok(supplier);
    }

    [HttpGet]
    public async Task<IActionResult> GetSuppliers(string? search = null, string sort = "name", bool desc = false, int page = 1, int pageSize = 100)
    {
        if (page < 1 || pageSize is < 1 or > 100) return BadRequest(new { message = "Invalid pagination." });
        var query = _context.Suppliers.AsNoTracking().Where(s => s.IsActive);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(s => EF.Functions.ILike(s.Name, $"%{search.Trim()}%"));
        Response.Headers["X-Total-Count"] = (await query.CountAsync()).ToString();
        var ordered = sort.ToLowerInvariant() == "updatedat" ? (desc ? query.OrderByDescending(x => x.UpdatedAt) : query.OrderBy(x => x.UpdatedAt)) : (desc ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name));
        var suppliers = await ordered.ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(suppliers);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSupplier(int id)
    {
        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == id);

        if (supplier == null)
        {
            return NotFound(new
            {
                message = "Supplier not found"
            });
        }

        return Ok(supplier);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSupplier(
        int id,
        UpdateSupplierRequest request)
    {
        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == id);

        if (supplier == null)
        {
            return NotFound(new
            {
                message = "Supplier not found"
            });
        }

        supplier.Name = request.Name;
        supplier.ContactPerson = request.ContactPerson;
        supplier.Email = request.Email;
        supplier.Phone = request.Phone;
        supplier.Address = request.Address;

        await _context.SaveChangesAsync();

        return Ok(supplier);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSupplier(int id)
    {
        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == id);

        if (supplier == null)
        {
            return NotFound(new
            {
                message = "Supplier not found"
            });
        }

        supplier.IsActive = false;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Supplier deleted successfully"
        });
    }
}
