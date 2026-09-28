using BuildFlow.Api.DTOs;

namespace BuildFlow.Api.Interfaces;

public interface IEquipmentService
{
    Task<PageResult<EquipmentDto>> ListAsync(
        PageQuery query,
        CancellationToken ct);

    Task<EquipmentDto> GetAsync(
        Guid id,
        CancellationToken ct);

    Task<EquipmentDto> CreateAsync(
        EquipmentWriteDto dto,
        Guid actorId,
        CancellationToken ct);

    Task<EquipmentDto> UpdateAsync(
        Guid id,
        EquipmentWriteDto dto,
        Guid actorId,
        CancellationToken ct);

    Task ArchiveAsync(
        Guid id,
        Guid actorId,
        CancellationToken ct);
}