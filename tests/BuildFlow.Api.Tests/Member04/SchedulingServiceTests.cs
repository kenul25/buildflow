using BuildFlow.Api.Services;
using BuildFlow.Api.Models;
using BuildFlow.Api.Tests.Infrastructure;
namespace BuildFlow.Api.Tests.Member04;
[Trait("Category", "Unit"), Trait("Member", "04")]
public sealed class SchedulingServiceTests
{
    private static readonly DateTimeOffset Start = new(2030, 1, 1, 8, 0, 0, TimeSpan.Zero);
    [Fact]
    public void IntersectingBookingsOverlap() => Assert.True(SchedulingService.Overlaps(Start, Start.AddHours(2), Start.AddHours(1), Start.AddHours(3)));
    [Fact]
    public void AdjacentBookingsDoNotOverlap() => Assert.False(SchedulingService.Overlaps(Start, Start.AddHours(2), Start.AddHours(2), Start.AddHours(4)));
    [Fact]
    public void ValidWindowIsAccepted() => SchedulingService.ValidateWindow(Start, Start.AddHours(1));
    [Fact]
    public async Task EngineerWriteIsDeniedBeforeDatabaseAccess()
    {
        using var db = UnitTestSupport.MainDb();
        var error = await Assert.ThrowsAsync<ApiException>(() => new SchedulingService(db).WriteAsync<Worker>(null, new() { Name = "Worker" }, Guid.NewGuid(), false, default));
        Assert.Equal(403, error.StatusCode);
    }

}
