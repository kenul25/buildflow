using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Services;

public sealed class SchedulingService(BuildFlowDbContext db)
{
    public static bool Overlaps(DateTimeOffset a, DateTimeOffset b, DateTimeOffset c, DateTimeOffset d) => a < d && c < b;
    public static void ValidateWindow(DateTimeOffset start, DateTimeOffset end)
    {
        if (end <= start) throw Bad("End time must be after start time.");
    }
    public static ApiException Bad(string message) => new(400, "invalid_schedule", message);
    public static ApiException Conflict(string message) => new(409, "schedule_conflict", message);

    // One transaction-scoped PostgreSQL lock covers schedules, shifts, skills and bookings.
    // This also protects cross-table predicates which a row lock alone cannot serialize.
    public Task LockAsync(CancellationToken ct) => db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(42004001)", ct);

    public async Task<bool> CanAccessActivityAsync(Guid activityId, Guid actor, bool manager, CancellationToken ct) =>
        await db.ConstructionActivities.AnyAsync(x => x.Id == activityId && !x.IsArchived && !x.Phase.IsArchived && !x.Phase.Site.IsArchived && !x.Phase.Site.Project.IsArchived && (manager || x.Phase.Site.Project.AssignedEngineerId == actor), ct);

    private IQueryable<T> Visible<T>(Guid actor, bool manager) where T : SchedulingRecord
    {
        var rows = db.Set<T>().AsQueryable();
        if (manager) return rows;
        var activities = db.ConstructionActivities.Where(x => !x.IsArchived && !x.Phase.IsArchived && !x.Phase.Site.IsArchived && !x.Phase.Site.Project.IsArchived && x.Phase.Site.Project.AssignedEngineerId == actor).Select(x => x.Id);
        var schedules = db.Set<WorkSchedule>().Where(x => activities.Contains(x.ActivityId)).Select(x => x.Id);
        if (typeof(T) == typeof(WorkSchedule) || typeof(T) == typeof(EquipmentRequest) || typeof(T) == typeof(SiteIssue))
            rows = rows.Where(x => activities.Contains(EF.Property<Guid>(x, "ActivityId")));
        if (typeof(T) == typeof(WorkerAssignment) || typeof(T) == typeof(EquipmentReservation))
            rows = rows.Where(x => schedules.Contains(EF.Property<Guid>(x, "ScheduleId")));
        return rows;
    }

