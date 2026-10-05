using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BuildFlow.Api.Data;
using BuildFlow.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Controllers;

[ApiController, Route("api/notifications"), Authorize]
public sealed class NotificationsController(BuildFlowDbContext db, TimeProvider clock) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool Manager => User.IsInRole("Administrator") || User.IsInRole("ProjectManager");
    private sealed record Notice(Guid Id, string Title, string Message, string Status, Guid WorkflowId, Guid RequestId, Guid ActivityId, DateTimeOffset CreatedAt, bool IsRead);

    // Status events are derived from authoritative workflows, never from the client.
    private async Task<List<Notice>> Feed(CancellationToken ct)
    {
        var rows = await db.PlanningWorkflows.AsNoTracking()
            .Where(x => !x.ResourceRequest.Project.IsArchived &&
                (Manager || x.ResourceRequest.Project.AssignedEngineerId == Actor || x.ResourceRequest.SubmittedById == Actor) &&
                (x.Status == "Approved" || x.Status == "Rejected" || x.Status == "RevisionRequested" || x.Status == "Failed" || Manager && x.Status == "PendingProjectManagerApproval"))
            .OrderByDescending(x => x.CompletedAt ?? x.CreatedAt).ThenBy(x => x.Id).Take(50)
            .Select(x => new { x.Id, x.Status, x.ResourceRequestId, x.ResourceRequest.ActivityId, x.ResourceRequest.Objective, At = x.CompletedAt ?? x.CreatedAt }).ToListAsync(ct);
        var notices = rows.Select(x => new Notice(
            new Guid(SHA256.HashData(Encoding.UTF8.GetBytes($"{x.Id}:{x.Status}:{x.At.UtcTicks}"))[..16]),
            x.Status switch { "Approved" => "Resource plan approved", "Rejected" => "Resource plan rejected", "RevisionRequested" => "Plan revision requested", "Failed" => "Planning needs attention", _ => "Plan awaiting your approval" },
            x.Objective, x.Status, x.Id, x.ResourceRequestId, x.ActivityId, x.At, false)).ToList();
        var ids = notices.Select(x => x.Id).ToArray();
        var reads = await db.NotificationReads.Where(x => x.UserId == Actor && ids.Contains(x.NotificationId)).Select(x => x.NotificationId).ToListAsync(ct);
        var readIds = reads.ToHashSet();
        return notices.Select(x => x with { IsRead = readIds.Contains(x.Id) }).ToList();
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var notices = await Feed(ct);
        return Ok(new { items = notices, unreadCount = notices.Count(x => !x.IsRead) });
    }

    private Task RecordRead(Guid id, CancellationToken ct) => db.Database.ExecuteSqlInterpolatedAsync(
        $"INSERT INTO \"NotificationReads\" (\"UserId\", \"NotificationId\", \"ReadAt\") VALUES ({Actor}, {id}, {clock.GetUtcNow()}) ON CONFLICT (\"UserId\", \"NotificationId\") DO NOTHING", ct);

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> Read(Guid id, CancellationToken ct)
    {
        if (!(await Feed(ct)).Any(x => x.Id == id)) throw new ApiException(404, "not_found", "Notification not found.");
        await RecordRead(id, ct);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> ReadAll(CancellationToken ct)
    {
        foreach (var notice in (await Feed(ct)).Where(x => !x.IsRead)) await RecordRead(notice.Id, ct);
        return NoContent();
    }
}
