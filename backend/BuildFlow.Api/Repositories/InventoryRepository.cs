using BuildFlow.Api.Data;
using BuildFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Repositories;

public interface IInventoryRepository
{
    IQueryable<Warehouse> Warehouses { get; }
    IQueryable<Material> Materials { get; }
    IQueryable<InventoryReservation> Reservations { get; }
    IQueryable<StockMovement> Movements { get; }
    Task<Warehouse?> FindWarehouseAsync(Guid id, CancellationToken ct);
    Task<Material?> FindMaterialAsync(Guid id, CancellationToken ct);
    Task<InventoryReservation?> FindReservationAsync(Guid id, CancellationToken ct);
    void Add(Warehouse warehouse);
    void Add(Material material);
    void Add(InventoryReservation reservation);
    void Add(StockMovement movement);
    void Remove(Warehouse warehouse);
    void Remove(Material material);
    Task SaveAsync(CancellationToken ct);
}

public sealed class InventoryRepository(BuildFlowDbContext db) : IInventoryRepository
{
    public IQueryable<Warehouse> Warehouses => db.Warehouses;
    public IQueryable<Material> Materials => db.Materials;
    public IQueryable<InventoryReservation> Reservations => db.InventoryReservations;
    public IQueryable<StockMovement> Movements => db.StockMovements;
    public Task<Warehouse?> FindWarehouseAsync(Guid id, CancellationToken ct) => db.Warehouses.SingleOrDefaultAsync(w => w.Id == id, ct);
    public Task<Material?> FindMaterialAsync(Guid id, CancellationToken ct) => db.Materials.SingleOrDefaultAsync(m => m.Id == id, ct);
    public Task<InventoryReservation?> FindReservationAsync(Guid id, CancellationToken ct) => db.InventoryReservations.SingleOrDefaultAsync(r => r.Id == id, ct);
    public void Add(Warehouse warehouse) => db.Warehouses.Add(warehouse);
    public void Add(Material material) => db.Materials.Add(material);
    public void Add(InventoryReservation reservation) => db.InventoryReservations.Add(reservation);
    public void Add(StockMovement movement) => db.StockMovements.Add(movement);
    public void Remove(Warehouse warehouse) => db.Warehouses.Remove(warehouse);
    public void Remove(Material material) => db.Materials.Remove(material);
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}