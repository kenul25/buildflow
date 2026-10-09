using BuildFlow.Api.DTOs;
using BuildFlow.Api.Tests.Infrastructure;
namespace BuildFlow.Api.Tests.Member02;
[Trait("Category", "Unit"), Trait("Member", "02")]
public sealed class InventoryValidationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void StockAdjustmentMustBePositive(int quantity) => Assert.Contains(UnitTestSupport.Validate(new StockAdjustmentDto { Quantity = quantity }), e => e.MemberNames.Contains("Quantity"));
    [Fact]
    public void ValidWarehousePassesValidation() => Assert.Empty(UnitTestSupport.Validate(new WarehouseWriteDto { Name = "Central warehouse" }));
}
