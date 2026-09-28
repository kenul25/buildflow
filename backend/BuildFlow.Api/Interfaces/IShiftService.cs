using BuildFlow.Api.DTOs;

namespace BuildFlow.Api.Interfaces;

public interface IShiftService
{
    Task<PageResult<ShiftDto>> ListAsync(PageQuery query, CancellationToken ct);

    Task<ShiftDto> GetAsync(
        Guid id,
        CancellationToken ct);

    Task<ShiftDto> CreateAsync(
        ShiftWriteDto dto,
        Guid actorId,
        CancellationToken ct);

    Task<ShiftDto> UpdateAsync(
        Guid id,
        ShiftWriteDto dto,
        Guid actorId,
        CancellationToken ct);

    Task ArchiveAsync(
        Guid id,
        Guid actorId,
        CancellationToken ct);
}