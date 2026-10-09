using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace BuildFlow.Api.Tests.Infrastructure;

public sealed class TestApiFactory(string connectionString) : WebApplicationFactory<global::Program>
{
    public const string JwtSecret = "BuildFlow-test-only-signing-key-at-least-32-characters";
    public const string AdminEmail = "admin@tests.example.invalid";
    public const string AdminPassword = "Test-only-Admin-123!";

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Development");

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Host configuration is available before Program reads JWT/database settings.
        builder.ConfigureHostConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?> {
            ["ConnectionStrings:DefaultConnection"] = connectionString,
            ["Jwt:Issuer"] = "buildflow-tests",
            ["Jwt:Audience"] = "buildflow-tests",
            ["Jwt:Secret"] = JwtSecret,
            ["InitialAdmin:Email"] = AdminEmail,
            ["InitialAdmin:Password"] = AdminPassword
        }));
        return base.CreateHost(builder);
    }
}
