using System.Security.Claims;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildFlow.Api.Controllers;

[ApiController]
[Route("api/equipment-reservations")]
[Authorize]
public sealed class EquipmentReservationController(
    IEquipmentReservationService service) : ControllerBase
{
    private Guid Actor =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public Task<PageResult<EquipmentReservationDto>> List(
        [FromQuery] PageQuery query,
        CancellationToken ct)
        => service.ListAsync(query, ct);

    [HttpGet("{id:guid}")]
    public Task<EquipmentReservationDto> Get(
        Guid id,
        CancellationToken ct)
        => service.GetAsync(id, ct);

    [HttpPost]
    [Authorize(Roles = "Administrator,ProjectManager")]
    public Task<EquipmentReservationDto> Create(
        [FromBody] EquipmentReservationWriteDto dto,
        CancellationToken ct)
        => service.CreateAsync(dto, Actor, ct);

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrator,ProjectManager")]
    public Task<EquipmentReservationDto> Update(
        Guid id,
        [FromBody] EquipmentReservationWriteDto dto,
        CancellationToken ct)
        => service.UpdateAsync(id, dto, Actor, ct);

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
