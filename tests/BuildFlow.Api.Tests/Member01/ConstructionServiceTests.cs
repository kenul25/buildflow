using BuildFlow.Api.DTOs;
using BuildFlow.Api.Models;
using BuildFlow.Api.Repositories;
using BuildFlow.Api.Services;
using BuildFlow.Api.Tests.Infrastructure;
using Moq;
namespace BuildFlow.Api.Tests.Member01;
[Trait("Category", "Unit"), Trait("Member", "01")]
public sealed class ConstructionServiceTests
{
    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 101)]
    public async Task InvalidPaginationIsRejectedBeforeRepositoryAccess(int page, int size)
    {
        using var db = UnitTestSupport.MainDb();
        var repository = new Mock<IConstructionRepository>(MockBehavior.Strict);
        var service = new ConstructionService(repository.Object, db);
        var error = await Assert.ThrowsAsync<ApiException>(() => service.ListAsync<Project>(new PageQuery(Page: page, PageSize: size), null, default));
        Assert.Equal(400, error.StatusCode);
        repository.VerifyNoOtherCalls();
    }
    [Fact]
    public void EquipmentEffortIsDistributedAcrossResourceCount()
    {
        var item = new ResourceRequestItem { Kind = "Equipment", Quantity = 16, Unit = "hours", ResourceCount = 2 };
        Assert.Equal(2, ResourceUsage.Count(item));
        Assert.Equal(8, ResourceUsage.Hours(item));
    }
}
