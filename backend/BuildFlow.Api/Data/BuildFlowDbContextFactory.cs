using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BuildFlow.Api.Data;

public sealed class BuildFlowDbContextFactory : IDesignTimeDbContextFactory<BuildFlowDbContext>
{
    public BuildFlowDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("BUILDFLOW_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=buildflow;Username=postgres;Password=YOUR_DB_PASSWORD";
        var options = new DbContextOptionsBuilder<BuildFlowDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new BuildFlowDbContext(options);
    }
}