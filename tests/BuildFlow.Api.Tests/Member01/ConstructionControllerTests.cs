using BuildFlow.Api.Controllers;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Models;
using BuildFlow.Api.Services;
using BuildFlow.Api.Tests.Infrastructure;
using Moq;
namespace BuildFlow.Api.Tests.Member01;
[Trait("Category", "Unit"), Trait("Member", "01")]
public sealed class ConstructionControllerTests
{
    [Fact]
    public async Task EngineerListPassesActorToServiceToRestrictProjectVisibility()
    {
        var actor = Guid.NewGuid();
        var query = new PageQuery();
        var expected = new PageResult<ConstructionDto>([], 0, 1, 20);
        var service = new Mock<IConstructionService>(MockBehavior.Strict);
        service.Setup(s => s.ListAsync<Project>(query, actor, default)).ReturnsAsync(expected);
        var controller = new ProjectsController(service.Object);
        UnitTestSupport.SetActor(controller, actor, "SiteEngineer");
        Assert.Same(expected, await controller.List(query, default));
        service.VerifyAll();
    }
}
