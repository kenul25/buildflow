using System.Net;
using System.Net.Http.Json;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Models;
using BuildFlow.Api.Repositories;
using BuildFlow.Api.Services;
using BuildFlow.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
namespace BuildFlow.Api.Tests.Member02;
[Collection("PostgreSQL"), Trait("Category", "Integration"), Trait("Member", "02")]
public sealed class InventoryApiIntegrationTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task WarehouseCreationPersistsAndCanBeReadThroughApi()
    {
        using var client = await fixture.ClientAsync("InventoryOfficer");
        var response = await client.PostAsJsonAsync("/api/inventory/warehouses", new WarehouseWriteDto { Name = "Warehouse " + Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var warehouse = (await response.Content.ReadFromJsonAsync<WarehouseDto>())!;
        var rows = await client.GetFromJsonAsync<List<WarehouseDto>>("/api/inventory/warehouses");
        Assert.Contains(rows!, w => w.Id == warehouse.Id);
        await using var db = fixture.MainDb();
        Assert.True(await db.Warehouses.AnyAsync(w => w.Id == warehouse.Id));
    }
    [Fact]
    public async Task ZeroStockAdjustmentIsRejectedByApiValidation()
    {
        using var client = await fixture.ClientAsync("InventoryOfficer");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/inventory/materials/{Guid.NewGuid()}/receive", new StockAdjustmentDto { Quantity = 0 })).StatusCode);
    }
    [Fact]
    public async Task SiteEngineerCannotCreateWarehouse()
    {
        using var client = await fixture.ClientAsync("SiteEngineer");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/inventory/warehouses", new WarehouseWriteDto { Name = "Denied warehouse" })).StatusCode);
    }
    [Fact]
    public async Task ReservationRejectsShortageAndReleaseIsIdempotentInPostgreSql()
    {
        await using var db = fixture.MainDb();
        var project = new Project { Name = "Inventory test", Code = "T" + Guid.NewGuid().ToString("N")[..20] };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        var service = new InventoryService(db, new InventoryRepository(db));
        var warehouse = await service.CreateWarehouseAsync(new() { Name = "Reservation warehouse " + Guid.NewGuid() }, default);
        var material = await service.CreateMaterialAsync(new() { Name = "Cement " + Guid.NewGuid(), Category = "Concrete", Unit = "bags", CurrentStock = 10, WarehouseId = warehouse.Id }, default);
        var reservation = await service.ReserveAsync(material.Id, new() { ProjectId = project.Id, Quantity = 4 }, default);
        var error = await Assert.ThrowsAsync<ApiException>(() => service.ReserveAsync(material.Id, new() { ProjectId = project.Id, Quantity = 7 }, default));
        Assert.Equal(409, error.StatusCode);
        await service.ReleaseReservationAsync(reservation.Id, default);
        await service.ReleaseReservationAsync(reservation.Id, default);
        await using var verify = fixture.MainDb();
        var saved = await verify.Materials.SingleAsync(m => m.Id == material.Id);
        Assert.Equal(10, saved.CurrentStock);
        Assert.Equal(0, saved.ReservedStock);
    }
}
