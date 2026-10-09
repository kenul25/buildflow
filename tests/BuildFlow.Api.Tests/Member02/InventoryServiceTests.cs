using BuildFlow.Api.DTOs;
using BuildFlow.Api.Models;
using BuildFlow.Api.Services;
using BuildFlow.Api.Tests.Infrastructure;
using BuildFlow.Api.Repositories;
using Moq;
namespace BuildFlow.Api.Tests.Member02;
[Trait("Category", "Unit"), Trait("Member", "02")]
public sealed class InventoryServiceTests
{
    [Fact]
    public void AvailableStockExcludesReservations() => Assert.Equal(6, new Material { CurrentStock = 10, ReservedStock = 4 }.AvailableStock);
    [Theory]
    [InlineData(-1, 0)]
    [InlineData(10, -1)]
    [InlineData(10, 11)]
    public void InvalidStockInvariantIsRejected(int current, int reserved) => Assert.Throws<InvalidOperationException>(() => new Material { CurrentStock = current, ReservedStock = reserved }.ValidateStock());
    [Fact]
    public async Task InvalidPaginationIsRejectedBeforeRepositoryAccess()
    {
        using var db = UnitTestSupport.MainDb();
        var repository = new Mock<IInventoryRepository>(MockBehavior.Strict);
        var service = new InventoryService(db, repository.Object);
        var error = await Assert.ThrowsAsync<ApiException>(() => service.ListMaterialsAsync(new InventoryPageQuery(PageSize: 101), default));
        Assert.Equal(400, error.StatusCode);
        repository.VerifyNoOtherCalls();
    }
}
