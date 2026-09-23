using System.Security.Claims;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Models;
using BuildFlow.Api.Services;
using BuildFlow.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildFlow.Api.Controllers;

[ApiController]
[Route("api/construction")]
[Authorize(Roles = "SiteEngineer,ProjectManager,Administrator")]
public sealed class ConstructionOperationsController(IConstructionOperationsService service, BuildFlowDbContext db, IConfiguration config) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool Manager => User.IsInRole(SystemRoles.ProjectManager) || User.IsInRole(SystemRoles.Administrator);

    [HttpGet("engineers")]
    [Authorize(Roles = "ProjectManager,Administrator")]
    public async Task<IActionResult> Engineers(CancellationToken ct) => Ok(await db.Users.AsNoTracking()
        .Where(u => u.IsActive && u.UserRoles.Any(r => r.Role.Name == SystemRoles.SiteEngineer))
        .OrderBy(u => u.FullName).Select(u => new { u.Id, u.FullName }).ToListAsync(ct));

    [HttpPost("activities/{activityId:guid}/progress")]
    public Task<ProgressDto> Progress(Guid activityId, ProgressWriteDto dto, CancellationToken ct) => service.UpdateProgressAsync(activityId, dto, Actor, Manager, ct);

    [HttpGet("activities/{activityId:guid}/progress")]
    public Task<IReadOnlyList<ProgressDto>> History(Guid activityId, CancellationToken ct) => service.ProgressHistoryAsync(activityId, Actor, Manager, ct);

    [HttpPost("resource-requests")]
    public async Task<ActionResult<ResourceRequestDto>> Submit(ResourceRequestWriteDto dto, CancellationToken ct)
    {
        var result = await service.SubmitRequestAsync(dto, Actor, Manager, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("resource-requests")]
    public Task<IReadOnlyList<ResourceRequestSummaryDto>> Requests(CancellationToken ct) => service.ListRequestsAsync(Actor, Manager, ct);

    [HttpPost("resource-requests/{id:guid}/planning")]
    public Task<WorkflowDto> Plan(Guid id, CancellationToken ct) => service.StartPlanningAsync(id, Actor, Manager, ct);

    [HttpGet("planning-workflows/{id:guid}")]
    public Task<WorkflowDto> Workflow(Guid id, CancellationToken ct) => service.GetWorkflowAsync(id, Actor, Manager, ct);

    [HttpPost("planning-workflows/{id:guid}/agent-results")]
    [AllowAnonymous]
    [RequestSizeLimit(65536)]
    public Task<WorkflowDto> AgentResult(Guid id, AgentResultWriteDto dto, CancellationToken ct)
    {
        var expected = config["Planning:InternalKey"];
        var supplied = Request.Headers["X-BuildFlow-Key"].ToString();
        if (string.IsNullOrEmpty(expected) || supplied.Length != expected.Length ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(supplied), Encoding.UTF8.GetBytes(expected)))
            throw new ApiException(401, "unauthorized", "Invalid internal agent key.");
        return service.ApplyAgentResultAsync(id, dto, ct);
    }

    [HttpPost("activities/{activityId:guid}/photos")]
    [RequestSizeLimit(5_100_000)]
    public async Task<IActionResult> Photo(Guid activityId, IFormFile file, CancellationToken ct)
    {
        var id = await service.UploadPhotoAsync(activityId, file, Actor, Manager, ct);
        return StatusCode(201, new { id });
    }

    [HttpGet("photos/{id:guid}")]
    public async Task<IActionResult> GetPhoto(Guid id, CancellationToken ct)
    {
        var photo = await service.GetPhotoAsync(id, Actor, Manager, ct);
        return File(photo.Content, photo.ContentType);
    }
}
