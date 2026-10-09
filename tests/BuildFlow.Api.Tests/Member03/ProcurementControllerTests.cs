using BuildFlow.Api.Controllers;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
namespace BuildFlow.Api.Tests.Member03;
[Trait("Category", "Unit"), Trait("Member", "03")]
public sealed class ProcurementControllerTests
{
    [Theory]
    [InlineData("", 1)]
    [InlineData("Cement", 0)]
    [InlineData("Cement", -1)]
    public async Task InvalidComparisonReturnsBadRequestBeforeDatabaseAccess(string name, int quantity)
    {
        using var db = UnitTestSupport.ProcurementDb();
        var controller = new ProcurementController(db);
        Assert.IsType<BadRequestObjectResult>(await controller.CompareQuotations(new ProcurementComparisonRequest { MaterialName = name, RequiredQuantity = quantity }));
    }
}
