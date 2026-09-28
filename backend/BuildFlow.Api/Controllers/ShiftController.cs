using System.Security.Claims;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildFlow.Api.Controllers;

[ApiController]
[Route("api/shifts")]
[Authorize]
public sealed class ShiftController(IShiftService service) : ControllerBase
{
    private Guid Actor =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public Task<PageResult<ShiftDto>> List(
        [FromQuery] PageQuery query,
        CancellationToken ct)
        => service.ListAsync(query, ct);

    [HttpGet("{id:guid}")]
    public Task<ShiftDto> Get(
        Guid id,
        CancellationToken ct)
        => service.GetAsync(id, ct);

    [HttpPost]
    [Authorize(Roles = "Administrator,ProjectManager")]
    public Task<ShiftDto> Create(
        [FromBody] ShiftWriteDto dto,
        CancellationToken ct)
        => service.CreateAsync(dto, Actor, ct);

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrator,ProjectManager")]
    public Task<ShiftDto> Update(
        Guid id,
        [FromBody] ShiftWriteDto dto,
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