using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Interfaces;
using BuildFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Services;

public sealed class ShiftService : IShiftService
{
    private readonly BuildFlowDbContext _db;

    public ShiftService(BuildFlowDbContext db)
    {
        _db = db;
    }

    public async Task<PageResult<ShiftDto>> ListAsync(
        PageQuery query,
        CancellationToken ct)
    {
        var shifts = _db.Shifts
            .AsNoTracking()
            .AsQueryable();

        if (!query.IncludeArchived)
        {
            shifts = shifts.Where(x => !x.IsArchived);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            shifts = shifts.Where(x =>
                x.Name.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            shifts = query.Status.ToLower() switch
            {
                "active" => shifts.Where(x => x.IsActive && !x.IsArchived),
                "inactive" => shifts.Where(x => !x.IsActive && !x.IsArchived),
                "archived" => shifts.Where(x => x.IsArchived),
                _ => shifts
            };
        }

        var total = await shifts.CountAsync(ct);

        shifts = query.Sort?.ToLower() switch
        {
            "name" => query.Desc
                ? shifts.OrderByDescending(x => x.Name)
                : shifts.OrderBy(x => x.Name),

            "starttime" => query.Desc
                ? shifts.OrderByDescending(x => x.StartTime)
                : shifts.OrderBy(x => x.StartTime),

            "endtime" => query.Desc
                ? shifts.OrderByDescending(x => x.EndTime)
                : shifts.OrderBy(x => x.EndTime),

            "createdat" => query.Desc
                ? shifts.OrderByDescending(x => x.CreatedAt)
                : shifts.OrderBy(x => x.CreatedAt),

            _ => shifts.OrderBy(x => x.Name)
        };

        var page = query.Page < 1 ? 1 : query.Page;

        var pageSize = query.PageSize <= 0
            ? 20
            : Math.Min(query.PageSize, 100);

        var items = await shifts
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ShiftDto
            {
                Id = x.Id,
                Name = x.Name,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                IsActive = x.IsActive,
                IsArchived = x.IsArchived,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return new PageResult<ShiftDto>(
            items,
            total,
            page,
            pageSize);
    }

    public async Task<ShiftDto> GetAsync(
        Guid id,
        CancellationToken ct)
    {
        var shift = await _db.Shifts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (shift is null)
        {
            throw new ApiException(
                StatusCodes.Status404NotFound,
                "SHIFT_NOT_FOUND",
                "Shift not found.");
        }

        return ToDto(shift);
    }

    public async Task<ShiftDto> CreateAsync(
        ShiftWriteDto dto,
        Guid actorId,
        CancellationToken ct)
    {
        ValidateTimes(dto);

        var name = dto.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "SHIFT_NAME_REQUIRED",
                "Shift name is required.");
        }

        var exists = await _db.Shifts.AnyAsync(
            x => x.Name.ToLower() == name.ToLower(),
            ct);

        if (exists)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "SHIFT_NAME_EXISTS",
                "A shift with this name already exists.");
        }

        var shift = new Shift
        {
            Id = Guid.NewGuid(),
            Name = name,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            IsActive = true,
            IsArchived = false
        };

        _db.Shifts.Add(shift);

        await _db.SaveChangesAsync(ct);

        return ToDto(shift);
    }

    public async Task<ShiftDto> UpdateAsync(
        Guid id,
        ShiftWriteDto dto,
        Guid actorId,
        CancellationToken ct)
    {
        ValidateTimes(dto);

        var shift = await _db.Shifts
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (shift is null)
        {
            throw new ApiException(
                StatusCodes.Status404NotFound,
                "SHIFT_NOT_FOUND",
                "Shift not found.");
        }

        if (shift.IsArchived)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "SHIFT_ARCHIVED",
                "Archived shifts cannot be updated.");
        }

        var name = dto.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "SHIFT_NAME_REQUIRED",
                "Shift name is required.");
        }

        var duplicate = await _db.Shifts.AnyAsync(
            x => x.Id != id &&
                 x.Name.ToLower() == name.ToLower(),
            ct);

        if (duplicate)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "SHIFT_NAME_EXISTS",
                "A shift with this name already exists.");
        }

        shift.Name = name;
        shift.StartTime = dto.StartTime;
        shift.EndTime = dto.EndTime;
        shift.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        return ToDto(shift);
    }

    public async Task ArchiveAsync(
        Guid id,
        Guid actorId,
        CancellationToken ct)
    {
        var shift = await _db.Shifts
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (shift is null)
        {
            throw new ApiException(
                StatusCodes.Status404NotFound,
                "SHIFT_NOT_FOUND",
                "Shift not found.");
        }

        shift.IsArchived = true;
        shift.IsActive = false;
        shift.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    private static void ValidateTimes(ShiftWriteDto dto)
    {
        if (dto.StartTime == dto.EndTime)
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "SHIFT_TIME_INVALID",
                "Shift start time and end time cannot be the same.");
        }
    }

    private static ShiftDto ToDto(Shift shift)
    {
        return new ShiftDto
        {
            Id = shift.Id,
            Name = shift.Name,
            StartTime = shift.StartTime,
            EndTime = shift.EndTime,
            IsActive = shift.IsActive,
            IsArchived = shift.IsArchived,
            CreatedAt = shift.CreatedAt,
            UpdatedAt = shift.UpdatedAt
        };
    }
}