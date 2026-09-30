using System.Security.Claims;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Models;
using BuildFlow.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildFlow.Api.Controllers;

[ApiController]
[Authorize]
public abstract class ConstructionCrudController<T>(IConstructionService service) : ControllerBase where T : ConstructionRecord, new()
{
    protected Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private Guid? Engineer => User.IsInRole(SystemRoles.SiteEngineer) && !User.IsInRole(SystemRoles.Administrator) && !User.IsInRole(SystemRoles.ProjectManager) ? Actor : null;

    [HttpGet]
    public Task<PageResult<ConstructionDto>> List([FromQuery] PageQuery query, CancellationToken ct) => service.ListAsync<T>(query, Engineer, ct);

    [HttpGet("{id:guid}")]
    public Task<ConstructionDto> Get(Guid id, CancellationToken ct) => service.GetAsync<T>(id, Engineer, ct);

    protected async Task<ActionResult<ConstructionDto>> CreateRecord(ConstructionWriteDto dto, CancellationToken ct)
    {
        var result = await service.CreateAsync<T>(dto, Actor, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    protected Task<ConstructionDto> UpdateRecord(Guid id, ConstructionWriteDto dto, CancellationToken ct) => service.UpdateAsync<T>(id, dto, Actor, ct);

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrator,ProjectManager")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    {
        await service.ArchiveAsync<T>(id, Actor, ct);
        return NoContent();
    }
}

[Route("api/projects")]
public sealed class ProjectsController(IConstructionService service) : ConstructionCrudController<Project>(service)
{
    [HttpPost, Authorize(Roles = "Administrator,ProjectManager")]
    public Task<ActionResult<ConstructionDto>> Create(ProjectWriteDto dto, CancellationToken ct) => CreateRecord(dto.ToServiceDto(), ct);
    [HttpPut("{id:guid}"), Authorize(Roles = "Administrator,ProjectManager")]
    public Task<ConstructionDto> Update(Guid id, ProjectWriteDto dto, CancellationToken ct) => UpdateRecord(id, dto.ToServiceDto(), ct);
}
[Route("api/sites")]
public sealed class SitesController(IConstructionService service) : ConstructionCrudController<Site>(service)
{
    [HttpPost, Authorize(Roles = "Administrator,ProjectManager")]
    public Task<ActionResult<ConstructionDto>> Create(SiteWriteDto dto, CancellationToken ct) => CreateRecord(dto.ToServiceDto(), ct);
    [HttpPut("{id:guid}"), Authorize(Roles = "Administrator,ProjectManager")]
    public Task<ConstructionDto> Update(Guid id, SiteWriteDto dto, CancellationToken ct) => UpdateRecord(id, dto.ToServiceDto(), ct);
}
[Route("api/phases")]
public sealed class PhasesController(IConstructionService service) : ConstructionCrudController<ConstructionPhase>(service)
{
    [HttpPost, Authorize(Roles = "Administrator,ProjectManager")]
    public Task<ActionResult<ConstructionDto>> Create(PhaseWriteDto dto, CancellationToken ct) => CreateRecord(dto.ToServiceDto(), ct);
    [HttpPut("{id:guid}"), Authorize(Roles = "Administrator,ProjectManager")]
    public Task<ConstructionDto> Update(Guid id, PhaseWriteDto dto, CancellationToken ct) => UpdateRecord(id, dto.ToServiceDto(), ct);
}
[Route("api/activities")]
public sealed class ActivitiesController(IConstructionService service) : ConstructionCrudController<ConstructionActivity>(service)
{
    [HttpPost, Authorize(Roles = "Administrator,ProjectManager")]
    public Task<ActionResult<ConstructionDto>> Create(ActivityWriteDto dto, CancellationToken ct) => CreateRecord(dto.ToServiceDto(), ct);
    [HttpPut("{id:guid}"), Authorize(Roles = "Administrator,ProjectManager")]
    public Task<ConstructionDto> Update(Guid id, ActivityWriteDto dto, CancellationToken ct) => UpdateRecord(id, dto.ToServiceDto(), ct);
}
