using BuildFlow.Api.DTOs;

namespace BuildFlow.Api.Interfaces;

public interface IWorkerSkillService
{
    Task<PageResult<WorkerSkillDto>> ListAsync(PageQuery query, CancellationToken ct);
    Task<WorkerSkillDto> GetAsync(Guid id, CancellationToken ct);
    Task<WorkerSkillDto> CreateAsync(
        WorkerSkillWriteDto dto,
        Guid actorId,
        CancellationToken ct);
    Task<WorkerSkillDto> UpdateAsync(
        Guid id,
        WorkerSkillWriteDto dto,
        Guid actorId,
        CancellationToken ct);
    Task ArchiveAsync(
        Guid id,
        Guid actorId,
        CancellationToken ct);
}