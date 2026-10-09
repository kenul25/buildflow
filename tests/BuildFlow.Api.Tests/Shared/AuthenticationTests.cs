using BuildFlow.Api.Services;
using BuildFlow.Api.Validators;
namespace BuildFlow.Api.Tests.Shared;
[Trait("Category", "Unit")]
public sealed class AuthenticationTests
{
    [Fact]
    public void PasswordHashVerifiesOnlyCorrectPasswordAndUsesUniqueSalt()
    {
        var service = new PasswordService();
        var first = service.Hash("Correct-Password-123!");
        Assert.True(service.Verify("Correct-Password-123!", first));
        Assert.False(service.Verify("Wrong-Password-123!", first));
        Assert.NotEqual(first, service.Hash("Correct-Password-123!"));
    }
    [Theory]
    [InlineData("broken")]
    [InlineData("pbkdf2-sha512.120000.!.!")]
    public void MalformedPasswordHashIsRejected(string hash) => Assert.False(new PasswordService().Verify("Password123!", hash));
    [Theory]
    [InlineData("password")]
    [InlineData("Password1")]
    [InlineData("PASSWORD1!")]
    public void WeakPasswordIsRejected(string password) => Assert.False(new PasswordRequirementsAttribute().IsValid(password));
}
