using System.Net.Http.Headers;
using BuildFlow.Api.Configuration;
using BuildFlow.Api.Data;
using BuildFlow.Api.Models;
using BuildFlow.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace BuildFlow.Api.Tests.Infrastructure;

[CollectionDefinition("PostgreSQL", DisableParallelization = true)]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly string databaseName = "buildflow_xunit_" + Guid.NewGuid().ToString("N");
    private string adminConnection = "";
    private bool databaseCreated;
    public string ConnectionString { get; private set; } = "";
    public TestApiFactory Factory { get; private set; } = null!;

    public BuildFlowDbContext MainDb() => new(new DbContextOptionsBuilder<BuildFlowDbContext>().UseNpgsql(ConnectionString).Options);
    public AppDbContext ProcurementDb() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString).Options);

    public async Task InitializeAsync()
    {
        var source = Environment.GetEnvironmentVariable("BUILDFLOW_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(source))
            throw new InvalidOperationException("Set BUILDFLOW_TEST_CONNECTION_STRING to a local/CI PostgreSQL account with CREATE DATABASE permission. Integration tests never use application credentials implicitly.");
        var settings = new NpgsqlConnectionStringBuilder(source) { Database = "postgres", Pooling = false };
        adminConnection = settings.ConnectionString;
        try
        {
            await using var admin = new NpgsqlConnection(adminConnection);
            await admin.OpenAsync();
            await new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", admin).ExecuteNonQueryAsync();
            databaseCreated = true;
            settings.Database = databaseName;
            ConnectionString = settings.ConnectionString;
            await using var main = MainDb();
            await using var procurement = ProcurementDb();
            await main.Database.MigrateAsync();
            await procurement.Database.MigrateAsync();
            Factory = new TestApiFactory(ConnectionString);
            // Force startup now so configuration/seeding failures are reported by the fixture.
            using var client = Factory.CreateClient();
            (await client.GetAsync("/health")).EnsureSuccessStatusCode();
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async Task<HttpClient> ClientAsync(string role = "ProjectManager", bool active = true, TimeProvider? clock = null)
    {
        await using var db = MainDb();
        var user = new AppUser { FullName = "Test " + role, Email = Guid.NewGuid() + "@tests.example.invalid", IsActive = active, PasswordHash = "unused" };
        user.NormalizedEmail = user.Email.ToUpperInvariant();
        var persistedRole = await db.Roles.SingleAsync(r => r.Name == role);
        user.UserRoles = [new UserRole { RoleId = persistedRole.Id }];
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var tokenService = new TokenService(Options.Create(new JwtOptions {
            Issuer = "buildflow-tests", Audience = "buildflow-tests", Secret = TestApiFactory.JwtSecret
        }), clock ?? TimeProvider.System);
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenService.CreateAccessToken(user, [role]).Token);
        return client;
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null) await Factory.DisposeAsync();
        if (!databaseCreated) return;
        await using var admin = new NpgsqlConnection(adminConnection);
        await admin.OpenAsync();
        await new NpgsqlCommand($"DROP DATABASE \"{databaseName}\" WITH (FORCE)", admin).ExecuteNonQueryAsync();
        databaseCreated = false;
    }
}
