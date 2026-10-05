using System.Security.Claims;
using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Controllers;
[ApiController, Route("api/PurchaseRequests"), Route("api/purchase-requests"), Authorize(Roles = "ProcurementOfficer,ProjectManager,Administrator,SiteEngineer")]
public sealed class PurchaseRequestsController(AppDbContext db, ProcurementService service) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool Office => !User.IsInRole("SiteEngineer") || User.IsInRole("ProjectManager") || User.IsInRole("Administrator") || User.IsInRole("ProcurementOfficer");
    [HttpGet] public async Task<IActionResult> List(string? search, string? status, int page = 1, int pageSize = 100, string sort = "updatedAt", bool desc = true, CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 100) return BadRequest(new { message = "Invalid pagination." });
        var projects = await service.ProjectsAsync(Actor, Office, ct);
        var rows = db.PurchaseRequests.AsNoTracking().Where(x => Office || x.ProjectId != null && projects.Contains(x.ProjectId.Value));
        if (!string.IsNullOrWhiteSpace(search)) rows = rows.Where(x => EF.Functions.ILike(x.MaterialName, $"%{search.Trim()}%"));
        if (!string.IsNullOrWhiteSpace(status)) rows = rows.Where(x => x.Status == status);
        Response.Headers["X-Total-Count"] = (await rows.CountAsync(ct)).ToString();
        var ordered = sort.ToLowerInvariant() switch { "material" => desc ? rows.OrderByDescending(x => x.MaterialName) : rows.OrderBy(x => x.MaterialName), "quantity" => desc ? rows.OrderByDescending(x => x.Quantity) : rows.OrderBy(x => x.Quantity), "status" => desc ? rows.OrderByDescending(x => x.Status) : rows.OrderBy(x => x.Status), _ => desc ? rows.OrderByDescending(x => x.UpdatedAt) : rows.OrderBy(x => x.UpdatedAt) };
        return Ok(await ordered.ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct));
    }
    [HttpGet("{id:int}")] public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var row = await db.PurchaseRequests.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return row == null || !await service.VisibleAsync(row.ProjectId, Actor, Office, ct) ? NotFound() : Ok(row);
    }
    [HttpPost, Authorize(Roles = "ProcurementOfficer,ProjectManager,Administrator")] public async Task<IActionResult> Create(CreatePurchaseRequestRequest dto, CancellationToken ct) => StatusCode(201, await service.WriteRequestAsync(null, dto, Actor, ct));
    [HttpPut("{id:int}"), Authorize(Roles = "ProcurementOfficer,ProjectManager,Administrator")] public async Task<IActionResult> Update(int id, CreatePurchaseRequestRequest dto, CancellationToken ct) => Ok(await service.WriteRequestAsync(id, dto, Actor, ct));
    [HttpPut("{id:int}/approve"), Authorize(Roles = "ProjectManager,Administrator")] public async Task<IActionResult> Approve(int id, CancellationToken ct) => Ok(await service.RequestDecisionAsync(id, true, Actor, ct));
    [HttpPut("{id:int}/links"), Authorize(Roles = "ProjectManager,Administrator")] public async Task<IActionResult> LinkLegacy(int id, LegacyProcurementLinksRequest dto, CancellationToken ct) => Ok(await service.LinkLegacyRequestAsync(id, dto, Actor, ct));
    [HttpDelete("{id:int}"), Authorize(Roles = "ProcurementOfficer,ProjectManager,Administrator")] public async Task<IActionResult> Cancel(int id, CancellationToken ct) => Ok(await service.RequestDecisionAsync(id, false, Actor, ct));
}
