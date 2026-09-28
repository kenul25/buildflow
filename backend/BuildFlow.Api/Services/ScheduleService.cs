using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Interfaces;
using BuildFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Services;

public sealed class ScheduleService(BuildFlowDbContext db) : IScheduleService
{
    private static ApiException Bad(string code, string message)
        => new(400, code, message);

    private static ApiException NotFound(string code, string message)
        => new(404, code, message);

    private static ApiException Conflict(string code, string message)
        => new(409, code, message);

    public async Task<PageResult<ScheduleDto>> ListAsync(
        PageQuery query,
        CancellationToken ct)
    {
        var rows = db.Schedules
            .AsNoTracking()
            .Where(x => query.IncludeArchived || !x.IsArchived);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            rows = rows.Where(x =>
                x.Status.Contains(search) ||
                x.ApprovalStatus.Contains(search) ||
                (x.Notes != null && x.Notes.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim();
            rows = rows.Where(x => x.Status == status);
        }

        var total = await rows.CountAsync(ct);

        rows = query.Sort?.ToLowerInvariant() switch
        {
            "starttime" => query.Desc
                ? rows.OrderByDescending(x => x.StartTime)
                : rows.OrderBy(x => x.StartTime),

            "endtime" => query.Desc
                ? rows.OrderByDescending(x => x.EndTime)
                : rows.OrderBy(x => x.EndTime),

            "status" => query.Desc
                ? rows.OrderByDescending(x => x.Status)
                : rows.OrderBy(x => x.Status),

            "approvalstatus" => query.Desc
                ? rows.OrderByDescending(x => x.ApprovalStatus)
                : rows.OrderBy(x => x.ApprovalStatus),

            _ => query.Desc
                ? rows.OrderByDescending(x => x.CreatedAt)
                : rows.OrderBy(x => x.CreatedAt)
        };

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0
            ? 20
            : Math.Min(query.PageSize, 100);

        var items = await rows
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ScheduleDto(
                x.Id,
                x.ActivityId,
                x.StartTime,
                x.EndTime,
                x.Status,
                x.ApprovalStatus,
                x.Notes,
                x.IsArchived,
                x.CreatedAt,
                x.UpdatedAt))
            .ToListAsync(ct);

        return new PageResult<ScheduleDto>(
            items,
            total,
            page,
            pageSize);
    }

    public async Task<ScheduleDto> GetAsync(
        Guid id,
        CancellationToken ct)
    {
        var schedule = await db.Schedules
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw NotFound(
                "schedule_not_found",
                "The schedule was not found.");

        return Map(schedule);
    }

    public async Task<ScheduleDto> CreateAsync(
        ScheduleWriteDto dto,
        Guid actorId,
        CancellationToken ct)
    {
        ValidateTime(dto.StartTime, dto.EndTime);
        ValidateStatus(dto.Status);
        ValidateApprovalStatus(dto.ApprovalStatus);

        var activity = await db.ConstructionActivities
            .FirstOrDefaultAsync(x => x.Id == dto.ActivityId, ct)
            ?? throw NotFound(
                "activity_not_found",
                "The activity was not found.");

        if (activity.IsArchived)
            throw Conflict(
                "activity_archived",
                "Archived activities cannot have schedules.");

        var overlap = await db.Schedules.AnyAsync(x =>
            !x.IsArchived &&
            x.ActivityId == dto.ActivityId &&
            x.Status != "Cancelled" &&
            x.Status != "Completed" &&
            x.StartTime < dto.EndTime &&
            x.EndTime > dto.StartTime, ct);

        if (overlap)
            throw Conflict(
                "schedule_overlap",
                "The activity already has an overlapping schedule.");

        var schedule = new Schedule
        {
            Id = Guid.NewGuid(),
            ActivityId = dto.ActivityId,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Status = string.IsNullOrWhiteSpace(dto.Status)
                ? "Proposed"
                : dto.Status.Trim(),
            ApprovalStatus = "PendingProjectManagerApproval",
            Notes = dto.Notes?.Trim()
        };

        db.Schedules.Add(schedule);
        await db.SaveChangesAsync(ct);

        return Map(schedule);
    }

    public async Task<ScheduleDto> UpdateAsync(
        Guid id,
        ScheduleWriteDto dto,
        Guid actorId,
        CancellationToken ct)
    {
        ValidateTime(dto.StartTime, dto.EndTime);
        ValidateStatus(dto.Status);

        var schedule = await db.Schedules
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw NotFound(
                "schedule_not_found",
                "The schedule was not found.");

        if (schedule.IsArchived)
            throw Conflict(
                "archived",
                "Archived schedules cannot be edited.");

        var activity = await db.ConstructionActivities
            .FirstOrDefaultAsync(x => x.Id == dto.ActivityId, ct)
            ?? throw NotFound(
                "activity_not_found",
                "The activity was not found.");

        if (activity.IsArchived)
            throw Conflict(
                "activity_archived",
                "Archived activities cannot have schedules.");

        var overlap = await db.Schedules.AnyAsync(x =>
            x.Id != id &&
            !x.IsArchived &&
            x.ActivityId == dto.ActivityId &&
            x.Status != "Cancelled" &&
            x.Status != "Completed" &&
            x.StartTime < dto.EndTime &&
            x.EndTime > dto.StartTime, ct);

        if (overlap)
            throw Conflict(
                "schedule_overlap",
                "The activity already has an overlapping schedule.");

        schedule.ActivityId = dto.ActivityId;
        schedule.StartTime = dto.StartTime;
        schedule.EndTime = dto.EndTime;
        schedule.Status = string.IsNullOrWhiteSpace(dto.Status)
            ? "Proposed"
            : dto.Status.Trim();
        schedule.Notes = dto.Notes?.Trim();

        await db.SaveChangesAsync(ct);

        return Map(schedule);
    }

    public async Task ArchiveAsync(
        Guid id,
        Guid actorId,
        CancellationToken ct)
    {
        var schedule = await db.Schedules
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw NotFound(
                "schedule_not_found",
                "The schedule was not found.");

        if (schedule.IsArchived)
            return;

        schedule.IsArchived = true;
        schedule.Status = "Cancelled";

        await db.SaveChangesAsync(ct);
    }

    private static void ValidateTime(
        DateTimeOffset startTime,
        DateTimeOffset endTime)
    {
        if (startTime >= endTime)
            throw Bad(
                "schedule_time_invalid",
                "Start time must be before end time.");
    }

    private static void ValidateStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return;

        if (status is not ("Proposed" or "Scheduled" or "InProgress" or "Completed" or "Cancelled"))
            throw Bad(
                "schedule_status_invalid",
                "Schedule status is invalid.");
    }

    private static void ValidateApprovalStatus(string? approvalStatus)
    {
        if (string.IsNullOrWhiteSpace(approvalStatus))
            return;

        if (approvalStatus is not (
            "PendingProjectManagerApproval" or
            "Approved" or
            "Rejected" or
            "RevisionRequested"))
            throw Bad(
                "approval_status_invalid",
                "Approval status is invalid.");
    }

    private static ScheduleDto Map(Schedule x)
        => new(
            x.Id,
            x.ActivityId,
            x.StartTime,
            x.EndTime,
            x.Status,
            x.ApprovalStatus,
            x.Notes,
            x.IsArchived,
            x.CreatedAt,
            x.UpdatedAt);
}
