using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Models;
using BuildFlow.Api.Services;
using BuildFlow.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
namespace BuildFlow.Api.Tests.Member03;
[Collection("PostgreSQL"), Trait("Category", "Integration"), Trait("Member", "03")]
public sealed class ProcurementApiIntegrationTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task SupplierCreationPersistsAndCanBeReadThroughApi()
    {
        using var client = await fixture.ClientAsync("ProcurementOfficer");
        var name = "Supplier " + Guid.NewGuid();
        var response = await client.PostAsJsonAsync("/api/Suppliers", new CreateSupplierRequest { Name = name, ContactPerson = "Contact", Email = "supplier@example.invalid", Phone = "+94112345678", Address = "Colombo" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var supplier = (await response.Content.ReadFromJsonAsync<Supplier>())!;
        var read = await client.GetFromJsonAsync<Supplier>($"/api/Suppliers/{supplier.Id}");
        Assert.Equal(name, read!.Name);
        await using var db = fixture.ProcurementDb();
        Assert.True(await db.Suppliers.AnyAsync(s => s.Id == supplier.Id && s.Name == name));
    }
    [Fact]
    public async Task InvalidSupplierEmailIsRejectedByApiValidation()
    {
        using var client = await fixture.ClientAsync("ProcurementOfficer");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Suppliers", new CreateSupplierRequest { Name = "Supplier", ContactPerson = "Contact", Email = "invalid", Phone = "+94112345678", Address = "Colombo" })).StatusCode);
    }
    [Fact]
    public async Task SiteEngineerCannotCompareProcurementQuotations()
    {
        using var client = await fixture.ClientAsync("SiteEngineer");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/Procurement/compare", new ProcurementComparisonRequest { MaterialName = "Cement", RequiredQuantity = 1 })).StatusCode);
    }
    [Fact]
    public async Task ComparisonWithoutMatchingQuotesReturnsApprovalRequiredAndNoSideEffects()
    {
        using var client = await fixture.ClientAsync("ProcurementOfficer");
        var response = await client.PostAsJsonAsync("/api/Procurement/compare", new ProcurementComparisonRequest { MaterialName = "Missing " + Guid.NewGuid(), RequiredQuantity = 5 });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("NoQuotationAvailable", result.GetProperty("status").GetString());
        Assert.True(result.GetProperty("approvalRequired").GetBoolean());
        Assert.Equal(0, result.GetProperty("sideEffects").GetArrayLength());
    }
    [Fact]
    public async Task ServiceRejectsNonPositiveRequestWithoutPersistingIt()
    {
        await using var db = fixture.ProcurementDb();
        await using var main = fixture.MainDb();
        var before = await db.PurchaseRequests.CountAsync();
        var error = await Assert.ThrowsAsync<ApiException>(() => new ProcurementService(db, main).WriteRequestAsync(null, new CreatePurchaseRequestRequest { Quantity = 0 }, Guid.NewGuid(), default));
        Assert.Equal(400, error.StatusCode);
        Assert.Equal(before, await db.PurchaseRequests.CountAsync());
    }
    [Fact]
    public async Task ComparisonChoosesLowestEligibleCostAndExcludesExpiredQuotes()
    {
        await using var main = fixture.MainDb();
        var warehouse = new Warehouse { Name = "Quote warehouse " + Guid.NewGuid() };
        var material = new Material { Name = "Quote cement " + Guid.NewGuid(), Category = "Concrete", Unit = "bags", Warehouse = warehouse };
        main.Materials.Add(material);
        await main.SaveChangesAsync();
        await using var db = fixture.ProcurementDb();
        var supplier = new Supplier { Name = "Quote supplier " + Guid.NewGuid() };
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();
        db.SupplierMaterials.Add(new SupplierMaterial { SupplierId = supplier.Id, MaterialId = material.Id, AvailableQuantity = 100, UnitPrice = 10 });
        var eligible = new SupplierQuotation { SupplierId = supplier.Id, MaterialId = material.Id, MaterialName = material.Name, Unit = material.Unit, Quantity = 100, UnitPrice = 3, TotalPrice = 300, DeliveryDate = DateTime.UtcNow.AddDays(2), ValidUntil = DateTime.UtcNow.AddDays(10) };
        db.SupplierQuotations.AddRange(eligible,
            new SupplierQuotation { SupplierId = supplier.Id, MaterialId = material.Id, MaterialName = material.Name, Unit = material.Unit, Quantity = 100, UnitPrice = 5, TotalPrice = 500, DeliveryDate = DateTime.UtcNow.AddDays(3), ValidUntil = DateTime.UtcNow.AddDays(10) },
            new SupplierQuotation { SupplierId = supplier.Id, MaterialId = material.Id, MaterialName = material.Name, Unit = material.Unit, Quantity = 100, UnitPrice = 1, TotalPrice = 100, DeliveryDate = DateTime.UtcNow.AddDays(1), ValidUntil = DateTime.UtcNow.AddDays(-1) });
        await db.SaveChangesAsync();
        using var client = await fixture.ClientAsync("ProcurementOfficer");
        var response = await client.PostAsJsonAsync("/api/Procurement/compare", new ProcurementComparisonRequest { MaterialName = material.Name, RequiredQuantity = 10, BudgetLimit = 40, RequiredByDate = DateTime.UtcNow.AddDays(5) });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var recommendation = result.GetProperty("recommendedQuotation");
        Assert.Equal(eligible.Id, recommendation.GetProperty("quotationId").GetInt32());
        Assert.Equal(30, recommendation.GetProperty("totalPrice").GetDecimal());
        Assert.Equal(1, result.GetProperty("quotationCount").GetInt32());
        Assert.True(result.GetProperty("approvalRequired").GetBoolean());
    }

}
