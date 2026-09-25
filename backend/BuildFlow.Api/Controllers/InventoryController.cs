using BuildFlow.Api.DTOs;
using BuildFlow.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildFlow.Api.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize]
public sealed class InventoryController(IInventoryService inventory, InventoryAnalysisAgent analysisAgent) : ControllerBase
{
    [HttpGet("warehouses")]
    public Task<IReadOnlyList<WarehouseDto>> Warehouses(CancellationToken ct) => inventory.ListWarehousesAsync(ct);

    [HttpGet("warehouses/page")]
    public Task<InventoryPageResult<WarehouseDto>> WarehousesPage([FromQuery] InventoryPageQuery query, CancellationToken ct) => inventory.ListWarehousesAsync(query, ct);

    [HttpPost("warehouses"), Authorize(Roles = "Administrator,ProjectManager,InventoryOfficer")]
    public async Task<ActionResult<WarehouseDto>> CreateWarehouse(WarehouseWriteDto dto, CancellationToken ct) =>
        Created("api/inventory/warehouses", await inventory.CreateWarehouseAsync(dto, ct));

    [HttpPut("warehouses/{id:guid}"), Authorize(Roles = "Administrator,ProjectManager,InventoryOfficer")]
    public Task<WarehouseDto> UpdateWarehouse(Guid id, WarehouseWriteDto dto, CancellationToken ct) => inventory.UpdateWarehouseAsync(id, dto, ct);

    [HttpDelete("warehouses/{id:guid}"), Authorize(Roles = "Administrator,ProjectManager,InventoryOfficer")]
    public async Task<IActionResult> DeleteWarehouse(Guid id, CancellationToken ct)
    {
        await inventory.DeleteWarehouseAsync(id, ct);
        return NoContent();
    }

    [HttpGet("materials")]
    public Task<IReadOnlyList<MaterialDto>> Materials([FromQuery] string? search, [FromQuery] Guid? warehouseId, CancellationToken ct) => inventory.ListMaterialsAsync(search, warehouseId, ct);

    [HttpGet("materials/page")]
    public Task<InventoryPageResult<MaterialDto>> MaterialsPage([FromQuery] InventoryPageQuery query, CancellationToken ct) => inventory.ListMaterialsAsync(query, ct);

    [HttpGet("materials/{id:guid}")]
    public Task<MaterialDto> Material(Guid id, CancellationToken ct) => inventory.GetMaterialAsync(id, ct);

    [HttpPost("materials"), Authorize(Roles = "Administrator,ProjectManager,InventoryOfficer")]
    public async Task<ActionResult<MaterialDto>> CreateMaterial(MaterialWriteDto dto, CancellationToken ct)
    {
        var material = await inventory.CreateMaterialAsync(dto, ct);
        return CreatedAtAction(nameof(Material), new { id = material.Id }, material);
    }

    [HttpPut("materials/{id:guid}"), Authorize(Roles = "Administrator,ProjectManager,InventoryOfficer")]
    public Task<MaterialDto> UpdateMaterial(Guid id, MaterialWriteDto dto, CancellationToken ct) => inventory.UpdateMaterialAsync(id, dto, ct);

    [HttpDelete("materials/{id:guid}"), Authorize(Roles = "Administrator,ProjectManager,InventoryOfficer")]
    public async Task<IActionResult> DeleteMaterial(Guid id, CancellationToken ct)
    {
        await inventory.DeleteMaterialAsync(id, ct);
        return NoContent();
    }

    [HttpGet("low-stock")]
    public Task<IReadOnlyList<MaterialDto>> LowStock([FromQuery] decimal threshold = 0, CancellationToken ct = default) => inventory.LowStockAsync(threshold, ct);

    [HttpGet("alerts/low-stock")]
    public Task<IReadOnlyList<LowStockAlertDto>> LowStockAlerts([FromQuery] decimal threshold = 0, CancellationToken ct = default) => inventory.GetLowStockAlertsAsync(threshold, ct);

    [HttpPost("materials/{id:guid}/receive"), Authorize(Roles = "Administrator,ProjectManager,SiteEngineer,InventoryOfficer")]
    public Task<MaterialDto> Receive(Guid id, StockAdjustmentDto dto, CancellationToken ct) => inventory.ReceiveAsync(id, dto.Quantity, ActorId, dto.Reference, ct);

    [HttpPost("materials/{id:guid}/issue"), Authorize(Roles = "Administrator,ProjectManager,SiteEngineer,InventoryOfficer")]
    public Task<MaterialDto> Issue(Guid id, StockAdjustmentDto dto, CancellationToken ct) => inventory.IssueAsync(id, dto.Quantity, ActorId, dto.Reference, ct);

    [HttpPost("materials/{id:guid}/return"), Authorize(Roles = "Administrator,ProjectManager,SiteEngineer,InventoryOfficer")]
    public Task<MaterialDto> Return(Guid id, StockAdjustmentDto dto, CancellationToken ct) => inventory.ReturnAsync(id, dto.Quantity, ActorId, dto.Reference, ct);

    [HttpPost("reservations/check")]
    public Task<MaterialAvailabilityReport> CheckAvailability(List<MaterialRequirementDto> requirements, CancellationToken ct) => inventory.CheckAvailabilityAsync(requirements, ct);

    [HttpPost("analysis")]
    public Task<MaterialAvailabilityReport> Analyze(List<MaterialRequirementDto> requirements, CancellationToken ct) => analysisAgent.AnalyzeAsync(requirements, ct);

    [HttpPost("materials/{id:guid}/reservations"), Authorize(Roles = "Administrator,ProjectManager,InventoryOfficer")]
    public Task<ReservationDto> Reserve(Guid id, ReservationWriteDto dto, CancellationToken ct) => inventory.ReserveAsync(id, dto, ct);

    [HttpDelete("reservations/{id:guid}"), Authorize(Roles = "Administrator,ProjectManager,InventoryOfficer")]
    public async Task<IActionResult> Release(Guid id, CancellationToken ct)
    {
        await inventory.ReleaseReservationAsync(id, ct);
        return NoContent();
    }

    [HttpGet("reservations")]
    public Task<InventoryPageResult<InventoryReservationDetailsDto>> Reservations([FromQuery] Guid? materialId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) => inventory.ListReservationsAsync(materialId, page, pageSize, ct);

    [HttpGet("movements")]
    public Task<InventoryPageResult<StockMovementDto>> Movements([FromQuery] Guid? materialId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) => inventory.ListMovementsAsync(materialId, page, pageSize, ct);

    private Guid? ActorId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) ? actorId : null;
}