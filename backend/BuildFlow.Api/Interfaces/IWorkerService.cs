using BuildFlow.Api.DTOs;

namespace BuildFlow.Api.Interfaces;

public interface IWorkerService
{
    Task<PageResult<WorkerDto>> ListAsync(
        PageQuery query,
        CancellationToken ct);

    Task<WorkerDto> GetAsync(
        Guid id,
        CancellationToken ct);

    Task<WorkerDto> CreateAsync(
        WorkerWriteDto dto,
        Guid actorId,
        CancellationToken ct);

    Task<WorkerDto> UpdateAsync(
        Guid id,
        WorkerWriteDto dto,
        Guid actorId,
        CancellationToken ct);

    Task ArchiveAsync(
        Guid id,
        Guid actorId,
        CancellationToken ct);
}