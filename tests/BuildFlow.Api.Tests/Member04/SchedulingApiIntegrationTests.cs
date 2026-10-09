using System.Net;
using System.Net.Http.Json;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Models;
using BuildFlow.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
namespace BuildFlow.Api.Tests.Member04;
[Collection("PostgreSQL"), Trait("Category", "Integration"), Trait("Member", "04")]
public sealed class SchedulingApiIntegrationTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task WorkerCanBeCreatedReadAndArchivedWithPostgreSqlPersistence()
    {
        using var client = await fixture.ClientAsync();
        var response = await client.PostAsJsonAsync("/api/scheduling/workers", new SchedulingWriteDto { Name = "Worker " + Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var worker = (await response.Content.ReadFromJsonAsync<Worker>())!;
        Assert.NotNull(response.Headers.Location);
        var read = await client.GetFromJsonAsync<Worker>($"/api/scheduling/workers/{worker.Id}");
        Assert.Equal(worker.Name, read!.Name);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/scheduling/workers/{worker.Id}")).StatusCode);
        await using var db = fixture.MainDb();
        Assert.True(await db.Set<Worker>().AnyAsync(w => w.Id == worker.Id && w.IsArchived));
    }
    [Fact]
    public async Task InvalidWorkerNameIsRejectedByApiValidation()
    {
        using var client = await fixture.ClientAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/scheduling/workers", new SchedulingWriteDto { Name = "" })).StatusCode);
    }
    [Fact]
    public async Task SiteEngineerCannotCreateWorkers()
    {
        using var client = await fixture.ClientAsync("SiteEngineer");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/scheduling/workers", new SchedulingWriteDto { Name = "Denied worker" })).StatusCode);
    }
    [Fact]
    public async Task OverlappingShiftsAreRejectedWithoutAddingSecondShift()
    {
        using var client = await fixture.ClientAsync();
        var created = await client.PostAsJsonAsync("/api/scheduling/workers", new SchedulingWriteDto { Name = "Shift worker " + Guid.NewGuid() });
        created.EnsureSuccessStatusCode();
        var worker = (await created.Content.ReadFromJsonAsync<Worker>())!;
        var start = DateTimeOffset.UtcNow.AddDays(5);
        var shift = new SchedulingWriteDto { Name = "Day shift", WorkerId = worker.Id, StartTime = start, EndTime = start.AddHours(8) };
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/scheduling/shifts", shift)).StatusCode);
        shift.StartTime = start.AddHours(1);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/scheduling/shifts", shift)).StatusCode);
        await using var db = fixture.MainDb();
        Assert.Equal(1, await db.Set<Shift>().CountAsync(s => s.WorkerId == worker.Id));
    }
}
