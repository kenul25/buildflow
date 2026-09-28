using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Interfaces;
using BuildFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Services;

public sealed class WorkerAssignmentService : IWorkerAssignmentService
{
    private readonly BuildFlowDbContext _db;

    public WorkerAssignmentService(BuildFlowDbContext db)
    {
        _db = db;
    }

    public async Task<PageResult<WorkerAssignmentDto>> ListAsync(
        PageQuery query,
        CancellationToken ct)
    {
        var assignments = _db.WorkerAssignments
            .AsNoTracking()
            .Include(x => x.Worker)
            .Include(x => x.Activity)
            .Where(x => !x.IsArchived)
            .AsQueryable();

        if (query.IncludeArchived)
        {
            assignments = _db.WorkerAssignments
                .AsNoTracking()
                .Include(x => x.Worker)
                .Include(x => x.Activity)
                .AsQueryable();
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            assignments = assignments.Where(x =>
                x.Worker.EmployeeCode.Contains(search) ||
                x.Worker.FullName.Contains(search) ||
                x.Status.Contains(search) ||
                (x.Notes != null && x.Notes.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim();

            assignments = assignments.Where(x =>
                x.Status.ToLower() == status.ToLower());
        }

        var total = await assignments.CountAsync(ct);

        assignments = query.Sort?.ToLower() switch
        {
            "worker" => query.Desc
                ? assignments.OrderByDescending(x => x.Worker.FullName)
                : assignments.OrderBy(x => x.Worker.FullName),

            "starttime" => query.Desc
                ? assignments.OrderByDescending(x => x.StartTime)
                : assignments.OrderBy(x => x.StartTime),

            "endtime" => query.Desc
                ? assignments.OrderByDescending(x => x.EndTime)
                : assignments.OrderBy(x => x.EndTime),

            "status" => query.Desc
                ? assignments.OrderByDescending(x => x.Status)
                : assignments.OrderBy(x => x.Status),

            "createdat" => query.Desc
                ? assignments.OrderByDescending(x => x.CreatedAt)
                : assignments.OrderBy(x => x.CreatedAt),

            _ => assignments.OrderBy(x => x.StartTime)
        };

        var page = query.Page < 1 ? 1 : query.Page;

        var pageSize = query.PageSize <= 0
            ? 20
            : Math.Min(query.PageSize, 100);

        var items = await assignments
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new WorkerAssignmentDto
            {
                Id = x.Id,
                WorkerId = x.WorkerId,
                ActivityId = x.ActivityId,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                Status = x.Status,
                Notes = x.Notes,
                IsArchived = x.IsArchived,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return new PageResult<WorkerAssignmentDto>(
            items,
            total,
            page,
            pageSize);
    }

    public async Task<WorkerAssignmentDto> GetAsync(
        Guid id,
        CancellationToken ct)
    {
        var assignment = await _db.WorkerAssignments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (assignment is null)
        {
            throw new ApiException(
                StatusCodes.Status404NotFound,
                "WORKER_ASSIGNMENT_NOT_FOUND",
                "Worker assignment not found.");
        }

        return ToDto(assignment);
    }

    public async Task<WorkerAssignmentDto> CreateAsync(
        WorkerAssignmentWriteDto dto,
        Guid actorId,
        CancellationToken ct)
    {
        ValidateTimes(dto);

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
                "Inactive or archived workers cannot be assigned.");
        }

        var activity = await _db.ConstructionActivities
            .FirstOrDefaultAsync(x => x.Id == dto.ActivityId, ct);

        if (activity is null)
        {
            throw new ApiException(
                StatusCodes.Status404NotFound,
                "ACTIVITY_NOT_FOUND",
                "Construction activity not found.");
        }

        var overlap = await _db.WorkerAssignments.AnyAsync(
            x => !x.IsArchived &&
                 x.WorkerId == dto.WorkerId &&
                 x.StartTime < dto.EndTime &&
                 x.EndTime > dto.StartTime,
            ct);

        if (overlap)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "WORKER_ASSIGNMENT_OVERLAP",
                "The worker already has an overlapping assignment.");
        }

