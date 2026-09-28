using System.Security.Claims;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildFlow.Api.Controllers;

[ApiController]
[Route("api/workers")]
[Authorize]
public sealed class WorkerController(IWorkerService service) : ControllerBase
{
    private Guid Actor =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public Task<PageResult<WorkerDto>> List(
        [FromQuery] PageQuery query,
        CancellationToken ct)
    {
        return service.ListAsync(query, ct);
    }

    [HttpGet("{id:guid}")]
    public Task<WorkerDto> Get(
        Guid id,
        CancellationToken ct)
    {
        return service.GetAsync(id, ct);
    }

    [HttpPost]
    [Authorize(Roles = "Administrator,ProjectManager")]
    public async Task<ActionResult<WorkerDto>> Create(
        WorkerWriteDto dto,
        CancellationToken ct)
    {
        var result = await service.CreateAsync(dto, Actor, ct);

        return CreatedAtAction(
            nameof(Get),
            new { id = result.Id },
            result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrator,ProjectManager")]
    public Task<WorkerDto> Update(
        Guid id,
        WorkerWriteDto dto,
        CancellationToken ct)
    {
        return service.UpdateAsync(id, dto, Actor, ct);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrator,ProjectManager")]
    public async Task<IActionResult> Archive(
        Guid id,
        CancellationToken ct)
    {
        await service.ArchiveAsync(id, Actor, ct);
        return NoContent();
    }
}
