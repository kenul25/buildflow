using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Services;

public sealed class ProcurementService(AppDbContext db, BuildFlowDbContext construction)
{
    public static DateTime Utc(DateTime value) => value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();
    private static ApiException Bad(string text) => new(400, "invalid_procurement", text);
    private static ApiException Conflict(string text) => new(409, "procurement_conflict", text);
    private static ApiException Missing() => new(404, "not_found", "Record not found.");
    public Task LockAsync(CancellationToken ct) => db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(42003001)", ct);
    public static async Task<decimal> RemainingSupplyAsync(SupplierMaterial supply, IQueryable<PurchaseOrder> orders, IQueryable<Delivery> deliveries, int? excludeOrder, CancellationToken ct)
    {
        var outstanding = await orders.Where(o => o.Id != excludeOrder && o.SupplierId == supply.SupplierId && o.MaterialId == supply.MaterialId && o.Status != "Cancelled" && o.Status != "Completed")
            .Select(o => o.Quantity - deliveries.Where(d => d.PurchaseOrderId == o.Id && (d.Status == "Received" || d.Status == "Completed")).Sum(d => d.Quantity)).SumAsync(ct);
        return Math.Max(0, supply.AvailableQuantity - outstanding);
    }

    public async Task<Guid[]> ProjectsAsync(Guid actor, bool office, CancellationToken ct) => await construction.Projects.Where(x => !x.IsArchived && (office || x.AssignedEngineerId == actor)).Select(x => x.Id).ToArrayAsync(ct);
    public async Task<bool> VisibleAsync(Guid? projectId, Guid actor, bool office, CancellationToken ct) => office || projectId != null && await construction.Projects.AnyAsync(x => x.Id == projectId && !x.IsArchived && x.AssignedEngineerId == actor, ct);

