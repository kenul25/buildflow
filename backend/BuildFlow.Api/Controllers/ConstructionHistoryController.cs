using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Models;
using BuildFlow.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BuildFlow.Api.Controllers;
[ApiController, Route("api/construction"), Authorize(Roles = "SiteEngineer,ProjectManager,Administrator")]
public sealed class ConstructionHistoryController(BuildFlowDbContext db, SchedulingService access) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool Manager => User.IsInRole("ProjectManager") || User.IsInRole("Administrator");
    private async Task Check(Guid activity, CancellationToken ct) { if (!await access.CanAccessActivityAsync(activity, Actor, Manager, ct)) throw new ApiException(404, "not_found", "Assigned active activity not found."); }
    private static bool Editable(ResourceRequest row, PlanningWorkflow? workflow) => row.Status == "Draft" && (workflow == null || workflow.Status is "Queued" or "PendingProjectManagerApproval");
    private static IReadOnlyList<JsonElement> PlanningLogs(PlanningWorkflow? workflow)
    {
        var logs = new List<JsonElement>();
        if (workflow?.PlanJson != null)
        {
            try
            {
                using var document = JsonDocument.Parse(workflow.PlanJson);
                if (document.RootElement.TryGetProperty("executionHistory", out var history) && history.ValueKind == JsonValueKind.Array)
                    logs.AddRange(history.EnumerateArray().Select(x => x.Clone()));
                if (document.RootElement.TryGetProperty("executionSummary", out var summary)) logs.Add(summary.Clone());
            }
            catch (JsonException) { /* The current workflow status remains readable if old audit JSON is damaged. */ }
        }
        if (workflow != null) logs.Add(JsonSerializer.SerializeToElement(new { workflowStatus = workflow.Status, completedAt = workflow.CompletedAt, createdAt = workflow.CreatedAt, error = workflow.Error }));
        return logs;
    }
    [HttpGet("resource-requests/{id:guid}")]
    public async Task<IActionResult> RequestDetails(Guid id, CancellationToken ct)
    {
        var row = await db.ResourceRequests.Include(x => x.Items).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ApiException(404, "not_found", "Request not found."); await Check(row.ActivityId, ct);
        var workflow = await db.PlanningWorkflows.AsNoTracking().SingleOrDefaultAsync(x => x.ResourceRequestId == id, ct);
        return Ok(new { row.Id, row.ProjectId, row.SiteId, row.ActivityId, row.Objective, row.Notes, row.BudgetLimit, row.RequiredBy, row.Status, canEdit = Editable(row, workflow), workflowStatus = workflow?.Status, planningHistory = PlanningLogs(workflow), items = row.Items.Select(x => new { x.Kind, x.Name, x.Quantity, x.Unit, x.ResourceCount }) });
    }
    [HttpPut("resource-requests/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, ResourceRequestWriteDto dto, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(42001001)", ct);
        var row = await db.ResourceRequests.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ApiException(404, "not_found", "Request not found."); await Check(row.ActivityId, ct);
        var workflow = await db.PlanningWorkflows.SingleOrDefaultAsync(x => x.ResourceRequestId == id, ct);
        if (!Editable(row, workflow)) throw new ApiException(409, "request_locked", "Only pending or queued requests can be edited. Planning in progress and completed decisions are locked.");
        if (dto.ActivityId != row.ActivityId || dto.ProjectId != row.ProjectId || dto.SiteId != row.SiteId) throw SchedulingService.Bad("Request hierarchy is immutable.");
        if (dto.Items.Count is < 1 or > 50 || dto.Items.Any(x => x.Quantity <= 0) || dto.RequiredBy < DateOnly.FromDateTime(DateTime.UtcNow)) throw SchedulingService.Bad("Invalid request items or required date.");
        ResourceUsage.Validate(dto.Items);
        db.ResourceRequestItems.RemoveRange(row.Items);
        var replacementItems = dto.Items.Select(x => new ResourceRequestItem { Id = Guid.NewGuid(), ResourceRequestId = row.Id, Kind = x.Kind, Name = x.Name.Trim(), Quantity = x.Quantity, Unit = x.Unit.Trim(), ResourceCount = x.ResourceCount }).ToList();
        db.ResourceRequestItems.AddRange(replacementItems); row.Items = replacementItems;
        row.Objective = dto.Objective.Trim(); row.Notes = dto.Notes?.Trim(); row.RequiredBy = dto.RequiredBy; row.BudgetLimit = dto.BudgetLimit;
        if (workflow != null) { workflow.Status = "Queued"; workflow.Error = null; workflow.CompletedAt = null; }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return await RequestDetails(id, ct);
    }
    [HttpDelete("resource-requests/{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(42001001)", ct);
        var row = await db.ResourceRequests.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ApiException(404, "not_found", "Request not found."); await Check(row.ActivityId, ct);
        var workflow = await db.PlanningWorkflows.SingleOrDefaultAsync(x => x.ResourceRequestId == id, ct);
        if (!Editable(row, workflow)) throw SchedulingService.Conflict("Only pending or queued requests can be deleted.");
        if (workflow != null) { workflow.Status = "Cancelled"; workflow.CompletedAt = DateTimeOffset.UtcNow; }
        row.Status = "Cancelled"; await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return NoContent();
    }
    [HttpPut("activities/{activityId:guid}/progress/{id:guid}")]
    public Task<IActionResult> EditProgress(Guid activityId, Guid id, ProgressWriteDto dto, CancellationToken ct) => ChangeProgress(activityId, id, dto, ct);

    [HttpDelete("activities/{activityId:guid}/progress/{id:guid}")]
    public Task<IActionResult> RemoveProgress(Guid activityId, Guid id, CancellationToken ct) => ChangeProgress(activityId, id, null, ct);

    private async Task<IActionResult> ChangeProgress(Guid activityId, Guid id, ProgressWriteDto? dto, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(42001001)", ct);
        await Check(activityId, ct);
        var rows = await db.ProgressUpdates.Where(x => x.ActivityId == activityId && !x.IsArchived).OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).ToListAsync(ct);
        var row = rows.SingleOrDefault(x => x.Id == id) ?? throw new ApiException(404, "not_found", "Progress update not found.");
        if (!Manager && row.SubmittedById != Actor) return Forbid();
        if (dto == null) row.IsArchived = true;
        else
        {
            var index = rows.IndexOf(row);
            if (dto.ProgressPercent < (index > 0 ? rows[index - 1].ProgressPercent : 0) || dto.ProgressPercent > (index + 1 < rows.Count ? rows[index + 1].ProgressPercent : 100))
                throw SchedulingService.Conflict("Progress must stay between the previous and next updates.");
            // Retain the original values for audit while showing the corrected entry inline.
            db.ProgressUpdates.Add(new ProgressUpdate { Id = Guid.NewGuid(), ActivityId = activityId, ProgressPercent = row.ProgressPercent, WorkCompleted = row.WorkCompleted, Blockers = row.Blockers, SubmittedById = row.SubmittedById, CorrectionOfId = row.Id, IsArchived = true });
            row.ProgressPercent = dto.ProgressPercent; row.WorkCompleted = dto.WorkCompleted.Trim(); row.Blockers = dto.Blockers?.Trim();
        }
        var activity = await db.ConstructionActivities.SingleAsync(x => x.Id == activityId, ct);
        activity.ProgressPercent = rows.LastOrDefault(x => !x.IsArchived)?.ProgressPercent ?? 0;
        activity.Status = activity.ProgressPercent == 100 ? "Completed" : activity.ProgressPercent == 0 ? "Planned" : "InProgress";
        activity.UpdatedById = Actor;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return NoContent();
    }
    [HttpGet("activities/{id:guid}/photos")]
    public async Task<IActionResult> Photos(Guid id, CancellationToken ct)
    {
        await Check(id, ct); return Ok(await db.SitePhotos.AsNoTracking().Where(x => x.ActivityId == id && !x.IsArchived).Select(x => new { x.Id, x.Caption, x.OriginalName, x.CreatedAt }).ToListAsync(ct));
    }
    [HttpPut("photos/{id:guid}")]
    public async Task<IActionResult> Caption(Guid id, CaptionDto dto, CancellationToken ct)
    {
        var row = await db.SitePhotos.SingleOrDefaultAsync(x => x.Id == id && !x.IsArchived, ct) ?? throw new ApiException(404, "not_found", "Photo not found."); await Check(row.ActivityId, ct);
        row.Caption = dto.Caption?.Trim(); await db.SaveChangesAsync(ct); return NoContent();
    }
    [HttpDelete("photos/{id:guid}")]
    public async Task<IActionResult> ArchivePhoto(Guid id, CancellationToken ct)
    {
        var row = await db.SitePhotos.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ApiException(404, "not_found", "Photo not found."); await Check(row.ActivityId, ct);
        if (!Manager && row.UploadedById != Actor) return Forbid(); row.IsArchived = true; await db.SaveChangesAsync(ct); return NoContent();
    }
    [HttpPost("activities/{id:guid}/progress/corrections"), Authorize(Roles = "ProjectManager,Administrator")]
    public async Task<IActionResult> Correct(Guid id, ProgressWriteDto dto, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(42001001)", ct); await Check(id, ct);
        var activity = await db.ConstructionActivities.SingleAsync(x => x.Id == id, ct);
        var prior = await db.ProgressUpdates.Where(x => x.ActivityId == id && !x.IsArchived).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
        var correction = new ProgressUpdate { Id = Guid.NewGuid(), ActivityId = id, ProgressPercent = dto.ProgressPercent, WorkCompleted = dto.WorkCompleted.Trim(), Blockers = dto.Blockers, SubmittedById = Actor, CorrectionOfId = prior?.Id };
        activity.ProgressPercent = dto.ProgressPercent; activity.Status = dto.ProgressPercent == 100 ? "Completed" : dto.ProgressPercent == 0 ? "Planned" : "InProgress"; activity.UpdatedById = Actor;
        db.Add(correction); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Ok(new { correction.Id });
    }
}
public sealed record CaptionDto([StringLength(2000)] string? Caption);