    public async Task<PageResult<T>> ListAsync<T>(SchedulingQuery query, Guid actor, bool manager, CancellationToken ct) where T : SchedulingRecord
    {
        if (query.Page < 1 || query.PageSize is < 1 or > 100) throw Bad("Invalid page or page size.");
        var rows = Visible<T>(actor, manager).AsNoTracking();
        if (!query.IncludeArchived) rows = rows.Where(x => !x.IsArchived);
        if (!string.IsNullOrWhiteSpace(query.Search)) rows = rows.Where(x => EF.Functions.ILike(x.Name, $"%{query.Search.Trim()}%"));
        if (!string.IsNullOrEmpty(query.Status) && typeof(T).GetProperty("Status") != null) rows = rows.Where(x => EF.Property<string>(x, "Status") == query.Status);
        if (query.ParentId is Guid parent)
        {
            var property = typeof(T) == typeof(WorkerSkill) || typeof(T) == typeof(Shift) ? "WorkerId" : typeof(T) == typeof(WorkerAssignment) || typeof(T) == typeof(EquipmentReservation) ? "ScheduleId" : "ActivityId";
            if (typeof(T).GetProperty(property) != null) rows = rows.Where(x => EF.Property<Guid>(x, property) == parent);
        }
        var total = await rows.CountAsync(ct);
        rows = query.Sort?.ToLowerInvariant() switch
        {
            "updatedat" => query.Desc ? rows.OrderByDescending(x => x.UpdatedAt) : rows.OrderBy(x => x.UpdatedAt),
            "starttime" when typeof(T).GetProperty("StartTime") != null => query.Desc ? rows.OrderByDescending(x => EF.Property<DateTimeOffset>(x, "StartTime")) : rows.OrderBy(x => EF.Property<DateTimeOffset>(x, "StartTime")),
            _ => query.Desc ? rows.OrderByDescending(x => x.Name) : rows.OrderBy(x => x.Name)
        };
        return new(await rows.ThenStable().Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct), total, query.Page, query.PageSize);
    }

    public async Task<T> GetAsync<T>(Guid id, Guid actor, bool manager, CancellationToken ct) where T : SchedulingRecord =>
        await Visible<T>(actor, manager).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ApiException(404, "not_found", "Record not found.");

    public async Task<T> WriteAsync<T>(Guid? id, SchedulingWriteDto dto, Guid actor, bool manager, CancellationToken ct) where T : SchedulingRecord, new()
    {
        if (!manager && typeof(T) != typeof(EquipmentRequest) && typeof(T) != typeof(SiteIssue)) throw new ApiException(403, "forbidden", "Management permission is required.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockAsync(ct);
        var record = id is Guid value ? await Visible<T>(actor, manager).SingleOrDefaultAsync(x => x.Id == value, ct) ?? throw new ApiException(404, "not_found", "Record not found.") : new T { Id = Guid.NewGuid(), CreatedById = actor };
        if (record.IsArchived) throw Conflict("Archived records cannot be edited.");
        if (record is WorkSchedule { Status: "Completed" } or WorkerAssignment { Status: "Completed" } or EquipmentReservation { Status: "Returned" }) throw Conflict("Completed history cannot be edited.");
        record.Name = dto.Name.Trim(); record.Notes = dto.Notes?.Trim(); record.UpdatedById = actor;
        await ApplyAsync(record, dto, actor, manager, ct);
        if (id == null) db.Add(record);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return record;
    }

    private static Guid Required(Guid? id, string label) => id is Guid value && value != Guid.Empty ? value : throw Bad($"{label} is required.");
    private async Task ApplyAsync(SchedulingRecord record, SchedulingWriteDto dto, Guid actor, bool manager, CancellationToken ct)
    {
        if (record.Name.Length < 2) throw Bad("Name must contain two characters.");
        if (record is Skill && await db.Set<Skill>().AnyAsync(x => x.Id != record.Id && !x.IsArchived && x.Name.ToUpper() == record.Name.ToUpper(), ct)) throw Conflict("An active skill with this name already exists.");
        if (record is Worker worker)
        {
            if (dto.UserId != null && !await db.Users.AnyAsync(x => x.Id == dto.UserId && x.IsActive, ct)) throw Bad("Active user not found.");
            if (!dto.IsActive && await db.Set<WorkerAssignment>().AnyAsync(x => x.WorkerId == worker.Id && !x.IsArchived && x.Status != "Completed" && x.Status != "Cancelled", ct)) throw Conflict("Finish or cancel assignments before deactivating this worker.");
            worker.UserId = dto.UserId; worker.Phone = dto.Phone.Trim(); worker.IsActive = dto.IsActive;
        }
        if (record is WorkerSkill skill)
        {
            if (db.Entry(skill).State != EntityState.Detached && (skill.WorkerId != dto.WorkerId || skill.SkillId != dto.SkillId)) throw Conflict("Archive the existing skill link before creating a different one.");
            skill.WorkerId = Required(dto.WorkerId, "Worker"); skill.SkillId = Required(dto.SkillId, "Skill");
            await WorkerAsync(skill.WorkerId, ct);
            if (!await db.Set<Skill>().AnyAsync(x => x.Id == skill.SkillId && !x.IsArchived, ct)) throw Bad("Active skill not found.");
            if (await db.Set<WorkerSkill>().AnyAsync(x => x.Id != skill.Id && x.WorkerId == skill.WorkerId && x.SkillId == skill.SkillId && !x.IsArchived, ct)) throw Conflict("Worker already has this skill.");
            if (await db.Set<WorkerAssignment>().AnyAsync(x => x.WorkerId == skill.WorkerId && x.RequiredSkillId != null && !x.IsArchived && x.Status != "Completed" && x.Status != "Cancelled" && !db.Set<WorkerSkill>().Any(s => s.WorkerId == x.WorkerId && s.SkillId == x.RequiredSkillId && !s.IsArchived && s.Id != skill.Id) && x.RequiredSkillId != skill.SkillId, ct)) throw Conflict("This skill is needed by an active assignment.");
        }
        if (record is Equipment equipment)
        {
            equipment.Code = dto.Code.Trim().ToUpperInvariant(); equipment.Category = dto.Category.Trim();
            if (equipment.Code.Length == 0 || equipment.Category.Length == 0) throw Bad("Equipment code and category are required.");
            equipment.Status = dto.Status ?? "Operational";
            if (equipment.Status is not ("Operational" or "Maintenance" or "OutOfService")) throw Bad("Invalid equipment status.");
            if (await db.Set<Equipment>().AnyAsync(x => x.Id != equipment.Id && x.Code == equipment.Code, ct)) throw Conflict("Equipment code already exists.");
            if (equipment.Status != "Operational" && await db.Set<EquipmentReservation>().AnyAsync(x => x.EquipmentId == equipment.Id && !x.IsArchived && x.Status != "Returned" && x.Status != "Cancelled", ct)) throw Conflict("Cancel active bookings before changing equipment availability.");
        }
        if (record is Shift shift)
        {
            if (db.Entry(shift).State != EntityState.Detached && shift.WorkerId != dto.WorkerId) throw Conflict("Shift worker cannot be changed.");
            shift.WorkerId = Required(dto.WorkerId, "Worker"); await WorkerAsync(shift.WorkerId, ct);
            shift.StartTime = Start(dto); shift.EndTime = End(dto); ValidateWindow(shift.StartTime, shift.EndTime);
            if (await db.Set<Shift>().AnyAsync(x => x.Id != shift.Id && !x.IsArchived && x.WorkerId == shift.WorkerId && x.StartTime < shift.EndTime && shift.StartTime < x.EndTime, ct)) throw Conflict("Worker shifts overlap.");
            var bookings = await db.Set<WorkerAssignment>().Where(x => x.WorkerId == shift.WorkerId && !x.IsArchived && x.Status != "Cancelled" && x.Status != "Completed").ToListAsync(ct);
            foreach (var booking in bookings)
                if (!(shift.StartTime <= booking.StartTime && shift.EndTime >= booking.EndTime) && !await db.Set<Shift>().AnyAsync(x => x.Id != shift.Id && !x.IsArchived && x.WorkerId == shift.WorkerId && x.StartTime <= booking.StartTime && x.EndTime >= booking.EndTime, ct)) throw Conflict("Shift change would leave an assignment uncovered.");
        }
        if (record is WorkSchedule schedule)
        {
            if (schedule.WorkflowId != null && (schedule.StartTime != dto.StartTime || schedule.EndTime != dto.EndTime || schedule.DependencyId != dto.DependencyId)) throw Conflict("Approved workflow dates and dependencies are fixed. Cancel the schedule and submit a revised resource request.");
            if (db.Entry(schedule).State != EntityState.Detached && schedule.ActivityId != dto.ActivityId) throw Conflict("Schedule activity cannot be changed.");
            if (dto.Status == "Completed" && (await db.Set<WorkerAssignment>().AnyAsync(x => x.ScheduleId == schedule.Id && !x.IsArchived && x.Status != "Completed" && x.Status != "Cancelled", ct) || await db.Set<EquipmentReservation>().AnyAsync(x => x.ScheduleId == schedule.Id && !x.IsArchived && x.Status != "Returned" && x.Status != "Cancelled", ct))) throw Conflict("Complete assignments and return equipment before completing the schedule.");
            schedule.ActivityId = Required(dto.ActivityId, "Activity"); schedule.StartTime = Start(dto); schedule.EndTime = End(dto);
            schedule.DependencyId = dto.DependencyId; schedule.Status = dto.Status ?? "Draft";
            if (schedule.Status is not ("Draft" or "Approved" or "InProgress" or "Completed")) throw Bad("Use cancellation for cancelled schedules.");
            await ValidateScheduleAsync(schedule, ct);
            if (await db.Set<WorkerAssignment>().AnyAsync(x => x.ScheduleId == schedule.Id && !x.IsArchived && (x.StartTime < schedule.StartTime || x.EndTime > schedule.EndTime), ct) || await db.Set<EquipmentReservation>().AnyAsync(x => x.ScheduleId == schedule.Id && !x.IsArchived && (x.StartTime < schedule.StartTime || x.EndTime > schedule.EndTime), ct)) throw Conflict("Schedule must contain existing bookings.");
            if (await db.Set<WorkSchedule>().AnyAsync(x => x.DependencyId == schedule.Id && !x.IsArchived && x.Status != "Cancelled" && x.StartTime < schedule.EndTime, ct)) throw Conflict("Dependent schedules start before this schedule finishes.");
        }
        if (record is WorkerAssignment assignment)
        {
            assignment.WorkerId = Required(dto.WorkerId, "Worker"); assignment.ScheduleId = Required(dto.ScheduleId, "Schedule"); assignment.RequiredSkillId = dto.RequiredSkillId;
            assignment.StartTime = Start(dto); assignment.EndTime = End(dto); assignment.Status = dto.Status ?? "Upcoming";
            if (assignment.Status is not ("Upcoming" or "InProgress" or "Completed" or "Delayed")) throw Bad("Invalid assignment status.");
            await ValidateWorkerBookingAsync(assignment, ct);
        }
        if (record is EquipmentReservation reservation)
        {
            reservation.EquipmentId = Required(dto.EquipmentId, "Equipment"); reservation.ScheduleId = Required(dto.ScheduleId, "Schedule"); reservation.StartTime = Start(dto); reservation.EndTime = End(dto); reservation.Status = dto.Status ?? "Reserved";
            if (reservation.Status is not ("Reserved" or "Received" or "Returned")) throw Bad("Invalid equipment booking status.");
            await ValidateEquipmentBookingAsync(reservation, ct);
        }
        if (record is EquipmentRequest request)
        {
            request.ActivityId = Required(dto.ActivityId, "Activity"); request.EquipmentId = Required(dto.EquipmentId, "Equipment");
            if (!await CanAccessActivityAsync(request.ActivityId, actor, manager, ct)) throw Bad("Assigned active activity is required.");
            await EquipmentAsync(request.EquipmentId, ct); request.Status = "Draft";
        }
        if (record is SiteIssue issue)
        {
            issue.ActivityId = Required(dto.ActivityId, "Activity"); issue.EquipmentId = dto.EquipmentId;
            if (!await CanAccessActivityAsync(issue.ActivityId, actor, manager, ct)) throw Bad("Assigned active activity is required.");
            if (issue.EquipmentId != null) await EquipmentAsync(issue.EquipmentId.Value, ct);
            issue.Severity = dto.Severity; issue.Status = dto.Status ?? "Open";
            if (issue.Status is not ("Open" or "Resolved")) throw Bad("Invalid issue status.");
        }
    }

    private static DateTimeOffset Start(SchedulingWriteDto dto) => dto.StartTime?.ToUniversalTime() ?? throw Bad("Start time is required.");
    private static DateTimeOffset End(SchedulingWriteDto dto) => dto.EndTime?.ToUniversalTime() ?? throw Bad("End time is required.");
    private async Task WorkerAsync(Guid id, CancellationToken ct) { if (!await db.Set<Worker>().AnyAsync(x => x.Id == id && x.IsActive && !x.IsArchived, ct)) throw Bad("Active worker not found."); }
    private async Task EquipmentAsync(Guid id, CancellationToken ct) { if (!await db.Set<Equipment>().AnyAsync(x => x.Id == id && !x.IsArchived && x.Status == "Operational", ct)) throw Bad("Operational equipment not found."); }

    public async Task ValidateScheduleAsync(WorkSchedule schedule, CancellationToken ct)
    {
        ValidateWindow(schedule.StartTime, schedule.EndTime);
        var activity = await db.ConstructionActivities.Include(x => x.Phase).ThenInclude(x => x.Site).ThenInclude(x => x.Project).SingleOrDefaultAsync(x => x.Id == schedule.ActivityId, ct);
        if (activity == null || activity.IsArchived || activity.Phase.IsArchived || activity.Phase.Site.IsArchived || activity.Phase.Site.Project.IsArchived) throw Bad("Active activity is required.");
        var deadlines = new[] { activity.DueDate, activity.Phase.EndDate, activity.Phase.Site.Project.EndDate };
        if (deadlines.Any(date => date != null && DateOnly.FromDateTime(schedule.EndTime.UtcDateTime) > date)) throw Bad("Schedule exceeds the activity, phase or project deadline.");
        if (new[] { activity.Phase.StartDate, activity.Phase.Site.Project.StartDate }.Any(date => date != null && DateOnly.FromDateTime(schedule.StartTime.UtcDateTime) < date)) throw Bad("Schedule starts before the phase or project.");
        var visited = new HashSet<Guid> { schedule.Id };
        var dependencyId = schedule.DependencyId;
        while (dependencyId != null)
        {
            if (!visited.Add(dependencyId.Value)) throw Bad("Schedule dependencies contain a cycle.");
            var dependency = await db.Set<WorkSchedule>().SingleOrDefaultAsync(x => x.Id == dependencyId && !x.IsArchived && x.Status != "Cancelled", ct) ?? throw Bad("Active dependency not found.");
            if (dependency.Id == schedule.DependencyId && dependency.EndTime > schedule.StartTime) throw Conflict("Dependency finishes after the schedule starts.");
            var dependencyProject = await db.ConstructionActivities.Where(x => x.Id == dependency.ActivityId).Select(x => x.Phase.Site.ProjectId).SingleAsync(ct);
            if (dependencyProject != activity.Phase.Site.ProjectId) throw Bad("Dependency must belong to the same project.");
            dependencyId = dependency.DependencyId;
        }
    }
    private async Task ValidateBookingWindowAsync(Guid scheduleId, DateTimeOffset start, DateTimeOffset end, string status, CancellationToken ct)
    {
        ValidateWindow(start, end);
        var schedule = await db.Set<WorkSchedule>().SingleOrDefaultAsync(x => x.Id == scheduleId && !x.IsArchived && x.Status != "Cancelled", ct) ?? throw Bad("Active schedule is required.");
        if (start < schedule.StartTime || end > schedule.EndTime) throw Bad("Booking must fall within the schedule.");
        if (status is "InProgress" or "Completed" or "Received" or "Returned" && schedule.Status == "Draft") throw Conflict("Approve the schedule before starting work or receiving equipment.");
    }
    public async Task ValidateWorkerBookingAsync(WorkerAssignment x, CancellationToken ct)
    {
        await WorkerAsync(x.WorkerId, ct); await ValidateBookingWindowAsync(x.ScheduleId, x.StartTime, x.EndTime, x.Status, ct);
        if (x.RequiredSkillId != null && !await db.Set<WorkerSkill>().AnyAsync(s => s.WorkerId == x.WorkerId && s.SkillId == x.RequiredSkillId && !s.IsArchived && db.Set<Skill>().Any(k => k.Id == s.SkillId && !k.IsArchived), ct)) throw Bad("Worker does not have the required skill.");
        if (!await db.Set<Shift>().AnyAsync(s => s.WorkerId == x.WorkerId && !s.IsArchived && s.StartTime <= x.StartTime && s.EndTime >= x.EndTime, ct)) throw Bad("Worker needs a shift covering this assignment.");
        if (await db.Set<WorkerAssignment>().AnyAsync(s => s.Id != x.Id && s.WorkerId == x.WorkerId && !s.IsArchived && s.Status != "Cancelled" && s.StartTime < x.EndTime && x.StartTime < s.EndTime, ct)) throw Conflict("Worker is already assigned during this time.");
    }
    public async Task ValidateEquipmentBookingAsync(EquipmentReservation x, CancellationToken ct)
    {
        await EquipmentAsync(x.EquipmentId, ct); await ValidateBookingWindowAsync(x.ScheduleId, x.StartTime, x.EndTime, x.Status, ct);
        if (await db.Set<EquipmentReservation>().AnyAsync(s => s.Id != x.Id && s.EquipmentId == x.EquipmentId && !s.IsArchived && s.Status != "Cancelled" && s.StartTime < x.EndTime && x.StartTime < s.EndTime, ct)) throw Conflict("Equipment is already reserved during this time.");
    }

    public async Task ArchiveAsync<T>(Guid id, Guid actor, bool manager, CancellationToken ct) where T : SchedulingRecord
    {
        if (!manager && typeof(T) != typeof(EquipmentRequest) && typeof(T) != typeof(SiteIssue)) throw new ApiException(403, "forbidden", "Management permission is required.");
        await using var tx = await db.Database.BeginTransactionAsync(ct); await LockAsync(ct);
        var row = await Visible<T>(actor, manager).SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ApiException(404, "not_found", "Record not found.");
        if (row.IsArchived) return;
        if (row is Worker && (await db.Set<WorkerAssignment>().AnyAsync(x => x.WorkerId == id && !x.IsArchived && x.Status != "Completed" && x.Status != "Cancelled", ct)) ||
            row is Skill && await db.Set<WorkerSkill>().AnyAsync(x => x.SkillId == id && !x.IsArchived, ct) ||
            row is Equipment && await db.Set<EquipmentReservation>().AnyAsync(x => x.EquipmentId == id && !x.IsArchived && x.Status != "Returned" && x.Status != "Cancelled", ct)) throw Conflict("Cancel active dependent records first.");
        if (row is WorkerSkill skill && await db.Set<WorkerAssignment>().AnyAsync(x => x.WorkerId == skill.WorkerId && x.RequiredSkillId == skill.SkillId && !x.IsArchived && x.Status != "Completed" && x.Status != "Cancelled", ct)) throw Conflict("Skill is required by an active assignment.");
        if (row is Shift shift && await db.Set<WorkerAssignment>().AnyAsync(x => x.WorkerId == shift.WorkerId && !x.IsArchived && x.Status != "Completed" && x.Status != "Cancelled" && x.StartTime >= shift.StartTime && x.EndTime <= shift.EndTime, ct)) throw Conflict("Shift covers active assignments.");
        if (row is WorkSchedule schedule)
        {
            if (await db.Set<WorkSchedule>().AnyAsync(x => x.DependencyId == id && !x.IsArchived && x.Status != "Cancelled", ct)) throw Conflict("Cancel dependent schedules first.");
            if (await db.Set<WorkerAssignment>().AnyAsync(x => x.ScheduleId == id && !x.IsArchived && x.Status != "Cancelled", ct) || await db.Set<EquipmentReservation>().AnyAsync(x => x.ScheduleId == id && !x.IsArchived && x.Status != "Cancelled", ct)) throw Conflict("Archive or cancel schedule bookings first.");
            if (schedule.Status != "Completed") schedule.Status = "Cancelled";
            if (schedule.Status == "Cancelled")
            {
                await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(42002001)", ct);
                var reservations = await db.InventoryReservations.Include(x => x.Material).Where(x => x.ScheduleId == id && x.Status == "Active").ToListAsync(ct);
                foreach (var allocation in reservations) { allocation.Material.ReservedStock -= allocation.Quantity; allocation.Status = "Released"; allocation.ReleasedAt = DateTimeOffset.UtcNow; }
            }
        }
        if (row is WorkerAssignment assignment && assignment.Status != "Completed") assignment.Status = "Cancelled";
        if (row is EquipmentReservation reservation && reservation.Status != "Returned") reservation.Status = "Cancelled";
        if (row is EquipmentRequest request) request.Status = "Cancelled";
        row.IsArchived = true; row.UpdatedById = actor; await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }

    public async Task<object> AvailabilityAsync(AvailabilityQuery query, CancellationToken ct)
    {
        ValidateWindow(query.StartTime, query.EndTime);
        var workers = await db.Set<Worker>().AsNoTracking().Where(w => !w.IsArchived && w.IsActive &&
            (query.SkillId == null || db.Set<WorkerSkill>().Any(s => s.WorkerId == w.Id && s.SkillId == query.SkillId && !s.IsArchived)) &&
            db.Set<Shift>().Any(s => s.WorkerId == w.Id && !s.IsArchived && s.StartTime <= query.StartTime && s.EndTime >= query.EndTime) &&
            !db.Set<WorkerAssignment>().Any(a => a.WorkerId == w.Id && !a.IsArchived && a.Status != "Cancelled" && a.StartTime < query.EndTime && query.StartTime < a.EndTime)).ToListAsync(ct);
        var equipment = await db.Set<Equipment>().AsNoTracking().Where(e => !e.IsArchived && e.Status == "Operational" &&
            !db.Set<EquipmentReservation>().Any(r => r.EquipmentId == e.Id && !r.IsArchived && r.Status != "Cancelled" && r.StartTime < query.EndTime && query.StartTime < r.EndTime)).ToListAsync(ct);
        return new { workers, equipment };
    }
}
internal static class StableSchedulingOrder
{
    public static IQueryable<T> ThenStable<T>(this IQueryable<T> rows) where T : SchedulingRecord => ((IOrderedQueryable<T>)rows).ThenBy(x => x.Id);
}