    public async Task<PurchaseRequest> WriteRequestAsync(int? id, CreatePurchaseRequestRequest dto, Guid actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); await LockAsync(ct);
        var row = id != null ? await db.PurchaseRequests.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw Missing() : new PurchaseRequest { CreatedById = actor };
        if (row.Status != "Pending") throw Conflict("Only pending requests can be edited.");
        if (dto.Quantity <= 0 || dto.Quantity > 999999999) throw Bad("Quantity must be positive and within the supported range.");
        if (dto.BudgetLimit < 0) throw Bad("Budget cannot be negative.");
        if (dto.ProjectId == null || !await construction.Projects.AnyAsync(x => x.Id == dto.ProjectId && !x.IsArchived, ct)) throw Bad("An active project is required.");
        var material = await construction.Materials.SingleOrDefaultAsync(x => x.Id == dto.MaterialId && !x.IsArchived, ct) ?? throw Bad("An active inventory material is required.");
        row.RequiredByDate = Utc(dto.RequiredByDate);
        if (row.RequiredByDate.Date < DateTime.UtcNow.Date) throw Bad("Required date cannot be in the past.");
        var deadline = await construction.Projects.Where(x => x.Id == dto.ProjectId).Select(x => x.EndDate).SingleAsync(ct);
        if (deadline != null && DateOnly.FromDateTime(row.RequiredByDate) > deadline) throw Bad("Required date exceeds the project deadline.");
        row.ProjectId = dto.ProjectId; row.MaterialId = material.Id; row.MaterialName = material.Name; row.Unit = material.Unit; row.Quantity = dto.Quantity; row.BudgetLimit = dto.BudgetLimit; row.UpdatedById = actor;
        if (id == null) db.PurchaseRequests.Add(row);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return row;
    }
    public async Task<PurchaseRequest> RequestDecisionAsync(int id, bool approve, Guid actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); await LockAsync(ct);
        var row = await db.PurchaseRequests.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw Missing();
        if (row.Status != "Pending") throw Conflict("Only pending requests can be approved or cancelled.");
        if (approve && (row.MaterialId == null || row.ProjectId == null)) throw Bad("Link the request to a project and inventory material before approval.");
        row.Status = approve ? "Approved" : "Cancelled"; row.UpdatedById = actor;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return row;
    }
    public async Task<PurchaseRequest> LinkLegacyRequestAsync(int id, LegacyProcurementLinksRequest dto, Guid actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); await LockAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(42002001)", ct);
        var row = await db.PurchaseRequests.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw Missing();
        if (row.ProjectId != null && row.MaterialId != null) throw Conflict("Only legacy requests with missing links can use this operation.");
        if (!await construction.Projects.AnyAsync(x => x.Id == dto.ProjectId && !x.IsArchived, ct)) throw Bad("An active project is required.");
        var material = await construction.Materials.AsNoTracking().SingleOrDefaultAsync(x => x.Id == dto.MaterialId && !x.IsArchived, ct) ?? throw Bad("An active material is required.");
        if (!row.MaterialName.Trim().Equals(material.Name, StringComparison.OrdinalIgnoreCase)) throw Bad("Select the inventory material matching this historical request.");
        row.ProjectId = dto.ProjectId; row.MaterialId = material.Id; row.Unit = material.Unit; row.UpdatedById = actor;
        foreach (var order in await db.PurchaseOrders.Where(x => x.PurchaseRequestId == id).ToListAsync(ct)) {
            order.ProjectId = dto.ProjectId; order.MaterialId = material.Id; order.Unit = material.Unit; order.UpdatedById = actor;
        }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return row;
    }

    public async Task<PurchaseOrder> WriteOrderAsync(int? id, CreatePurchaseOrderRequest dto, Guid actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); await LockAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(42002001)", ct);
        var row = id != null ? await db.PurchaseOrders.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw Missing() : new PurchaseOrder { CreatedById = actor };
        if (row.Status != "Pending") throw Conflict("Only pending orders can be edited.");
        if (id != null && (row.PurchaseRequestId != dto.PurchaseRequestId || await db.Deliveries.AnyAsync(x => x.PurchaseOrderId == id && x.Status != "Cancelled" && x.Status != "Rejected", ct))) throw Conflict("Source request is immutable and orders with active deliveries cannot be edited.");
        var request = await db.PurchaseRequests.SingleOrDefaultAsync(x => x.Id == dto.PurchaseRequestId, ct) ?? throw Bad("Purchase request not found.");
        if (id == null && request.Status != "Approved" || id != null && request.Status != "ConvertedToOrder") throw Conflict("An approved request is required.");
        if (await db.PurchaseOrders.AnyAsync(x => x.Id != row.Id && x.PurchaseRequestId == request.Id && x.Status != "Cancelled", ct)) throw Conflict("Request already has an active order.");
        if (request.MaterialId == null || request.ProjectId == null) throw Bad("Request requires project and material links.");
        if (!await construction.Projects.AnyAsync(x => x.Id == request.ProjectId && !x.IsArchived, ct) || !await construction.Materials.AnyAsync(x => x.Id == request.MaterialId && !x.IsArchived && x.Unit == request.Unit, ct)) throw Bad("Request project or material is archived, or its unit changed. Create a new request with the current material.");
        var supplier = await db.Suppliers.SingleOrDefaultAsync(x => x.Id == dto.SupplierId && x.IsActive, ct) ?? throw Bad("Active supplier not found.");
        var link = await db.SupplierMaterials.SingleOrDefaultAsync(x => x.SupplierId == supplier.Id && x.MaterialId == request.MaterialId && x.IsActive, ct) ?? throw Bad("Supplier does not offer the requested material.");
        if (await RemainingSupplyAsync(link, db.PurchaseOrders, db.Deliveries, id, ct) < request.Quantity) throw Conflict("Supplier quantity is insufficient after existing orders.");
        var date = Utc(dto.DeliveryDate);
        if (date.Date < DateTime.UtcNow.Date || date.Date > request.RequiredByDate.Date) throw Bad("Delivery must meet the required date.");
        var price = link.UnitPrice;
        if (dto.QuotationId != null)
        {
            var quote = await db.SupplierQuotations.SingleOrDefaultAsync(x => x.Id == dto.QuotationId && x.IsActive, ct) ?? throw Bad("Valid quotation required.");
            if (quote.SupplierId != supplier.Id || quote.MaterialId != request.MaterialId || quote.Quantity < request.Quantity || quote.ValidUntil == null || quote.ValidUntil < DateTime.UtcNow || quote.DeliveryDate < DateTime.UtcNow || quote.DeliveryDate.Date > request.RequiredByDate.Date) throw Bad("Quotation does not meet material, quantity, validity or deadline requirements.");
            price = quote.UnitPrice; date = quote.DeliveryDate;
        }
        else if (date.Date < DateTime.UtcNow.AddDays(link.LeadTimeDays).Date) throw Bad("Delivery date does not allow supplier lead time.");
        if (dto.UnitPrice != price) throw Bad("Unit price must match the current supplier offer or selected quotation.");
        var cost = price * request.Quantity;
        if (request.BudgetLimit is decimal budget && cost > budget) throw Bad("Order exceeds the approved request budget.");
        row.PurchaseRequestId = request.Id; row.SupplierId = supplier.Id; row.QuotationId = dto.QuotationId; row.MaterialId = request.MaterialId; row.ProjectId = request.ProjectId; row.MaterialName = request.MaterialName; row.Unit = request.Unit; row.Quantity = request.Quantity; row.UnitPrice = price; row.TotalCost = cost; row.DeliveryDate = date; row.UpdatedById = actor;
        request.Status = "ConvertedToOrder"; request.EstimatedUnitPrice = price; request.EstimatedTotalCost = cost; request.UpdatedById = actor;
        if (id == null) db.PurchaseOrders.Add(row);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return row;
    }
    public async Task CancelOrderAsync(int id, Guid actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); await LockAsync(ct);
        var row = await db.PurchaseOrders.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw Missing();
        if (row.Status != "Pending" || await db.Deliveries.AnyAsync(x => x.PurchaseOrderId == id && x.Status != "Cancelled" && x.Status != "Rejected", ct)) throw Conflict("Cancel pending deliveries first; received orders cannot be cancelled.");
        row.Status = "Cancelled"; row.UpdatedById = actor;
        var request = await db.PurchaseRequests.SingleAsync(x => x.Id == row.PurchaseRequestId, ct); request.Status = "Approved"; request.UpdatedById = actor;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task<Delivery> WriteDeliveryAsync(int? id, int orderId, decimal quantity, DateTime date, string? evidence, Guid actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); await LockAsync(ct);
        var row = id != null ? await db.Deliveries.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw Missing() : new Delivery { CreatedById = actor };
        if (row.Status != "Pending") throw Conflict("Only pending deliveries can be edited.");
        if (id != null) orderId = row.PurchaseOrderId;
        var order = await db.PurchaseOrders.SingleOrDefaultAsync(x => x.Id == orderId, ct) ?? throw Bad("Order not found.");
        if (order.Status is "Cancelled" or "Completed") throw Conflict("Order cannot accept deliveries.");
        if (quantity <= 0) throw Bad("Quantity must be positive.");
        var scheduled = await db.Deliveries.Where(x => x.PurchaseOrderId == orderId && x.Id != row.Id && x.Status != "Cancelled" && x.Status != "Rejected").SumAsync(x => x.Quantity, ct);
        if (scheduled + quantity > order.Quantity) throw Conflict("Cumulative delivery quantity exceeds order quantity.");
        date = Utc(date);
        if (date.Date < order.CreatedAt.Date || date.Date > order.DeliveryDate.Date) throw Bad("Delivery date must fall between order creation and its deadline.");
        if (evidence?.Length > 2000 || !string.IsNullOrEmpty(evidence) && !evidence.StartsWith("/api/deliveries/") && !Uri.TryCreate(evidence, UriKind.Absolute, out _)) throw Bad("Invalid evidence URL.");
        row.PurchaseOrderId = orderId; row.MaterialName = order.MaterialName; row.Quantity = quantity; row.DeliveryDate = date; row.EvidenceUrl = evidence ?? ""; row.UpdatedById = actor;
        if (id == null) db.Deliveries.Add(row);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return row;
    }
    public async Task<Delivery> DeliveryStatusAsync(int id, string status, Guid actor, bool office, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); await LockAsync(ct);
        var row = await db.Deliveries.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw Missing();
        var order = await db.PurchaseOrders.SingleAsync(x => x.Id == row.PurchaseOrderId, ct);
        if (!await VisibleAsync(order.ProjectId, actor, office, ct)) throw Missing();
        if (!office && status is not ("Received" or "Completed")) throw new ApiException(403, "forbidden", "Site users can only confirm receipt.");
        if (row.Status == status) return row;
        if (order.Status == "Cancelled" || row.Status is "Completed" or "Cancelled" or "Rejected") throw Conflict("Terminal deliveries cannot be reopened.");
        if (!(row.Status == "Pending" && status is "Received" or "Rejected" or "Cancelled" || row.Status == "Received" && status == "Completed")) throw Conflict("Invalid delivery transition.");
        if (status == "Received")
        {
            if (order.MaterialId == null) throw Bad("Order must reference an inventory material.");
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(42002001)", ct);
            var material = await db.Set<Material>().SingleOrDefaultAsync(x => x.Id == order.MaterialId && !x.IsArchived, ct) ?? throw Bad("Active material not found.");
            material.CurrentStock += row.Quantity; material.ValidateStock(); row.StockReceivedAt = DateTimeOffset.UtcNow;
            db.Set<StockMovement>().Add(new StockMovement { Id = Guid.NewGuid(), MaterialId = material.Id, Type = "Receive", Quantity = row.Quantity, StockAfter = material.CurrentStock, ActorId = actor, Reference = $"Delivery {row.Id}" });
            var offered = await db.SupplierMaterials.SingleOrDefaultAsync(x => x.SupplierId == order.SupplierId && x.MaterialId == order.MaterialId, ct);
            if (offered != null) offered.AvailableQuantity = Math.Max(0, offered.AvailableQuantity - row.Quantity);
        }
        row.Status = status; row.UpdatedById = actor;
        var received = await db.Deliveries.Where(x => x.PurchaseOrderId == order.Id && x.Id != id && (x.Status == "Received" || x.Status == "Completed")).SumAsync(x => x.Quantity, ct);
        if (status is "Received" or "Completed") received += row.Quantity;
        order.Status = received >= order.Quantity ? "Completed" : received > 0 ? "PartiallyReceived" : "Pending"; order.UpdatedById = actor;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return row;
    }
}
