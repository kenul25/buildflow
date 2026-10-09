using BuildFlow.Api.DTOs;
using BuildFlow.Api.Tests.Infrastructure;
namespace BuildFlow.Api.Tests.Member01;
[Trait("Category", "Unit"), Trait("Member", "01")]
public sealed class ConstructionValidationTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void ProgressOutsidePercentageRangeIsRejected(int value) =>
        Assert.Contains(UnitTestSupport.Validate(new ProgressWriteDto { ProgressPercent = value, WorkCompleted = "Concrete poured" }), e => e.MemberNames.Contains("ProgressPercent"));
    [Fact]
    public void ProjectRequiresCode() => Assert.Contains(UnitTestSupport.Validate(new ProjectWriteDto { Name = "Test project" }), e => e.MemberNames.Contains("Code"));
    [Fact]
    public void ValidProjectPassesValidation() => Assert.Empty(UnitTestSupport.Validate(new ProjectWriteDto { Name = "Test project", Code = "TEST", Status = "Planned" }));
}
