using BuildFlow.Api.DTOs;

namespace BuildFlow.Api.Interfaces;

public interface ISkillService
{
    Task<PageResult<SkillDto>> ListAsync(
        PageQuery query,
        CancellationToken ct);

    Task<SkillDto> GetAsync(
        Guid id,
        CancellationToken ct);

    Task<SkillDto> CreateAsync(
        SkillWriteDto dto,
        Guid actorId,
        CancellationToken ct);

    Task<SkillDto> UpdateAsync(
        Guid id,
        SkillWriteDto dto,
        Guid actorId,
        CancellationToken ct);

    Task ArchiveAsync(
        Guid id,
        Guid actorId,
        CancellationToken ct);
}