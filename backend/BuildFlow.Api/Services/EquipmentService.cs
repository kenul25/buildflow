using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Interfaces;
using BuildFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Services;

public sealed class EquipmentService : IEquipmentService
{
    private readonly BuildFlowDbContext _db;

    private static readonly string[] AllowedStatuses =
    {
        "Available",
        "InUse",
        "Maintenance",
        "Unavailable"
    };

    public EquipmentService(BuildFlowDbContext db)
    {
        _db = db;
    }

    public async Task<PageResult<EquipmentDto>> ListAsync(
        PageQuery query,
        CancellationToken ct)
    {
        var equipment = _db.Equipment
            .AsNoTracking()
            .AsQueryable();

        if (!query.IncludeArchived)
        {
            equipment = equipment.Where(x => !x.IsArchived);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            equipment = equipment.Where(x =>
                x.EquipmentCode.Contains(search) ||
                x.Name.Contains(search) ||
                x.Type.Contains(search) ||
                x.Status.Contains(search) ||
                (x.Description != null && x.Description.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim();

            equipment = equipment.Where(x =>
                x.Status.ToLower() == status.ToLower());
        }

        var total = await equipment.CountAsync(ct);

        equipment = query.Sort?.ToLower() switch
        {
            "code" => query.Desc
                ? equipment.OrderByDescending(x => x.EquipmentCode)
                : equipment.OrderBy(x => x.EquipmentCode),

            "name" => query.Desc
                ? equipment.OrderByDescending(x => x.Name)
                : equipment.OrderBy(x => x.Name),

            "type" => query.Desc
                ? equipment.OrderByDescending(x => x.Type)
                : equipment.OrderBy(x => x.Type),

            "status" => query.Desc
                ? equipment.OrderByDescending(x => x.Status)
                : equipment.OrderBy(x => x.Status),

            "createdat" => query.Desc
                ? equipment.OrderByDescending(x => x.CreatedAt)
                : equipment.OrderBy(x => x.CreatedAt),

            _ => equipment.OrderBy(x => x.Name)
        };

        var page = query.Page < 1 ? 1 : query.Page;

        var pageSize = query.PageSize <= 0
            ? 20
            : Math.Min(query.PageSize, 100);

        var items = await equipment
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new EquipmentDto
            {
                Id = x.Id,
                EquipmentCode = x.EquipmentCode,
                Name = x.Name,
                Type = x.Type,
                Status = x.Status,
                Description = x.Description,
                IsActive = x.IsActive,
                IsArchived = x.IsArchived,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return new PageResult<EquipmentDto>(
            items,
            total,
            page,
            pageSize);
    }

    public async Task<EquipmentDto> GetAsync(
        Guid id,
        CancellationToken ct)
    {
        var equipment = await _db.Equipment
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (equipment is null)
        {
            throw new ApiException(
                StatusCodes.Status404NotFound,
                "EQUIPMENT_NOT_FOUND",
                "Equipment not found.");
        }

        return ToDto(equipment);
    }

    public async Task<EquipmentDto> CreateAsync(
        EquipmentWriteDto dto,
        Guid actorId,
        CancellationToken ct)
    {
        var code = dto.EquipmentCode.Trim();
        var name = dto.Name.Trim();
        var type = dto.Type.Trim();
        var status = NormalizeStatus(dto.Status);

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "EQUIPMENT_CODE_REQUIRED",
                "Equipment code is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "EQUIPMENT_NAME_REQUIRED",
                "Equipment name is required.");
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "EQUIPMENT_TYPE_REQUIRED",
                "Equipment type is required.");
        }

        ValidateStatus(status);

        var exists = await _db.Equipment.AnyAsync(
            x => x.EquipmentCode.ToLower() == code.ToLower(),
            ct);

        if (exists)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "EQUIPMENT_CODE_EXISTS",
                "An equipment with this code already exists.");
        }

        var equipment = new Equipment
        {
            Id = Guid.NewGuid(),
            EquipmentCode = code,
            Name = name,
            Type = type,
            Status = status,
            Description = string.IsNullOrWhiteSpace(dto.Description)
                ? null
                : dto.Description.Trim(),
            IsActive = true,
            IsArchived = false
        };

        _db.Equipment.Add(equipment);

        await _db.SaveChangesAsync(ct);

        return ToDto(equipment);
    }

    public async Task<EquipmentDto> UpdateAsync(
        Guid id,
        EquipmentWriteDto dto,
        Guid actorId,
        CancellationToken ct)
    {
        var equipment = await _db.Equipment
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (equipment is null)
        {
            throw new ApiException(
                StatusCodes.Status404NotFound,
                "EQUIPMENT_NOT_FOUND",
                "Equipment not found.");
        }

        if (equipment.IsArchived)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "EQUIPMENT_ARCHIVED",
                "Archived equipment cannot be updated.");
        }

        var code = dto.EquipmentCode.Trim();
        var name = dto.Name.Trim();
        var type = dto.Type.Trim();
        var status = NormalizeStatus(dto.Status);

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "EQUIPMENT_CODE_REQUIRED",
                "Equipment code is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "EQUIPMENT_NAME_REQUIRED",
                "Equipment name is required.");
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "EQUIPMENT_TYPE_REQUIRED",
                "Equipment type is required.");
        }

        ValidateStatus(status);

        var duplicate = await _db.Equipment.AnyAsync(
            x => x.Id != id &&
                 x.EquipmentCode.ToLower() == code.ToLower(),
            ct);

        if (duplicate)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "EQUIPMENT_CODE_EXISTS",
                "An equipment with this code already exists.");
        }

        equipment.EquipmentCode = code;
        equipment.Name = name;
        equipment.Type = type;
        equipment.Status = status;
        equipment.Description = string.IsNullOrWhiteSpace(dto.Description)
            ? null
            : dto.Description.Trim();
        equipment.IsActive = !string.Equals(
            status,
            "Unavailable",
            StringComparison.OrdinalIgnoreCase);
        equipment.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        return ToDto(equipment);
    }

    public async Task ArchiveAsync(
        Guid id,
        Guid actorId,
        CancellationToken ct)
    {
        var equipment = await _db.Equipment
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (equipment is null)
        {
            throw new ApiException(
                StatusCodes.Status404NotFound,
                "EQUIPMENT_NOT_FOUND",
                "Equipment not found.");
        }

        equipment.IsArchived = true;
        equipment.IsActive = false;
        equipment.Status = "Unavailable";
        equipment.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    private static string NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return "Available";

        var value = status.Trim();

        return AllowedStatuses.FirstOrDefault(
            x => x.Equals(value, StringComparison.OrdinalIgnoreCase))
            ?? value;
    }

    private static void ValidateStatus(string status)
    {
        if (!AllowedStatuses.Any(
                x => x.Equals(status, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "EQUIPMENT_STATUS_INVALID",
                "Equipment status must be Available, InUse, Maintenance, or Unavailable.");
        }
    }

    private static EquipmentDto ToDto(Equipment equipment)
    {
        return new EquipmentDto
        {
            Id = equipment.Id,
            EquipmentCode = equipment.EquipmentCode,
            Name = equipment.Name,
            Type = equipment.Type,
            Status = equipment.Status,
            Description = equipment.Description,
            IsActive = equipment.IsActive,
            IsArchived = equipment.IsArchived,
            CreatedAt = equipment.CreatedAt,
            UpdatedAt = equipment.UpdatedAt
        };
    }
}