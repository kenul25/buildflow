using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Models;
using BuildFlow.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Controllers;

[ApiController, Authorize]
public abstract class SchedulingCrudController<T>(SchedulingService service) : ControllerBase where T : SchedulingRecord, new()
{
    protected Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    protected bool Manager => User.IsInRole("Administrator") || User.IsInRole("ProjectManager");
    [HttpGet] public Task<PageResult<T>> List([FromQuery] SchedulingQuery query, CancellationToken ct) => service.ListAsync<T>(query, Actor, Manager, ct);
    [HttpGet("{id:guid}")] public Task<T> Get(Guid id, CancellationToken ct) => service.GetAsync<T>(id, Actor, Manager, ct);
    [HttpPost] public async Task<ActionResult<T>> Create(SchedulingWriteDto dto, CancellationToken ct)
    {
        var row = await service.WriteAsync<T>(null, dto, Actor, Manager, ct);
        return CreatedAtAction(nameof(Get), new { id = row.Id }, row);
    }
    [HttpPut("{id:guid}")] public Task<T> Update(Guid id, SchedulingWriteDto dto, CancellationToken ct) => service.WriteAsync<T>(id, dto, Actor, Manager, ct);
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await service.ArchiveAsync<T>(id, Actor, Manager, ct); return NoContent(); }
}
[Route("api/scheduling/workers")] public sealed class WorkersController(SchedulingService s) : SchedulingCrudController<Worker>(s);
[Route("api/scheduling/skills")] public sealed class SkillsController(SchedulingService s) : SchedulingCrudController<Skill>(s);
[Route("api/scheduling/worker-skills")] public sealed class WorkerSkillsController(SchedulingService s) : SchedulingCrudController<WorkerSkill>(s);
[Route("api/scheduling/shifts")] public sealed class ShiftsController(SchedulingService s) : SchedulingCrudController<Shift>(s);
[Route("api/scheduling/equipment")] public sealed class EquipmentController(SchedulingService s) : SchedulingCrudController<Equipment>(s);
[Route("api/scheduling/schedules")] public sealed class SchedulesController(SchedulingService s) : SchedulingCrudController<WorkSchedule>(s);
[Route("api/scheduling/worker-assignments")] public sealed class WorkerAssignmentsController(SchedulingService s) : SchedulingCrudController<WorkerAssignment>(s);
[Route("api/scheduling/equipment-reservations")] public sealed class EquipmentReservationsController(SchedulingService s) : SchedulingCrudController<EquipmentReservation>(s);
[Route("api/scheduling/equipment-requests")] public sealed class EquipmentRequestsController(SchedulingService s) : SchedulingCrudController<EquipmentRequest>(s);
[Route("api/scheduling/issues")] public sealed class SiteIssuesController(SchedulingService s) : SchedulingCrudController<SiteIssue>(s);

[ApiController, Route("api/scheduling"), Authorize(Roles = "Administrator,ProjectManager,SiteEngineer")]
public sealed class SchedulingOperationsController(SchedulingService service, BuildFlowDbContext db) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool Manager => User.IsInRole("Administrator") || User.IsInRole("ProjectManager");
    [HttpGet("availability")] public Task<object> Availability([FromQuery] AvailabilityQuery query, CancellationToken ct) => service.AvailabilityAsync(query, ct);
    [HttpPut("worker-assignments/{id:guid}/status")]
    public async Task<IActionResult> AssignmentStatus(Guid id, StatusWriteDto dto, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); await service.LockAsync(ct);
        var assignment = await db.Set<WorkerAssignment>().SingleOrDefaultAsync(x => x.Id == id && !x.IsArchived, ct) ?? throw new ApiException(404, "not_found", "Assignment not found.");
        var schedule = await db.Set<WorkSchedule>().SingleAsync(x => x.Id == assignment.ScheduleId, ct);
        if (!await service.CanAccessActivityAsync(schedule.ActivityId, Actor, Manager, ct)) return NotFound();
        if (schedule.IsArchived || schedule.Status is "Draft" or "Cancelled") throw SchedulingService.Conflict("An approved active schedule is required.");
        if (assignment.Status is "Completed" or "Cancelled" || dto.Status is not ("InProgress" or "Completed" or "Delayed") || dto.Status == "Completed" && assignment.Status == "Upcoming") throw SchedulingService.Conflict("Invalid assignment status transition.");
        assignment.Status = dto.Status; assignment.Notes = dto.Notes ?? assignment.Notes; assignment.UpdatedById = Actor;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Ok(assignment);
    }
    [HttpPost("equipment/scan")]
    public async Task<IActionResult> Scan([FromBody] EquipmentScanDto dto, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); await service.LockAsync(ct);
        var equipment = await db.Set<Equipment>().SingleOrDefaultAsync(x => x.Code == dto.Code.Trim().ToUpperInvariant() && !x.IsArchived, ct) ?? throw new ApiException(404, "not_found", "Equipment code not found.");
        var now = DateTimeOffset.UtcNow;
        var reservation = await db.Set<EquipmentReservation>().OrderBy(x => x.StartTime).FirstOrDefaultAsync(x => x.EquipmentId == equipment.Id && !x.IsArchived && x.StartTime <= now && (dto.Action == "return" ? x.Status == "Received" : x.Status == "Reserved" && x.EndTime >= now), ct) ?? throw SchedulingService.Conflict("No eligible equipment assignment exists.");
        if (dto.Action == "receive" && (equipment.Status != "Operational" || await db.Set<EquipmentReservation>().AnyAsync(x => x.EquipmentId == equipment.Id && !x.IsArchived && x.Status == "Received", ct))) throw SchedulingService.Conflict("Equipment is unavailable or has not been returned.");
        var schedule = await db.Set<WorkSchedule>().SingleAsync(x => x.Id == reservation.ScheduleId, ct);
        if (!await service.CanAccessActivityAsync(schedule.ActivityId, Actor, Manager, ct)) return NotFound();
        if (schedule.Status is "Draft" or "Cancelled" || schedule.IsArchived) throw SchedulingService.Conflict("Approve the schedule first.");
        if (dto.Action == "receive" && reservation.Status == "Reserved") reservation.Status = "Received";
        else if (dto.Action == "return" && reservation.Status == "Received") reservation.Status = "Returned";
        else throw SchedulingService.Conflict("Invalid equipment action.");
        reservation.UpdatedById = Actor; await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Ok(reservation);
    }
}
public sealed record EquipmentScanDto([Required, StringLength(40)] string Code, [Required, RegularExpression("receive|return")] string Action);
