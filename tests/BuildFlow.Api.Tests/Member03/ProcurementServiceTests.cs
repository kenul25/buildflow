using BuildFlow.Api.Services;
namespace BuildFlow.Api.Tests.Member03;
[Trait("Category", "Unit"), Trait("Member", "03")]
public sealed class ProcurementServiceTests
{
    [Fact]
    public void UnspecifiedDeadlineIsInterpretedAsUtcWithoutChangingTime()
    {
        var input = new DateTime(2030, 1, 2, 12, 30, 0, DateTimeKind.Unspecified);
        var result = ProcurementService.Utc(input);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(input.Ticks, result.Ticks);
    }
    [Fact]
    public void LocalDeadlinePreservesTheInstant()
    {
        var input = new DateTime(2030, 1, 2, 12, 30, 0, DateTimeKind.Local);
        Assert.Equal(input.ToUniversalTime(), ProcurementService.Utc(input));
    }
}
