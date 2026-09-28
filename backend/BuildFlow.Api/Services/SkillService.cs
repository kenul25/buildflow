using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Interfaces;
using BuildFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Services;

public sealed class SkillService : ISkillService
{
    private readonly BuildFlowDbContext _db;

    public SkillService(BuildFlowDbContext db)
    {
        _db = db;
    }

    public async Task<PageResult<SkillDto>> ListAsync(
        PageQuery query,
        CancellationToken ct)
    {
        var skills = _db.Skills
            .AsNoTracking()
            .AsQueryable();

        if (!query.IncludeArchived)
        {
            skills = skills.Where(x => !x.IsArchived);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            skills = skills.Where(x =>
                x.Name.Contains(search) ||
                (x.Description != null && x.Description.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            skills = query.Status.ToLower() switch
            {
                "active" => skills.Where(x => x.IsActive && !x.IsArchived),
                "inactive" => skills.Where(x => !x.IsActive && !x.IsArchived),
                "archived" => skills.Where(x => x.IsArchived),
                _ => skills
            };
        }

        var total = await skills.CountAsync(ct);

        skills = query.Sort?.ToLower() switch
        {
            "name" => query.Desc
                ? skills.OrderByDescending(x => x.Name)
                : skills.OrderBy(x => x.Name),

            "updatedat" => query.Desc
                ? skills.OrderByDescending(x => x.UpdatedAt)
                : skills.OrderBy(x => x.UpdatedAt),

            "createdat" => query.Desc
                ? skills.OrderByDescending(x => x.CreatedAt)
                : skills.OrderBy(x => x.CreatedAt),

            _ => skills.OrderBy(x => x.Name)
        };

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0
            ? 20
            : Math.Min(query.PageSize, 100);

        var items = await skills
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SkillDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                IsActive = x.IsActive,
                IsArchived = x.IsArchived,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return new PageResult<SkillDto>(
            items,
            total,
            page,
            pageSize);
    }

    public async Task<SkillDto> GetAsync(
        Guid id,
        CancellationToken ct)
    {
        var skill = await _db.Skills
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (skill is null)
        {
            throw new ApiException(
                StatusCodes.Status404NotFound,
                "SKILL_NOT_FOUND",
                "Skill not found.");
        }

        return ToDto(skill);
    }

    public async Task<SkillDto> CreateAsync(
        SkillWriteDto dto,
        Guid actorId,
        CancellationToken ct)
    {
        var name = dto.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "SKILL_NAME_REQUIRED",
                "Skill name is required.");
        }

        var exists = await _db.Skills
            .AnyAsync(
                x => x.Name.ToLower() == name.ToLower(),
                ct);

        if (exists)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "SKILL_NAME_EXISTS",
                "A skill with this name already exists.");
        }

        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = string.IsNullOrWhiteSpace(dto.Description)
                ? null
                : dto.Description.Trim(),
            IsActive = true,
            IsArchived = false
        };

        _db.Skills.Add(skill);

        await _db.SaveChangesAsync(ct);

        return ToDto(skill);
    }

    public async Task<SkillDto> UpdateAsync(
        Guid id,
        SkillWriteDto dto,
        Guid actorId,
        CancellationToken ct)
    {
        var skill = await _db.Skills
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (skill is null)
        {
            throw new ApiException(
                StatusCodes.Status404NotFound,
                "SKILL_NOT_FOUND",
                "Skill not found.");
        }

        if (skill.IsArchived)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "SKILL_ARCHIVED",
                "Archived skills cannot be updated.");
        }

        var name = dto.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "SKILL_NAME_REQUIRED",
                "Skill name is required.");
        }

        var duplicate = await _db.Skills
            .AnyAsync(
                x => x.Id != id &&
                     x.Name.ToLower() == name.ToLower(),
                ct);

        if (duplicate)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "SKILL_NAME_EXISTS",
                "A skill with this name already exists.");
        }

        skill.Name = name;
        skill.Description = string.IsNullOrWhiteSpace(dto.Description)
            ? null
            : dto.Description.Trim();
        skill.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        return ToDto(skill);
    }

    public async Task ArchiveAsync(
        Guid id,
        Guid actorId,
        CancellationToken ct)
    {
        var skill = await _db.Skills
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (skill is null)
        {
            throw new ApiException(
                StatusCodes.Status404NotFound,
                "SKILL_NOT_FOUND",
                "Skill not found.");
        }

        skill.IsArchived = true;
        skill.IsActive = false;
        skill.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    private static SkillDto ToDto(Skill skill)
    {
        return new SkillDto
        {
            Id = skill.Id,
            Name = skill.Name,
            Description = skill.Description,
            IsActive = skill.IsActive,
            IsArchived = skill.IsArchived,
            CreatedAt = skill.CreatedAt,
            UpdatedAt = skill.UpdatedAt
        };
    }
}