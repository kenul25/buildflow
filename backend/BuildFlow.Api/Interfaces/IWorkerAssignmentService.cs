using BuildFlow.Api.DTOs;

namespace BuildFlow.Api.Interfaces;

public interface IWorkerAssignmentService
{
    Task<PageResult<WorkerAssignmentDto>> ListAsync(
        PageQuery query,
        CancellationToken ct);

    Task<WorkerAssignmentDto> GetAsync(
        Guid id,
        CancellationToken ct);

    Task<WorkerAssignmentDto> CreateAsync(
        WorkerAssignmentWriteDto dto,
        Guid actorId,
        CancellationToken ct);

    Task<WorkerAssignmentDto> UpdateAsync(
        Guid id,
        WorkerAssignmentWriteDto dto,
        Guid actorId,
        CancellationToken ct);

    Task ArchiveAsync(
        Guid id,
        Guid actorId,
        CancellationToken ct);
}