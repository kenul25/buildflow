using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Interfaces;
using BuildFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Services;

public sealed class WorkerService : IWorkerService
{
    private readonly BuildFlowDbContext _db;

    public WorkerService(BuildFlowDbContext db)
    {
        _db = db;
    }

    public async Task<PageResult<WorkerDto>> ListAsync(
        PageQuery query,
        CancellationToken ct)
    {
        var workers = _db.Workers
            .AsNoTracking()
            .AsQueryable();

        if (!query.IncludeArchived)
        {
            workers = workers.Where(x => !x.IsArchived);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            workers = workers.Where(x =>
                x.EmployeeCode.Contains(search) ||
                x.FullName.Contains(search) ||
                x.Role.Contains(search));
        }

        var total = await workers.CountAsync(ct);

        workers = query.Sort?.ToLower() switch
        {
            "name" => query.Desc
                ? workers.OrderByDescending(x => x.FullName)
                : workers.OrderBy(x => x.FullName),

            "role" => query.Desc
                ? workers.OrderByDescending(x => x.Role)
                : workers.OrderBy(x => x.Role),

            "createdat" => query.Desc
                ? workers.OrderByDescending(x => x.CreatedAt)
                : workers.OrderBy(x => x.CreatedAt),

            _ => workers.OrderBy(x => x.FullName)
        };

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0
            ? 20
            : Math.Min(query.PageSize, 100);

        var items = await workers
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new WorkerDto
            {
                Id = x.Id,
                EmployeeCode = x.EmployeeCode,
                FullName = x.FullName,
                PhoneNumber = x.PhoneNumber,
                Role = x.Role,
                IsActive = x.IsActive,
                IsArchived = x.IsArchived,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return new PageResult<WorkerDto>(
            items,
            total,
            page,
            pageSize);
    }

    public async Task<WorkerDto> GetAsync(
        Guid id,
        CancellationToken ct)
    {
        var worker = await _db.Workers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (worker is null)
        {
            throw new ApiException(
                StatusCodes.Status404NotFound,
                "WORKER_NOT_FOUND",
                "Worker not found.");
        }

        return ToDto(worker);
    }

    public async Task<WorkerDto> CreateAsync(
        WorkerWriteDto dto,
        Guid actorId,
        CancellationToken ct)
    {
        var employeeCode = dto.EmployeeCode.Trim();

        var exists = await _db.Workers
            .AnyAsync(x => x.EmployeeCode == employeeCode, ct);

        if (exists)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "WORKER_CODE_EXISTS",
                "Employee code already exists.");
        }

        var worker = new Worker
        {
            Id = Guid.NewGuid(),
            EmployeeCode = employeeCode,
            FullName = dto.FullName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber)
                ? null
                : dto.PhoneNumber.Trim(),
            Role = dto.Role.Trim(),
            IsActive = true,
            IsArchived = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _db.Workers.Add(worker);

        await _db.SaveChangesAsync(ct);

        return ToDto(worker);
    }

    public async Task<WorkerDto> UpdateAsync(
        Guid id,
        WorkerWriteDto dto,
        Guid actorId,
        CancellationToken ct)
    {
        var worker = await _db.Workers
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (worker is null)
        {
            throw new ApiException(
                StatusCodes.Status404NotFound,
                "WORKER_NOT_FOUND",
                "Worker not found.");
        }

        var employeeCode = dto.EmployeeCode.Trim();

        var duplicate = await _db.Workers
            .AnyAsync(
                x => x.Id != id &&
                     x.EmployeeCode == employeeCode,
                ct);

        if (duplicate)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "WORKER_CODE_EXISTS",
                "Employee code already exists.");
        }

        worker.EmployeeCode = employeeCode;
        worker.FullName = dto.FullName.Trim();
        worker.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber)
            ? null
            : dto.PhoneNumber.Trim();
        worker.Role = dto.Role.Trim();
        worker.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        return ToDto(worker);
    }

    public async Task ArchiveAsync(
        Guid id,
        Guid actorId,
        CancellationToken ct)
    {
        var worker = await _db.Workers
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (worker is null)
        {
            throw new ApiException(
                StatusCodes.Status404NotFound,
                "WORKER_NOT_FOUND",
                "Worker not found.");
        }

        worker.IsArchived = true;
        worker.IsActive = false;
        worker.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    private static WorkerDto ToDto(Worker worker)
    {
        return new WorkerDto
        {
            Id = worker.Id,
            EmployeeCode = worker.EmployeeCode,
            FullName = worker.FullName,
            PhoneNumber = worker.PhoneNumber,
            Role = worker.Role,
            IsActive = worker.IsActive,
            IsArchived = worker.IsArchived,
            CreatedAt = worker.CreatedAt,
            UpdatedAt = worker.UpdatedAt
        };
    }
}