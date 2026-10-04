using System.Security.Claims;
using BuildFlow.Api.Data;
using BuildFlow.Api.Models;
using BuildFlow.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Controllers;
[ApiController, Route("api/dashboard"), Authorize]
public sealed class DashboardController(BuildFlowDbContext db, AppDbContext procurement, InventoryService inventory) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Summary(CancellationToken ct)
    {
        var actor = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var manager = User.IsInRole("Administrator") || User.IsInRole("ProjectManager");
        var office = manager || User.IsInRole("ProcurementOfficer");
        var projects = db.Projects.Where(x => !x.IsArchived && (manager || x.AssignedEngineerId == actor));
        var ids = projects.Select(x => x.Id);
        var activities = db.ConstructionActivities.Where(x => !x.IsArchived && !x.Phase.IsArchived && !x.Phase.Site.IsArchived && ids.Contains(x.Phase.Site.ProjectId));
        var schedules = db.Set<WorkSchedule>().Where(x => !x.IsArchived && activities.Select(a => a.Id).Contains(x.ActivityId));
        var projectIds = await ids.ToArrayAsync(ct);
        await inventory.RefreshExpiryAsync(ct);
        return Ok(new {
            activeProjects = await projects.CountAsync(x => x.Status != "Completed", ct),
            activeActivities = await activities.CountAsync(x => x.Status != "Completed", ct),
            approvedPlans = await schedules.CountAsync(x => x.Status == "Approved" || x.Status == "InProgress", ct),
            pendingApprovals = await db.PlanningWorkflows.CountAsync(x => x.Status == "PendingProjectManagerApproval" && ids.Contains(x.ResourceRequest.ProjectId), ct),
            pendingRequests = await db.ResourceRequests.CountAsync(x => x.Status != "Cancelled" && ids.Contains(x.ProjectId) && !db.PlanningWorkflows.Any(w => w.ResourceRequestId == x.Id && w.Status == "Approved"), ct),
            lowStock = await db.Materials.CountAsync(x => !x.IsArchived && x.CurrentStock - x.ReservedStock <= 0, ct),
            pendingPurchaseRequests = await procurement.PurchaseRequests.CountAsync(x => x.Status == "Pending" && (office || x.ProjectId != null && projectIds.Contains(x.ProjectId.Value)), ct),
            pendingDeliveries = await procurement.Deliveries.CountAsync(x => (x.Status == "Pending" || x.Status == "Received") && procurement.PurchaseOrders.Any(o => o.Id == x.PurchaseOrderId && (office || o.ProjectId != null && projectIds.Contains(o.ProjectId.Value))), ct)
        });
    }
}
