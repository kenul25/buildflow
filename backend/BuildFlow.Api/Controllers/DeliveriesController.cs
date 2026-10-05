using System.Security.Claims;
using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Controllers;
[ApiController, Route("api/Deliveries"), Authorize(Roles = "ProcurementOfficer,ProjectManager,Administrator,SiteEngineer")]
public sealed class DeliveriesController(AppDbContext db, ProcurementService service, IWebHostEnvironment environment) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool Office => User.IsInRole("ProcurementOfficer") || User.IsInRole("ProjectManager") || User.IsInRole("Administrator");
    [HttpGet] public async Task<IActionResult> List(string? search, string? status, int page = 1, int pageSize = 100, string sort = "updatedAt", bool desc = true, CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 100) return BadRequest();
        var projects = await service.ProjectsAsync(Actor, Office, ct);
        var orders = db.PurchaseOrders.Where(x => Office || x.ProjectId != null && projects.Contains(x.ProjectId.Value)).Select(x => x.Id);
        var rows = db.Deliveries.AsNoTracking().Where(x => orders.Contains(x.PurchaseOrderId));
        if (!string.IsNullOrWhiteSpace(search)) rows = rows.Where(x => EF.Functions.ILike(x.MaterialName, $"%{search.Trim()}%"));
        if (!string.IsNullOrWhiteSpace(status)) rows = rows.Where(x => x.Status == status);
        Response.Headers["X-Total-Count"] = (await rows.CountAsync(ct)).ToString();
        var ordered = sort.ToLowerInvariant() switch { "material" => desc ? rows.OrderByDescending(x => x.MaterialName) : rows.OrderBy(x => x.MaterialName), "quantity" => desc ? rows.OrderByDescending(x => x.Quantity) : rows.OrderBy(x => x.Quantity), "status" => desc ? rows.OrderByDescending(x => x.Status) : rows.OrderBy(x => x.Status), _ => desc ? rows.OrderByDescending(x => x.UpdatedAt) : rows.OrderBy(x => x.UpdatedAt) };
        return Ok(await ordered.ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct));
    }
    private async Task<bool> Visible(int id, CancellationToken ct)
    {
        var row = await db.Deliveries.Where(x => x.Id == id).Join(db.PurchaseOrders, x => x.PurchaseOrderId, x => x.Id, (d, o) => new { o.ProjectId }).FirstOrDefaultAsync(ct);
        return row != null && await service.VisibleAsync(row.ProjectId, Actor, Office, ct);
    }
    [HttpGet("{id:int}")] public async Task<IActionResult> Get(int id, CancellationToken ct) => await Visible(id, ct) ? Ok(await db.Deliveries.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)) : NotFound();
    [HttpPost, Authorize(Roles = "ProcurementOfficer,ProjectManager,Administrator")] public async Task<IActionResult> Create(CreateDeliveryRequest dto, CancellationToken ct) => StatusCode(201, await service.WriteDeliveryAsync(null, dto.PurchaseOrderId, dto.Quantity, dto.DeliveryDate, dto.EvidenceUrl, Actor, ct));
    [HttpPut("{id:int}"), Authorize(Roles = "ProcurementOfficer,ProjectManager,Administrator")] public async Task<IActionResult> Update(int id, UpdateDeliveryRequest dto, CancellationToken ct) => Ok(await service.WriteDeliveryAsync(id, 0, dto.Quantity, dto.DeliveryDate, dto.EvidenceUrl, Actor, ct));
    [HttpPut("{id:int}/status")] public async Task<IActionResult> Status(int id, string status, CancellationToken ct) => Ok(await service.DeliveryStatusAsync(id, status, Actor, Office, ct));
    [HttpDelete("{id:int}"), Authorize(Roles = "ProcurementOfficer,ProjectManager,Administrator")] public async Task<IActionResult> Cancel(int id, CancellationToken ct) => Ok(await service.DeliveryStatusAsync(id, "Cancelled", Actor, Office, ct));
    [HttpPost("{id:int}/evidence"), RequestSizeLimit(5_100_000)]
    public async Task<IActionResult> Evidence(int id, IFormFile file, CancellationToken ct)
    {
        if (!await Visible(id, ct)) return NotFound();
        if (file.Length is <= 0 or > 5_000_000 || file.ContentType is not ("image/jpeg" or "image/png" or "image/webp")) return BadRequest(new { message = "Upload a JPEG, PNG or WebP up to 5 MB." });
        using var stream = new MemoryStream(); await file.CopyToAsync(stream, ct); var bytes = stream.ToArray();
        if (!PhotoContent.Valid(bytes, file.ContentType)) return BadRequest(new { message = "Image content does not match its type." });
        await using var tx = await db.Database.BeginTransactionAsync(ct); await service.LockAsync(ct);
        var row = await db.Deliveries.SingleAsync(x => x.Id == id, ct);
        if (row.Status is "Completed" or "Cancelled" or "Rejected") throw new ApiException(409, "terminal_delivery", "Evidence cannot change on a terminal delivery.");
        var directory = Path.Combine(environment.ContentRootPath, "uploads", "delivery-evidence"); Directory.CreateDirectory(directory);
        await System.IO.File.WriteAllBytesAsync(Path.Combine(directory, id.ToString()), bytes, ct);
        row.EvidenceUrl = $"/api/deliveries/{id}/evidence"; row.UpdatedById = Actor; await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return Ok(new { row.EvidenceUrl });
    }
    [HttpGet("{id:int}/evidence")] public async Task<IActionResult> GetEvidence(int id, CancellationToken ct)
    {
        if (!await Visible(id, ct)) return NotFound();
        var path = Path.Combine(environment.ContentRootPath, "uploads", "delivery-evidence", id.ToString());
        if (!System.IO.File.Exists(path)) return NotFound();
        var bytes = await System.IO.File.ReadAllBytesAsync(path, ct);
        return File(bytes, bytes.Length >= 8 && bytes[0] == 137 ? "image/png" : bytes.Length >= 12 && bytes[0] == 82 ? "image/webp" : "image/jpeg");
    }
}
public static class PhotoContent
{
    public static bool Valid(byte[] b, string type) => type switch
    {
        "image/jpeg" => b.Length >= 3 && b[0] == 255 && b[1] == 216 && b[2] == 255,
        "image/png" => b.Length >= 8 && b.AsSpan(0, 8).SequenceEqual(new byte[] { 137,80,78,71,13,10,26,10 }),
        "image/webp" => b.Length >= 12 && System.Text.Encoding.ASCII.GetString(b, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(b, 8, 4) == "WEBP",
        _ => false
    };
}
