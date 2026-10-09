using BuildFlow.Api.Services;
namespace BuildFlow.Api.Tests.Member04;
[Trait("Category", "Unit"), Trait("Member", "04")]
public sealed class SchedulingValidationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void EndMustBeAfterStart(int hours)
    {
        var start = new DateTimeOffset(2030, 1, 1, 8, 0, 0, TimeSpan.Zero);
        var error = Assert.Throws<ApiException>(() => SchedulingService.ValidateWindow(start, start.AddHours(hours)));
        Assert.Equal(400, error.StatusCode);
        Assert.Equal("invalid_schedule", error.Code);
    }
}
