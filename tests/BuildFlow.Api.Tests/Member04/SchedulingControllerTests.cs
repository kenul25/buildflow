using BuildFlow.Api.Controllers;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Services;
using BuildFlow.Api.Tests.Infrastructure;
namespace BuildFlow.Api.Tests.Member04;
[Trait("Category", "Unit"), Trait("Member", "04")]
public sealed class SchedulingControllerTests
{
    [Fact]
    public async Task InvalidListPaginationPropagatesClientError()
    {
        using var db = UnitTestSupport.MainDb();
        var controller = new WorkersController(new SchedulingService(db));
        UnitTestSupport.SetActor(controller, Guid.NewGuid());
        var error = await Assert.ThrowsAsync<ApiException>(() => controller.List(new SchedulingQuery(Page: 0), default));
        Assert.Equal(400, error.StatusCode);
    }
}
