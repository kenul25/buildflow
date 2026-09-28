using System.Security.Claims;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildFlow.Api.Controllers;

[ApiController]
[Route("api/worker-skills")]
[Authorize]
public sealed class WorkerSkillController(IWorkerSkillService service) : ControllerBase
{
    private Guid Actor =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public Task<PageResult<WorkerSkillDto>> List(
        [FromQuery] PageQuery query,
        CancellationToken ct)
        => service.ListAsync(query, ct);

    [HttpGet("{id:guid}")]
    public Task<WorkerSkillDto> Get(
        Guid id,
        CancellationToken ct)
        => service.GetAsync(id, ct);

    [HttpPost]
    [Authorize(Roles = "Administrator,ProjectManager")]
    public Task<WorkerSkillDto> Create(
        [FromBody] WorkerSkillWriteDto dto,
        CancellationToken ct)
        => service.CreateAsync(dto, Actor, ct);

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrator,ProjectManager")]
    public Task<WorkerSkillDto> Update(
        Guid id,
        [FromBody] WorkerSkillWriteDto dto,
        CancellationToken ct)
        => service.UpdateAsync(id, dto, Actor, ct);

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrator,ProjectManager")]
    public Task<IActionResult> Archive(
        Guid id,
        CancellationToken ct)
        => ArchiveInternal(id, ct);

    private async Task<IActionResult> ArchiveInternal(
        Guid id,
        CancellationToken ct)
    {
        await service.ArchiveAsync(id, Actor, ct);
        return NoContent();
    }
}