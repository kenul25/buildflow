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
public class SupplierMaterialsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly BuildFlowDbContext _buildFlowContext;

    public SupplierMaterialsController(
        AppDbContext context,
        BuildFlowDbContext buildFlowContext)
    {
        _context = context;
        _buildFlowContext = buildFlowContext;
    }

    [HttpPost]
    public async Task<IActionResult> CreateSupplierMaterial(
        CreateSupplierMaterialRequest request)
    {
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

        var material = await _buildFlowContext.Materials
            .FirstOrDefaultAsync(m => m.Id == request.MaterialId);

        if (material == null)
        {
            return NotFound(new
            {
                message = "Material not found"
            });
        }

        var exists = await _context.SupplierMaterials
            .AnyAsync(sm =>
                sm.SupplierId == request.SupplierId &&
                sm.MaterialId == request.MaterialId);

        if (exists)
        {
            return BadRequest(new
            {
                message = "This supplier is already linked to this material"
            });
        }

        var supplierMaterial = new SupplierMaterial
        {
            SupplierId = request.SupplierId,
            MaterialId = request.MaterialId,
            AvailableQuantity = request.AvailableQuantity,
            UnitPrice = request.UnitPrice,
            LeadTimeDays = request.LeadTimeDays,
            IsActive = true
        };

        _context.SupplierMaterials.Add(supplierMaterial);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetSupplierMaterial),
            new { id = supplierMaterial.Id },
            supplierMaterial);
    }

    [HttpGet]
    public async Task<IActionResult> GetSupplierMaterials()
    {
        var supplierMaterials = await _context.SupplierMaterials
            .Include(sm => sm.Supplier)
            .Where(sm => sm.IsActive)
            .OrderBy(sm => sm.SupplierId)
            .ThenBy(sm => sm.MaterialId)
            .Select(sm => new
            {
                sm.Id,
                sm.SupplierId,
                SupplierName = sm.Supplier.Name,
                sm.MaterialId,
                sm.AvailableQuantity,
                sm.UnitPrice,
                sm.LeadTimeDays,
                sm.IsActive
            })
            .ToListAsync();

        return Ok(supplierMaterials);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSupplierMaterial(int id)
    {
        var supplierMaterial = await _context.SupplierMaterials
            .Include(sm => sm.Supplier)
            .FirstOrDefaultAsync(sm => sm.Id == id);

        if (supplierMaterial == null)
        {
            return NotFound(new
            {
                message = "Supplier material not found"
            });
        }

        return Ok(new
        {
            supplierMaterial.Id,
            supplierMaterial.SupplierId,
            SupplierName = supplierMaterial.Supplier.Name,
            supplierMaterial.MaterialId,
            supplierMaterial.AvailableQuantity,
            supplierMaterial.UnitPrice,
            supplierMaterial.LeadTimeDays,
            supplierMaterial.IsActive
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSupplierMaterial(
        int id,
        UpdateSupplierMaterialRequest request)
    {
        var supplierMaterial = await _context.SupplierMaterials
            .FirstOrDefaultAsync(sm => sm.Id == id);

        if (supplierMaterial == null)
        {
            return NotFound(new
            {
                message = "Supplier material not found"
            });
        }

        if (!supplierMaterial.IsActive)
        {
            return BadRequest(new
            {
                message = "Inactive supplier material cannot be updated"
            });
        }

        supplierMaterial.AvailableQuantity = request.AvailableQuantity;
        supplierMaterial.UnitPrice = request.UnitPrice;
        supplierMaterial.LeadTimeDays = request.LeadTimeDays;

        await _context.SaveChangesAsync();

        return Ok(supplierMaterial);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeactivateSupplierMaterial(int id)
    {
        var supplierMaterial = await _context.SupplierMaterials
            .FirstOrDefaultAsync(sm => sm.Id == id);

        if (supplierMaterial == null)
        {
            return NotFound(new
            {
                message = "Supplier material not found"
            });
        }

        if (!supplierMaterial.IsActive)
        {
            return BadRequest(new
            {
                message = "Supplier material is already inactive"
            });
        }

        supplierMaterial.IsActive = false;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Supplier material deactivated successfully"
        });
    }
}