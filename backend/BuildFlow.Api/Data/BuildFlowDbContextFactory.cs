using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BuildFlow.Api.Data;

public sealed class BuildFlowDbContextFactory : IDesignTimeDbContextFactory<BuildFlowDbContext>
{
    public BuildFlowDbContext CreateDbContext(string[] args)
    {
        var connectionString = MigrationConnection.Resolve();
        var options = new DbContextOptionsBuilder<BuildFlowDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new BuildFlowDbContext(options);
    }
}

internal static class MigrationConnection
{
    public static string Resolve()
    {
        var directory = Path.GetDirectoryName(typeof(BuildFlowDbContextFactory).Assembly.Location)!;
        var configuration = new ConfigurationBuilder().SetBasePath(directory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets("1e423a1e-65a2-4452-94d0-4566928104f5")
            .AddEnvironmentVariables().Build();
        return Environment.GetEnvironmentVariable("BUILDFLOW_CONNECTION_STRING")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Configure the migration database connection.");
    }
}
