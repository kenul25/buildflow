using BuildFlow.Api.DTOs;

namespace BuildFlow.Api.Interfaces;

public interface IEquipmentReservationService
{
    Task<PageResult<EquipmentReservationDto>> ListAsync(
        PageQuery query,
        CancellationToken ct);

    Task<EquipmentReservationDto> GetAsync(
        Guid id,
        CancellationToken ct);

    Task<EquipmentReservationDto> CreateAsync(
        EquipmentReservationWriteDto dto,
        Guid actorId,
        CancellationToken ct);

    Task<EquipmentReservationDto> UpdateAsync(
        Guid id,
        EquipmentReservationWriteDto dto,
        Guid actorId,
        CancellationToken ct);

    Task ArchiveAsync(
        Guid id,
        Guid actorId,
        CancellationToken ct);
}
