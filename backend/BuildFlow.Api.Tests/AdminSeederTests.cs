using BuildFlow.Api.Configuration;
using BuildFlow.Api.Data;
using BuildFlow.Api.Models;
using BuildFlow.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BuildFlow.Api.Tests;

public sealed class AdminSeederTests
{
    [Fact]
    public async Task Seed_creates_one_administrator_and_is_idempotent()
    {
        await using var database = CreateDatabase();
        var passwordService = new PasswordService();
        var seeder = new AdminSeeder(
            database,
            passwordService,
            Options.Create(new InitialAdminOptions
            {
                Email = "ADMIN@buildflow.com",
                Password = "Admin@1234"
            }),
            NullLogger<AdminSeeder>.Instance);

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        var user = await database.Users
            .Include(item => item.UserRoles)
            .ThenInclude(item => item.Role)
            .SingleAsync();
        Assert.Equal("admin@buildflow.com", user.Email);
        Assert.True(passwordService.Verify("Admin@1234", user.PasswordHash));
        Assert.Single(user.UserRoles);
        Assert.Equal(SystemRoles.Administrator, user.UserRoles.Single().Role.Name);
    }

    private static BuildFlowDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<BuildFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var database = new BuildFlowDbContext(options);
        database.Database.EnsureCreated();
        return database;
    }
}