        var assignment = new WorkerAssignment
        {
            Id = Guid.NewGuid(),
            WorkerId = dto.WorkerId,
            ActivityId = dto.ActivityId,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Status = string.IsNullOrWhiteSpace(dto.Status)
                ? "Planned"
                : dto.Status.Trim(),
            Notes = string.IsNullOrWhiteSpace(dto.Notes)
                ? null
                : dto.Notes.Trim(),
            IsArchived = false
        };

        _db.WorkerAssignments.Add(assignment);

        await _db.SaveChangesAsync(ct);

        return ToDto(assignment);
    }

    public async Task<WorkerAssignmentDto> UpdateAsync(
        Guid id,
        WorkerAssignmentWriteDto dto,
        Guid actorId,
        CancellationToken ct)
    {
        ValidateTimes(dto);

        var assignment = await _db.WorkerAssignments
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (assignment is null)
        {
            throw new ApiException(
                StatusCodes.Status404NotFound,
                "WORKER_ASSIGNMENT_NOT_FOUND",
                "Worker assignment not found.");
        }

        if (assignment.IsArchived)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "WORKER_ASSIGNMENT_ARCHIVED",
                "Archived assignments cannot be updated.");
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
                "Inactive or archived workers cannot be assigned.");
        }

        var activity = await _db.ConstructionActivities
            .FirstOrDefaultAsync(x => x.Id == dto.ActivityId, ct);

        if (activity is null)
        {
            throw new ApiException(
                StatusCodes.Status404NotFound,
                "ACTIVITY_NOT_FOUND",
                "Construction activity not found.");
        }

        var overlap = await _db.WorkerAssignments.AnyAsync(
            x => !x.IsArchived &&
                 x.Id != id &&
                 x.WorkerId == dto.WorkerId &&
                 x.StartTime < dto.EndTime &&
                 x.EndTime > dto.StartTime,
            ct);

        if (overlap)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "WORKER_ASSIGNMENT_OVERLAP",
                "The worker already has an overlapping assignment.");
        }

        assignment.WorkerId = dto.WorkerId;
        assignment.ActivityId = dto.ActivityId;
        assignment.StartTime = dto.StartTime;
        assignment.EndTime = dto.EndTime;
        assignment.Status = string.IsNullOrWhiteSpace(dto.Status)
            ? "Planned"
            : dto.Status.Trim();
        assignment.Notes = string.IsNullOrWhiteSpace(dto.Notes)
            ? null
            : dto.Notes.Trim();
        assignment.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        return ToDto(assignment);
    }

    public async Task ArchiveAsync(
        Guid id,
        Guid actorId,
        CancellationToken ct)
    {
        var assignment = await _db.WorkerAssignments
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (assignment is null)
        {
            throw new ApiException(
                StatusCodes.Status404NotFound,
                "WORKER_ASSIGNMENT_NOT_FOUND",
                "Worker assignment not found.");
        }

        assignment.IsArchived = true;
        assignment.Status = "Completed";
        assignment.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    private static void ValidateTimes(WorkerAssignmentWriteDto dto)
    {
        if (dto.StartTime >= dto.EndTime)
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "WORKER_ASSIGNMENT_TIME_INVALID",
                "Start time must be earlier than end time.");
        }
    }

    private static WorkerAssignmentDto ToDto(
        WorkerAssignment assignment)
    {
        return new WorkerAssignmentDto
        {
            Id = assignment.Id,
            WorkerId = assignment.WorkerId,
            ActivityId = assignment.ActivityId,
            StartTime = assignment.StartTime,
            EndTime = assignment.EndTime,
            Status = assignment.Status,
            Notes = assignment.Notes,
            IsArchived = assignment.IsArchived,
            CreatedAt = assignment.CreatedAt,
            UpdatedAt = assignment.UpdatedAt
        };
    }
}