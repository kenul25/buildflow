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
    public async Task<IActionResult> GetSuppliers()
    {
        var suppliers = await _context.Suppliers
            .Where(s => s.IsActive)
            .ToListAsync();

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
