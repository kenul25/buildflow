using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Models;
using BuildFlow.Api.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BuildFlow.Api.Services;

public interface IInventoryService
{
    Task<IReadOnlyList<WarehouseDto>> ListWarehousesAsync(CancellationToken ct);
    Task<InventoryPageResult<WarehouseDto>> ListWarehousesAsync(InventoryPageQuery query, CancellationToken ct);
    Task<WarehouseDto> CreateWarehouseAsync(WarehouseWriteDto dto, CancellationToken ct);
    Task<WarehouseDto> UpdateWarehouseAsync(Guid id, WarehouseWriteDto dto, CancellationToken ct);
    Task DeleteWarehouseAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<MaterialDto>> ListMaterialsAsync(string? search, Guid? warehouseId, CancellationToken ct);
    Task<InventoryPageResult<MaterialDto>> ListMaterialsAsync(InventoryPageQuery query, CancellationToken ct);
    Task<MaterialDto> GetMaterialAsync(Guid id, CancellationToken ct);
    Task<MaterialDto> CreateMaterialAsync(MaterialWriteDto dto, CancellationToken ct);
    Task<MaterialDto> UpdateMaterialAsync(Guid id, MaterialWriteDto dto, CancellationToken ct);
    Task DeleteMaterialAsync(Guid id, CancellationToken ct);
    Task<MaterialDto> ReceiveAsync(Guid id, decimal quantity, Guid? actorId, string? reference, CancellationToken ct);
    Task<MaterialDto> IssueAsync(Guid id, decimal quantity, Guid? actorId, string? reference, CancellationToken ct);
    Task<MaterialDto> ReturnAsync(Guid id, decimal quantity, Guid? actorId, string? reference, CancellationToken ct);
    Task<IReadOnlyList<MaterialDto>> LowStockAsync(decimal threshold, CancellationToken ct);
    Task<IReadOnlyList<LowStockAlertDto>> GetLowStockAlertsAsync(decimal threshold, CancellationToken ct);
    Task<MaterialAvailabilityReport> CheckAvailabilityAsync(IEnumerable<MaterialRequirementDto> requirements, CancellationToken ct);
    Task<ReservationDto> ReserveAsync(Guid materialId, ReservationWriteDto dto, CancellationToken ct);
    Task ReleaseReservationAsync(Guid reservationId, CancellationToken ct);
    Task<InventoryPageResult<InventoryReservationDetailsDto>> ListReservationsAsync(Guid? materialId, int page, int pageSize, CancellationToken ct);
    Task<InventoryPageResult<StockMovementDto>> ListMovementsAsync(Guid? materialId, int page, int pageSize, CancellationToken ct);
}

public sealed class InventoryService(BuildFlowDbContext db, IInventoryRepository repository) : IInventoryService
{
    public async Task<IReadOnlyList<WarehouseDto>> ListWarehousesAsync(CancellationToken ct) =>
        await db.Warehouses.AsNoTracking().OrderBy(w => w.Name)
            .Select(w => new WarehouseDto(w.Id, w.Name, w.Location)).ToListAsync(ct);

