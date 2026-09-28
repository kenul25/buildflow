using BuildFlow.Api.DTOs;

namespace BuildFlow.Api.Interfaces;

public interface IScheduleService
{
    Task<PageResult<ScheduleDto>> ListAsync(
        PageQuery query,
        CancellationToken ct);

    Task<ScheduleDto> GetAsync(
        Guid id,
        CancellationToken ct);

    Task<ScheduleDto> CreateAsync(
        ScheduleWriteDto dto,
        Guid actorId,
        CancellationToken ct);

    Task<ScheduleDto> UpdateAsync(
        Guid id,
        ScheduleWriteDto dto,
        Guid actorId,
        CancellationToken ct);

    Task ArchiveAsync(
        Guid id,
        Guid actorId,
        CancellationToken ct);
}
