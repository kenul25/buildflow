using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Interfaces;
using BuildFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Services;

public sealed class EquipmentReservationService(BuildFlowDbContext db)
    : IEquipmentReservationService
{
    private static ApiException Bad(string code, string message)
        => new(400, code, message);

    private static ApiException NotFound(string code, string message)
        => new(404, code, message);

    private static ApiException Conflict(string code, string message)
        => new(409, code, message);

    public async Task<PageResult<EquipmentReservationDto>> ListAsync(
        PageQuery query,
        CancellationToken ct)
    {
        var rows = db.EquipmentReservations
            .AsNoTracking()
            .Include(x => x.Equipment)
            .Include(x => x.Activity)
            .Where(x => !x.IsArchived);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();

            rows = rows.Where(x =>
                x.Equipment.EquipmentCode.ToLower().Contains(search) ||
                x.Equipment.Name.ToLower().Contains(search) ||
                x.Activity.Name.ToLower().Contains(search) ||
                (x.Notes != null && x.Notes.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim();
            rows = rows.Where(x => x.Status == status);
        }

        var total = await rows.CountAsync(ct);

        rows = query.Sort?.ToLowerInvariant() switch
        {
            "equipment" => query.Desc
                ? rows.OrderByDescending(x => x.Equipment.Name)
                : rows.OrderBy(x => x.Equipment.Name),

            "starttime" => query.Desc
                ? rows.OrderByDescending(x => x.StartTime)
                : rows.OrderBy(x => x.StartTime),

            "endtime" => query.Desc
                ? rows.OrderByDescending(x => x.EndTime)
                : rows.OrderBy(x => x.EndTime),

            "status" => query.Desc
                ? rows.OrderByDescending(x => x.Status)
                : rows.OrderBy(x => x.Status),

            _ => query.Desc
                ? rows.OrderByDescending(x => x.CreatedAt)
                : rows.OrderBy(x => x.CreatedAt)
        };

        var items = await rows
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new EquipmentReservationDto(
                x.Id,
                x.EquipmentId,
                x.ActivityId,
                x.StartTime,
                x.EndTime,
                x.Status,
                x.Notes,
                x.IsArchived,
                x.CreatedAt,
                x.UpdatedAt))
            .ToListAsync(ct);

        return new(items, total, query.Page, query.PageSize);
    }

    public async Task<EquipmentReservationDto> GetAsync(
        Guid id,
        CancellationToken ct)
    {
        var item = await db.EquipmentReservations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw NotFound(
                "equipment_reservation_not_found",
                "The equipment reservation was not found.");

        return Map(item);
    }

    public async Task<EquipmentReservationDto> CreateAsync(
        EquipmentReservationWriteDto dto,
        Guid actorId,
        CancellationToken ct)
    {
        ValidateTime(dto);

        var equipment = await db.Equipment
            .FirstOrDefaultAsync(x => x.Id == dto.EquipmentId, ct)
            ?? throw NotFound(
                "equipment_not_found",
                "The equipment was not found.");

        if (!equipment.IsActive || equipment.IsArchived)
            throw Conflict(
                "equipment_unavailable",
                "Inactive or archived equipment cannot be reserved.");

        if (equipment.Status is "Maintenance" or "Unavailable")
            throw Conflict(
                "equipment_unavailable",
                "Equipment is currently unavailable.");

        var activity = await db.ConstructionActivities
            .FirstOrDefaultAsync(x => x.Id == dto.ActivityId, ct)
            ?? throw NotFound(
                "activity_not_found",
                "The activity was not found.");

        if (activity.IsArchived)
            throw Conflict(
                "activity_archived",
                "Archived activities cannot have equipment reservations.");

        var overlap = await db.EquipmentReservations.AnyAsync(x =>
            !x.IsArchived &&
            x.EquipmentId == dto.EquipmentId &&
            x.Status != "Cancelled" && x.Status != "Completed" &&
            x.StartTime < dto.EndTime &&
            x.EndTime > dto.StartTime, ct);

        if (overlap)
            throw Conflict(
                "equipment_double_booking",
                "The equipment is already reserved for the selected time period.");

        var reservation = new EquipmentReservation
        {
            Id = Guid.NewGuid(),
            EquipmentId = dto.EquipmentId,
            ActivityId = dto.ActivityId,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Status = string.IsNullOrWhiteSpace(dto.Status) ? "Planned" : dto.Status.Trim(),
            Notes = dto.Notes?.Trim()
        };

        ValidateStatus(reservation.Status);

        db.EquipmentReservations.Add(reservation);
        await db.SaveChangesAsync(ct);

        return Map(reservation);
    }

    public async Task<EquipmentReservationDto> UpdateAsync(
        Guid id,
        EquipmentReservationWriteDto dto,
        Guid actorId,
        CancellationToken ct)
    {
        ValidateTime(dto);

        var reservation = await db.EquipmentReservations
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw NotFound(
                "equipment_reservation_not_found",
                "The equipment reservation was not found.");

        if (reservation.IsArchived)
            throw Conflict(
                "archived",
                "Archived reservations cannot be edited.");

        var equipment = await db.Equipment
            .FirstOrDefaultAsync(x => x.Id == dto.EquipmentId, ct)
            ?? throw NotFound(
                "equipment_not_found",
                "The equipment was not found.");

        if (!equipment.IsActive || equipment.IsArchived)
            throw Conflict(
                "equipment_unavailable",
                "Inactive or archived equipment cannot be reserved.");

        if (equipment.Status is "Maintenance" or "Unavailable")
            throw Conflict(
                "equipment_unavailable",
                "Equipment is currently unavailable.");

        var activity = await db.ConstructionActivities
            .FirstOrDefaultAsync(x => x.Id == dto.ActivityId, ct)
            ?? throw NotFound(
                "activity_not_found",
                "The activity was not found.");

        if (activity.IsArchived)
            throw Conflict(
                "activity_archived",
                "Archived activities cannot have equipment reservations.");

        var overlap = await db.EquipmentReservations.AnyAsync(x =>
            x.Id != id &&
            !x.IsArchived &&
            x.EquipmentId == dto.EquipmentId &&
            x.Status != "Cancelled" && x.Status != "Completed" &&
            x.StartTime < dto.EndTime &&
            x.EndTime > dto.StartTime, ct);

        if (overlap)
            throw Conflict(
                "equipment_double_booking",
                "The equipment is already reserved for the selected time period.");

        reservation.EquipmentId = dto.EquipmentId;
        reservation.ActivityId = dto.ActivityId;
        reservation.StartTime = dto.StartTime;
        reservation.EndTime = dto.EndTime;
        reservation.Status = string.IsNullOrWhiteSpace(dto.Status)
            ? "Planned"
            : dto.Status.Trim();
        reservation.Notes = dto.Notes?.Trim();

        ValidateStatus(reservation.Status);

        await db.SaveChangesAsync(ct);

        return Map(reservation);
    }

    public async Task ArchiveAsync(
        Guid id,
        Guid actorId,
        CancellationToken ct)
    {
        var reservation = await db.EquipmentReservations
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw NotFound(
                "equipment_reservation_not_found",
                "The equipment reservation was not found.");

        if (reservation.IsArchived)
            return;

        reservation.IsArchived = true;
        reservation.Status = "Cancelled";

        await db.SaveChangesAsync(ct);
    }

    private static void ValidateTime(EquipmentReservationWriteDto dto)
    {
        if (dto.StartTime >= dto.EndTime)
            throw Bad(
                "equipment_reservation_time_invalid",
                "Start time must be before end time.");
    }

    private static void ValidateStatus(string status)
    {
        if (status is not ("Planned" or "Reserved" or "InUse" or "Completed" or "Cancelled"))
            throw Bad(
                "equipment_reservation_status_invalid",
                "Reservation status is invalid.");
    }

    private static EquipmentReservationDto Map(
        EquipmentReservation x)
        => new(
            x.Id,
            x.EquipmentId,
            x.ActivityId,
            x.StartTime,
            x.EndTime,
            x.Status,
            x.Notes,
            x.IsArchived,
            x.CreatedAt,
            x.UpdatedAt);
}