    public async Task<InventoryPageResult<WarehouseDto>> ListWarehousesAsync(InventoryPageQuery query, CancellationToken ct)
    {
        ValidatePage(query.Page, query.PageSize);
        var rows = repository.Warehouses.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search)) rows = rows.Where(w => EF.Functions.ILike(w.Name, $"%{query.Search.Trim()}%"));
        var total = await rows.CountAsync(ct);
        rows = query.Sort?.ToLowerInvariant() switch
        {
            "updatedat" => query.Desc ? rows.OrderByDescending(w => w.UpdatedAt) : rows.OrderBy(w => w.UpdatedAt),
            _ => query.Desc ? rows.OrderByDescending(w => w.Name) : rows.OrderBy(w => w.Name)
        };
        var items = await rows.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(w => new WarehouseDto(w.Id, w.Name, w.Location)).ToListAsync(ct);
        return new(items, total, query.Page, query.PageSize);
    }

    public async Task<WarehouseDto> CreateWarehouseAsync(WarehouseWriteDto dto, CancellationToken ct)
    {
        var name = Required(dto.Name, "Warehouse name");
        if (await db.Warehouses.AnyAsync(w => w.Name == name, ct))
            throw new ApiException(409, "duplicate_warehouse", "A warehouse with this name already exists.");
        var warehouse = new Warehouse { Id = Guid.NewGuid(), Name = name, Location = dto.Location?.Trim() };
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(ct);
        return new(warehouse.Id, warehouse.Name, warehouse.Location);
    }

    public async Task<WarehouseDto> UpdateWarehouseAsync(Guid id, WarehouseWriteDto dto, CancellationToken ct)
    {
        var warehouse = await db.Warehouses.SingleOrDefaultAsync(w => w.Id == id, ct)
            ?? throw MissingWarehouse();
        var name = Required(dto.Name, "Warehouse name");
        if (await db.Warehouses.AnyAsync(w => w.Id != id && w.Name == name, ct))
            throw new ApiException(409, "duplicate_warehouse", "A warehouse with this name already exists.");
        warehouse.Name = name;
        warehouse.Location = dto.Location?.Trim();
        await db.SaveChangesAsync(ct);
        return new(warehouse.Id, warehouse.Name, warehouse.Location);
    }

    public async Task DeleteWarehouseAsync(Guid id, CancellationToken ct)
    {
        var warehouse = await db.Warehouses.SingleOrDefaultAsync(w => w.Id == id, ct)
            ?? throw MissingWarehouse();
        if (await db.Materials.AnyAsync(m => m.WarehouseId == id, ct))
            throw new ApiException(409, "warehouse_not_empty", "Remove or move all materials before deleting the warehouse.");
        db.Warehouses.Remove(warehouse);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<MaterialDto>> ListMaterialsAsync(string? search, Guid? warehouseId, CancellationToken ct)
    {
        await RefreshExpiryAsync(ct);
        var query = db.Materials.AsNoTracking().Include(m => m.Warehouse).Where(m => !m.IsArchived);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(m => EF.Functions.ILike(m.Name, $"%{search.Trim()}%"));
        if (warehouseId is Guid id) query = query.Where(m => m.WarehouseId == id);
        return await query.OrderBy(m => m.Name).ThenBy(m => m.Unit).Select(Map).ToListAsync(ct);
    }

    public async Task<InventoryPageResult<MaterialDto>> ListMaterialsAsync(InventoryPageQuery query, CancellationToken ct)
    {
        ValidatePage(query.Page, query.PageSize);
        await RefreshExpiryAsync(ct);
        IQueryable<Material> rows = repository.Materials.AsNoTracking().Include(m => m.Warehouse).Where(m => !m.IsArchived);
        if (!string.IsNullOrWhiteSpace(query.Search)) rows = rows.Where(m => EF.Functions.ILike(m.Name, $"%{query.Search.Trim()}%") || EF.Functions.ILike(m.Category, $"%{query.Search.Trim()}%"));
        if (query.WarehouseId is Guid warehouseId) rows = rows.Where(m => m.WarehouseId == warehouseId);
        var total = await rows.CountAsync(ct);
        rows = query.Sort?.ToLowerInvariant() switch
        {
            "stock" => query.Desc ? rows.OrderByDescending(m => m.CurrentStock - m.ReservedStock) : rows.OrderBy(m => m.CurrentStock - m.ReservedStock),
            "updatedat" => query.Desc ? rows.OrderByDescending(m => m.UpdatedAt) : rows.OrderBy(m => m.UpdatedAt),
            _ => query.Desc ? rows.OrderByDescending(m => m.Name) : rows.OrderBy(m => m.Name)
        };
        var items = await rows.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).Select(Map).ToListAsync(ct);
        return new(items, total, query.Page, query.PageSize);
    }

    public async Task<MaterialDto> GetMaterialAsync(Guid id, CancellationToken ct)
    {
        await RefreshExpiryAsync(ct);
        return await db.Materials.AsNoTracking().Include(m => m.Warehouse).Where(m => m.Id == id && !m.IsArchived).Select(Map).SingleOrDefaultAsync(ct) ?? throw MissingMaterial();
    }

    public async Task<MaterialDto> CreateMaterialAsync(MaterialWriteDto dto, CancellationToken ct)
    {
        await EnsureWarehouseAsync(dto.WarehouseId, ct);
        ValidateWrite(dto);
        if (dto.ReservedStock != 0) throw Invalid("Reserved stock is managed through reservations.");
        var name = Required(dto.Name, "Material name");
        var unit = Required(dto.Unit, "Material unit");
        if (await db.Materials.AnyAsync(m => m.Name == name && m.Unit == unit && m.WarehouseId == dto.WarehouseId, ct))
            throw new ApiException(409, "duplicate_material", "This material already exists in the warehouse.");
        var material = new Material { Id = Guid.NewGuid() };
        Apply(material, dto);
        db.Materials.Add(material);
        if (material.CurrentStock > 0) db.StockMovements.Add(new StockMovement { Id = Guid.NewGuid(), MaterialId = material.Id, Type = "Opening", Quantity = material.CurrentStock, StockAfter = material.CurrentStock, Reference = "Opening stock" });
        await db.SaveChangesAsync(ct);
        return await GetMaterialAsync(material.Id, ct);
    }

    public async Task<MaterialDto> UpdateMaterialAsync(Guid id, MaterialWriteDto dto, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await InventoryLockAsync(ct);
        await EnsureWarehouseAsync(dto.WarehouseId, ct);
        ValidateWrite(dto);
        var material = await LockMaterialAsync(id, ct);
        await ExpireReservationsAsync(id, ct);
        if (dto.CurrentStock != material.CurrentStock || dto.ReservedStock != material.ReservedStock) throw new ApiException(409, "stock_managed", "Stock changed or is being edited directly. Refresh and use receive, issue, return or an audited correction.");
        if ((material.Unit != dto.Unit.Trim() || material.WarehouseId != dto.WarehouseId) && await db.StockMovements.AnyAsync(x => x.MaterialId == id, ct)) throw Invalid("Unit and warehouse cannot change after stock history exists.");
        if (await db.Materials.AnyAsync(m => m.Id != id && m.Name == dto.Name.Trim() && m.Unit == dto.Unit.Trim() && m.WarehouseId == dto.WarehouseId, ct))
            throw new ApiException(409, "duplicate_material", "This material already exists in the warehouse.");
        Apply(material, dto);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return await GetMaterialAsync(id, ct);
    }

    public async Task DeleteMaterialAsync(Guid id, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await InventoryLockAsync(ct);
        var material = await LockMaterialAsync(id, ct);
        await ExpireReservationsAsync(id, ct);
        if (material.CurrentStock != 0 || material.ReservedStock != 0) throw new ApiException(409, "material_has_stock", "Issue or correct remaining stock before archiving the material.");
        material.IsArchived = true;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public Task<MaterialDto> ReceiveAsync(Guid id, decimal quantity, Guid? actorId, string? reference, CancellationToken ct) => AdjustStockAsync(id, quantity, "Receive", actorId, reference, ct);
    public Task<MaterialDto> ReturnAsync(Guid id, decimal quantity, Guid? actorId, string? reference, CancellationToken ct) => AdjustStockAsync(id, quantity, "Return", actorId, reference, ct);
    public Task<MaterialDto> IssueAsync(Guid id, decimal quantity, Guid? actorId, string? reference, CancellationToken ct) => AdjustStockAsync(id, quantity, "Issue", actorId, reference, ct);

    private async Task<MaterialDto> AdjustStockAsync(Guid id, decimal quantity, string movementType, Guid? actorId, string? reference, CancellationToken ct)
    {
        EnsurePositive(quantity);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var material = await LockMaterialAsync(id, ct);
        await ExpireReservationsAsync(id, ct);
        if (movementType == "Issue" && material.AvailableStock < quantity)
            throw new ApiException(409, "insufficient_stock", $"Only {material.AvailableStock:0.###} {material.Unit} is available.");
        material.CurrentStock += movementType == "Issue" ? -quantity : quantity;
        material.ValidateStock();
        repository.Add(new StockMovement
        {
            Id = Guid.NewGuid(), MaterialId = material.Id, Type = movementType,
            Quantity = quantity, StockAfter = material.CurrentStock, ActorId = actorId, Reference = reference?.Trim()
        });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return await GetMaterialAsync(id, ct);
    }

    public async Task<IReadOnlyList<MaterialDto>> LowStockAsync(decimal threshold, CancellationToken ct)
    {
        await RefreshExpiryAsync(ct);
        if (threshold < 0) throw Invalid("Threshold cannot be negative.");
        return await db.Materials.AsNoTracking().Include(m => m.Warehouse).Where(m => !m.IsArchived && m.CurrentStock - m.ReservedStock <= threshold).OrderBy(m => m.CurrentStock - m.ReservedStock).Select(Map).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<LowStockAlertDto>> GetLowStockAlertsAsync(decimal threshold, CancellationToken ct)
    {
        await RefreshExpiryAsync(ct);
        if (threshold < 0) throw Invalid("Threshold cannot be negative.");
        return await db.Materials.AsNoTracking()
            .Where(m => !m.IsArchived && m.CurrentStock - m.ReservedStock <= threshold)
            .OrderBy(m => m.CurrentStock - m.ReservedStock)
            .Select(m => new LowStockAlertDto(
                m.Id, m.Name, m.Unit, m.WarehouseId, m.CurrentStock - m.ReservedStock, threshold,
                m.CurrentStock - m.ReservedStock == 0 ? "Critical" : "Warning",
                m.CurrentStock - m.ReservedStock == 0
                    ? $"{m.Name} is out of available stock."
                    : $"{m.Name} has only {m.CurrentStock - m.ReservedStock:0.###} {m.Unit} available."
            )).ToListAsync(ct);
    }

    public async Task<MaterialAvailabilityReport> CheckAvailabilityAsync(IEnumerable<MaterialRequirementDto> requirements, CancellationToken ct)
    {
        await RefreshExpiryAsync(ct);
        var requested = requirements.GroupBy(r => new { Name = r.Name.Trim().ToUpperInvariant(), Unit = r.Unit.Trim().ToUpperInvariant() })
            .Select(group => new { group.Key.Name, group.Key.Unit, Required = group.Sum(r => r.RequiredQuantity) }).ToList();
        var names = requested.Select(r => r.Name).ToArray();
        var materials = await db.Materials.AsNoTracking().Where(m => !m.IsArchived && names.Contains(m.Name.ToUpper())).ToListAsync(ct);
        var items = requested.Select(required =>
        {
            var matches = materials.Where(m => string.Equals(m.Name, required.Name, StringComparison.OrdinalIgnoreCase) && string.Equals(m.Unit, required.Unit, StringComparison.OrdinalIgnoreCase));
            var current = matches.Sum(m => m.CurrentStock);
            var reserved = matches.Sum(m => m.ReservedStock);
            var available = current - reserved;
            var shortage = Math.Max(0, required.Required - available);
            return new MaterialAvailabilityDto(required.Name, required.Unit, required.Required, current, reserved, available, shortage, shortage == 0);
        }).ToList();
        return new(items, items.All(item => item.IsAvailable), DateTimeOffset.UtcNow);
    }

    public async Task<ReservationDto> ReserveAsync(Guid materialId, ReservationWriteDto dto, CancellationToken ct)
    {
        if (dto.ProjectId == null || !await db.Projects.AnyAsync(p => p.Id == dto.ProjectId && !p.IsArchived, ct)) throw Invalid("An active project is required for a reservation.");
        EnsurePositive(dto.Quantity);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var material = await LockMaterialAsync(materialId, ct);
        await ExpireReservationsAsync(material.Id, ct);
        if (material.AvailableStock < dto.Quantity)
            throw new ApiException(409, "insufficient_stock", $"Only {material.AvailableStock:0.###} {material.Unit} is available.");
        material.ReservedStock += dto.Quantity;
        var reservation = new InventoryReservation
        {
            Id = Guid.NewGuid(), MaterialId = material.Id, Quantity = dto.Quantity, ProjectId = dto.ProjectId,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(dto.DurationMinutes)
        };
        db.InventoryReservations.Add(reservation);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new(reservation.Id, reservation.MaterialId, reservation.Quantity, reservation.ProjectId, reservation.Status, reservation.ExpiresAt);
    }

    public async Task ReleaseReservationAsync(Guid reservationId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await InventoryLockAsync(ct);
        var reservation = await db.InventoryReservations.SingleOrDefaultAsync(r => r.Id == reservationId, ct)
            ?? throw new ApiException(404, "reservation_not_found", "The reservation was not found.");
        if (reservation.Status != "Active") return;
        var material = await LockMaterialAsync(reservation.MaterialId, ct);
        material.ReservedStock -= reservation.Quantity;
        reservation.Status = "Released";
        reservation.ReleasedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<InventoryPageResult<InventoryReservationDetailsDto>> ListReservationsAsync(Guid? materialId, int page, int pageSize, CancellationToken ct)
    {
        await RefreshExpiryAsync(ct);
        ValidatePage(page, pageSize);
        IQueryable<InventoryReservation> rows = repository.Reservations.AsNoTracking().Include(r => r.Material);
        if (materialId is Guid id) rows = rows.Where(r => r.MaterialId == id);
        var total = await rows.CountAsync(ct);
        var items = await rows.OrderByDescending(r => r.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new InventoryReservationDetailsDto(r.Id, r.MaterialId, r.Material.Name, r.Quantity, r.ProjectId, r.Status, r.ExpiresAt, r.ReleasedAt, r.CreatedAt)).ToListAsync(ct);
        return new(items, total, page, pageSize);
    }

    public async Task<InventoryPageResult<StockMovementDto>> ListMovementsAsync(Guid? materialId, int page, int pageSize, CancellationToken ct)
    {
        ValidatePage(page, pageSize);
        IQueryable<StockMovement> rows = repository.Movements.AsNoTracking().Include(m => m.Material);
        if (materialId is Guid id) rows = rows.Where(m => m.MaterialId == id);
        var total = await rows.CountAsync(ct);
        var items = await rows.OrderByDescending(m => m.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(m => new StockMovementDto(m.Id, m.MaterialId, m.Material.Name, m.Type, m.Quantity, m.StockAfter, m.ActorId, m.Reference, m.CreatedAt, m.ReversedAt, m.ReversalOfId)).ToListAsync(ct);
        return new(items, total, page, pageSize);
    }

    private async Task<Material> LockMaterialAsync(Guid id, CancellationToken ct)
    {
        await InventoryLockAsync(ct);
        var material = await db.Materials.FromSqlInterpolated($"SELECT * FROM \"Materials\" WHERE \"Id\" = {id} FOR UPDATE").SingleOrDefaultAsync(ct);
        return material is { IsArchived: false } ? material : throw MissingMaterial();
    }

    public Task InventoryLockAsync(CancellationToken ct) => db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(42002001)", ct);
    public async Task RefreshExpiryAsync(CancellationToken ct)
    {
        if (db.Database.CurrentTransaction != null) return;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await InventoryLockAsync(ct);
        var ids = await db.InventoryReservations.Where(x => x.Status == "Active" && x.ExpiresAt <= DateTimeOffset.UtcNow).Select(x => x.MaterialId).Distinct().OrderBy(x => x).ToListAsync(ct);
        foreach (var id in ids) await ExpireReservationsAsync(id, ct);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }

    public async Task<ReservationDto> EditReservationAsync(Guid id, ReservationWriteDto dto, bool consume, CancellationToken ct, Guid? actor = null)
    {
        EnsurePositive(dto.Quantity);
        await using var tx = await db.Database.BeginTransactionAsync(ct); await InventoryLockAsync(ct);
        var row = await db.InventoryReservations.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ApiException(404, "not_found", "Reservation not found.");
        var material = await LockMaterialAsync(row.MaterialId, ct); await ExpireReservationsAsync(row.MaterialId, ct);
        if (row.Status != "Active") throw new ApiException(409, "inactive_reservation", "Only active reservations can be changed.");
        if (consume)
        {
            material.ReservedStock -= row.Quantity; material.CurrentStock -= row.Quantity; row.Status = "Consumed"; row.ReleasedAt = DateTimeOffset.UtcNow;
            db.StockMovements.Add(new StockMovement { Id = Guid.NewGuid(), MaterialId = material.Id, Type = "Issue", Quantity = row.Quantity, StockAfter = material.CurrentStock, ActorId = actor, Reference = $"Reservation {id}" });
        }
        else
        {
            if (dto.ProjectId == null || !await db.Projects.AnyAsync(p => p.Id == dto.ProjectId && !p.IsArchived, ct)) throw Invalid("An active project is required.");
            if (dto.Quantity > material.AvailableStock + row.Quantity) throw new ApiException(409, "insufficient_stock", "Reservation exceeds available stock.");
            material.ReservedStock += dto.Quantity - row.Quantity; row.Quantity = dto.Quantity; row.ProjectId = dto.ProjectId; row.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(dto.DurationMinutes);
        }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return new(row.Id, row.MaterialId, row.Quantity, row.ProjectId, row.Status, row.ExpiresAt);
    }

    public async Task ReverseMovementAsync(Guid id, Guid actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); await InventoryLockAsync(ct);
        var row = await db.StockMovements.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ApiException(404, "not_found", "Movement not found.");
        if (row.ReversedAt != null || row.ReversalOfId != null || row.Reference != null && (row.Reference.StartsWith("Delivery ") || row.Reference.StartsWith("Reservation "))) throw new ApiException(409, "cannot_reverse", "This movement is already reversed or belongs to a managed receipt/reservation.");
        var material = await LockMaterialAsync(row.MaterialId, ct); await ExpireReservationsAsync(row.MaterialId, ct);
        var delta = row.Type == "Issue" ? row.Quantity : -row.Quantity;
        if (material.CurrentStock + delta < material.ReservedStock) throw new ApiException(409, "insufficient_stock", "Reversal would consume reserved or unavailable stock.");
        material.CurrentStock += delta; row.ReversedAt = DateTimeOffset.UtcNow;
        db.StockMovements.Add(new StockMovement { Id = Guid.NewGuid(), MaterialId = material.Id, Type = delta > 0 ? "Return" : "Issue", Quantity = row.Quantity, StockAfter = material.CurrentStock, ActorId = actor, ReversalOfId = id, Reference = $"Correction of {id}" });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }

    private async Task ExpireReservationsAsync(Guid materialId, CancellationToken ct)
    {
        var expired = await db.InventoryReservations.Where(r => r.MaterialId == materialId && r.Status == "Active" && r.ExpiresAt <= DateTimeOffset.UtcNow).ToListAsync(ct);
        foreach (var reservation in expired)
        {
            reservation.Status = "Expired";
            reservation.ReleasedAt = DateTimeOffset.UtcNow;
            var material = await db.Materials.SingleAsync(m => m.Id == materialId, ct);
            material.ReservedStock -= reservation.Quantity;
        }
    }

    private async Task EnsureWarehouseAsync(Guid id, CancellationToken ct)
    {
        if (!await db.Warehouses.AnyAsync(w => w.Id == id, ct)) throw new ApiException(400, "invalid_warehouse", "The warehouse was not found.");
    }

    private static void Apply(Material material, MaterialWriteDto dto)
    {
        material.Name = Required(dto.Name, "Material name"); material.Category = Required(dto.Category, "Material category"); material.Unit = Required(dto.Unit, "Material unit");
        material.UnitPrice = dto.UnitPrice; material.CurrentStock = dto.CurrentStock; material.ReservedStock = dto.ReservedStock; material.WarehouseId = dto.WarehouseId;
        material.ValidateStock();
    }

    private static void ValidateWrite(MaterialWriteDto dto)
    {
        if (dto.ReservedStock > dto.CurrentStock) throw Invalid("Reserved stock cannot exceed current stock.");
        Required(dto.Category, "Material category");
    }

    private static string Required(string? value, string field) => !string.IsNullOrWhiteSpace(value) ? value.Trim() : throw Invalid($"{field} is required.");
    private static void ValidatePage(int page, int pageSize) { if (page < 1 || pageSize is < 1 or > 100) throw Invalid("Page must be positive and pageSize must be between 1 and 100."); }
    private static void EnsurePositive(decimal quantity) { if (quantity <= 0) throw Invalid("Quantity must be greater than zero."); }
    private static ApiException Invalid(string message) => new(400, "invalid_inventory_data", message);
    private static ApiException MissingMaterial() => new(404, "material_not_found", "The material was not found.");
    private static ApiException MissingWarehouse() => new(404, "warehouse_not_found", "The warehouse was not found.");
    private static readonly Expression<Func<Material, MaterialDto>> Map = material => new(material.Id, material.Name, material.Category, material.Unit, material.UnitPrice, material.CurrentStock, material.ReservedStock, material.CurrentStock - material.ReservedStock, material.WarehouseId, material.Warehouse.Name);
}
