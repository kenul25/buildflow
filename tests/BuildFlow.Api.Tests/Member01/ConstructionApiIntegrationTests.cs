using System.Net;
using System.Net.Http.Json;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Models;
using BuildFlow.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
namespace BuildFlow.Api.Tests.Member01;
[Collection("PostgreSQL"), Trait("Category", "Integration"), Trait("Member", "01")]
public sealed class ConstructionApiIntegrationTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task ProjectCanBeCreatedReadAndArchivedWithPostgreSqlPersistence()
    {
        using var client = await fixture.ClientAsync();
        var code = "T" + Guid.NewGuid().ToString("N")[..20];
        var response = await client.PostAsJsonAsync("/api/projects", new ProjectWriteDto { Name = "Test project", Code = code });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var project = (await response.Content.ReadFromJsonAsync<ConstructionDto>())!;
        Assert.NotNull(response.Headers.Location);
        var read = await client.GetFromJsonAsync<ConstructionDto>($"/api/projects/{project.Id}");
        Assert.Equal(code.ToUpperInvariant(), read!.Code);
        await using var db = fixture.MainDb();
        Assert.True(await db.Projects.AnyAsync(p => p.Id == project.Id && p.Code == code.ToUpperInvariant()));
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/projects/{project.Id}")).StatusCode);
        Assert.True(await db.Projects.AnyAsync(p => p.Id == project.Id && p.IsArchived));
    }
    [Fact]
    public async Task ReversedProjectDatesAreRejectedAndDoNotPersist()
    {
        using var client = await fixture.ClientAsync();
        var code = "T" + Guid.NewGuid().ToString("N")[..20];
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var response = await client.PostAsJsonAsync("/api/projects", new ProjectWriteDto { Name = "Invalid dates", Code = code, StartDate = date, EndDate = date.AddDays(-1) });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var db = fixture.MainDb();
        Assert.False(await db.Projects.AnyAsync(p => p.Code == code.ToUpperInvariant()));
    }
    [Fact]
    public async Task SiteEngineerCannotCreateProject()
    {
        using var client = await fixture.ClientAsync("SiteEngineer");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/projects", new ProjectWriteDto { Name = "Denied project", Code = "DENIED" })).StatusCode);
    }
    [Fact]
    public async Task EngineerCannotReadAnotherEngineersProject()
    {
        using var engineer = await fixture.ClientAsync("SiteEngineer");
        var me = (await engineer.GetFromJsonAsync<UserResponse>("/api/auth/me"))!;
        await using var db = fixture.MainDb();
        var project = new Project { Name = "Assigned project", Code = "T" + Guid.NewGuid().ToString("N")[..20], AssignedEngineerId = me.Id };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.OK, (await engineer.GetAsync($"/api/projects/{project.Id}")).StatusCode);
        using var other = await fixture.ClientAsync("SiteEngineer");
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/projects/{project.Id}")).StatusCode);
    }

}
