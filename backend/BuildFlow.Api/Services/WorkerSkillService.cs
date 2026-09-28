using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Interfaces;
using BuildFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Services;

public sealed class WorkerSkillService : IWorkerSkillService
{
    private readonly BuildFlowDbContext _db;

    public WorkerSkillService(BuildFlowDbContext db)
    {
        _db = db;
    }

    public async Task<PageResult<WorkerSkillDto>> ListAsync(
        PageQuery query,
        CancellationToken ct)
    {
        var workerSkills = _db.WorkerSkills
            .AsNoTracking()
            .Include(x => x.Worker)
            .Include(x => x.Skill)
            .Where(x => !x.Worker.IsArchived && !x.Skill.IsArchived)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            workerSkills = workerSkills.Where(x =>
                x.Worker.EmployeeCode.Contains(search) ||
                x.Worker.FullName.Contains(search) ||
                x.Skill.Name.Contains(search) ||
                (x.ProficiencyLevel != null &&
                 x.ProficiencyLevel.Contains(search)));
        }

        var total = await workerSkills.CountAsync(ct);

        workerSkills = query.Sort?.ToLower() switch
        {
            "skill" => query.Desc
                ? workerSkills.OrderByDescending(x => x.Skill.Name)
                : workerSkills.OrderBy(x => x.Skill.Name),

            "createdat" => query.Desc
                ? workerSkills.OrderByDescending(x => x.CreatedAt)
                : workerSkills.OrderBy(x => x.CreatedAt),

            _ => query.Desc
                ? workerSkills.OrderByDescending(x => x.Worker.FullName)
                : workerSkills.OrderBy(x => x.Worker.FullName)
        };

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0
            ? 20
            : Math.Min(query.PageSize, 100);

        var items = await workerSkills
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new WorkerSkillDto
            {
                Id = x.Id,
                WorkerId = x.WorkerId,
                SkillId = x.SkillId,
                ProficiencyLevel = x.ProficiencyLevel,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return new PageResult<WorkerSkillDto>(
            items,
            total,
            page,
            pageSize);
    }

    public async Task<WorkerSkillDto> GetAsync(
        Guid id,
        CancellationToken ct)
    {
        var workerSkill = await _db.WorkerSkills
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (workerSkill is null)
        {
            throw new ApiException(
                StatusCodes.Status404NotFound,
                "WORKER_SKILL_NOT_FOUND",
                "Worker skill not found.");
        }

        return ToDto(workerSkill);
    }

    public async Task<WorkerSkillDto> CreateAsync(
        WorkerSkillWriteDto dto,
        Guid actorId,
        CancellationToken ct)
    {
        var worker = await _db.Workers
            .FirstOrDefaultAsync(x => x.Id == dto.WorkerId, ct);

        if (worker is null)
        {
            throw new ApiException(
                StatusCodes.Status404NotFound,
                "WORKER_NOT_FOUND",
                "Worker not found.");
        }

        if (worker.IsArchived || !worker.IsActive)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "WORKER_INACTIVE",
                "Inactive or archived workers cannot receive skills.");
        }

        var skill = await _db.Skills
            .FirstOrDefaultAsync(x => x.Id == dto.SkillId, ct);

        if (skill is null)
        {
            throw new ApiException(
                StatusCodes.Status404NotFound,
                "SKILL_NOT_FOUND",
                "Skill not found.");
        }

        if (skill.IsArchived || !skill.IsActive)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "SKILL_INACTIVE",
                "Inactive or archived skills cannot be assigned.");
        }

        var duplicate = await _db.WorkerSkills.AnyAsync(
            x => x.WorkerId == dto.WorkerId &&
                 x.SkillId == dto.SkillId,
            ct);

        if (duplicate)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "WORKER_SKILL_EXISTS",
                "This skill is already assigned to the worker.");
        }

        var workerSkill = new WorkerSkill
        {
            Id = Guid.NewGuid(),
            WorkerId = dto.WorkerId,
            SkillId = dto.SkillId,
            ProficiencyLevel = string.IsNullOrWhiteSpace(dto.ProficiencyLevel)
                ? null
                : dto.ProficiencyLevel.Trim()
        };

        _db.WorkerSkills.Add(workerSkill);

        await _db.SaveChangesAsync(ct);

        return ToDto(workerSkill);
    }

    public async Task<WorkerSkillDto> UpdateAsync(
        Guid id,
        WorkerSkillWriteDto dto,
        Guid actorId,
        CancellationToken ct)
    {
        var workerSkill = await _db.WorkerSkills
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (workerSkill is null)
        {
            throw new ApiException(
                StatusCodes.Status404NotFound,
                "WORKER_SKILL_NOT_FOUND",
                "Worker skill not found.");
        }

        var worker = await _db.Workers
            .FirstOrDefaultAsync(x => x.Id == dto.WorkerId, ct);

        if (worker is null)
        {
            throw new ApiException(
                StatusCodes.Status404NotFound,
                "WORKER_NOT_FOUND",
                "Worker not found.");
        }

        if (worker.IsArchived || !worker.IsActive)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "WORKER_INACTIVE",
                "Inactive or archived workers cannot receive skills.");
        }

        var skill = await _db.Skills
            .FirstOrDefaultAsync(x => x.Id == dto.SkillId, ct);

        if (skill is null)
        {
            throw new ApiException(
                StatusCodes.Status404NotFound,
                "SKILL_NOT_FOUND",
                "Skill not found.");
        }

        if (skill.IsArchived || !skill.IsActive)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "SKILL_INACTIVE",
                "Inactive or archived skills cannot be assigned.");
        }

        var duplicate = await _db.WorkerSkills.AnyAsync(
            x => x.Id != id &&
                 x.WorkerId == dto.WorkerId &&
                 x.SkillId == dto.SkillId,
            ct);

        if (duplicate)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "WORKER_SKILL_EXISTS",
                "This skill is already assigned to the worker.");
        }

        workerSkill.WorkerId = dto.WorkerId;
        workerSkill.SkillId = dto.SkillId;
        workerSkill.ProficiencyLevel =
            string.IsNullOrWhiteSpace(dto.ProficiencyLevel)
                ? null
                : dto.ProficiencyLevel.Trim();
        workerSkill.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        return ToDto(workerSkill);
    }

    public async Task ArchiveAsync(
        Guid id,
        Guid actorId,
        CancellationToken ct)
    {
        var workerSkill = await _db.WorkerSkills
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (workerSkill is null)
        {
            throw new ApiException(
                StatusCodes.Status404NotFound,
                "WORKER_SKILL_NOT_FOUND",
                "Worker skill not found.");
        }

        _db.WorkerSkills.Remove(workerSkill);

        await _db.SaveChangesAsync(ct);
    }

    private static WorkerSkillDto ToDto(WorkerSkill workerSkill)
    {
        return new WorkerSkillDto
        {
            Id = workerSkill.Id,
            WorkerId = workerSkill.WorkerId,
            SkillId = workerSkill.SkillId,
            ProficiencyLevel = workerSkill.ProficiencyLevel,
            CreatedAt = workerSkill.CreatedAt,
            UpdatedAt = workerSkill.UpdatedAt
        };
    }
}
