using BuildFlow.Api.DTOs;
using BuildFlow.Api.Tests.Infrastructure;
namespace BuildFlow.Api.Tests.Member03;
[Trait("Category", "Unit"), Trait("Member", "03")]
public sealed class ProcurementValidationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    public void SupplierEmailMustBePresentAndValid(string email) => Assert.Contains(UnitTestSupport.Validate(new CreateSupplierRequest { Name = "Supplier", ContactPerson = "Contact", Email = email, Phone = "+94112345678", Address = "Colombo" }), e => e.MemberNames.Contains("Email"));
    [Fact]
    public void ValidSupplierPassesValidation() => Assert.Empty(UnitTestSupport.Validate(new CreateSupplierRequest { Name = "Supplier", ContactPerson = "Contact", Email = "supplier@example.invalid", Phone = "+94112345678", Address = "Colombo" }));
}
