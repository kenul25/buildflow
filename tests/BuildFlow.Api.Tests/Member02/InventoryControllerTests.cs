using BuildFlow.Api.Controllers;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Services;
using Moq;
namespace BuildFlow.Api.Tests.Member02;
[Trait("Category", "Unit"), Trait("Member", "02")]
public sealed class InventoryControllerTests
{
    [Fact]
    public async Task AnalysisReturnsServiceShortageReport()
    {
        var requirements = new List<MaterialRequirementDto> { new() { Name = "Cement", Unit = "bags", RequiredQuantity = 12 } };
        var expected = new MaterialAvailabilityReport([new("Cement", "bags", 12, 10, 4, 6, 6, false)], false, DateTimeOffset.UtcNow);
        var service = new Mock<IInventoryService>(MockBehavior.Strict);
        service.Setup(s => s.CheckAvailabilityAsync(requirements, default)).ReturnsAsync(expected);
        var controller = new InventoryController(service.Object, new InventoryAnalysisAgent(service.Object), null!, null!);
        Assert.Same(expected, await controller.Analyze(requirements, default));
        service.VerifyAll();
    }
}
