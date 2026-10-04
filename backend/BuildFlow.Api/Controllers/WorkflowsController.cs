using System.Security.Claims;
using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Controllers;
[ApiController, Route("api/workflows"), Authorize(Roles = "SiteEngineer,ProjectManager,Administrator")]
public sealed class WorkflowsController(WorkflowExecutionService service, IConstructionOperationsService operations, BuildFlowDbContext db) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool Manager => User.IsInRole("ProjectManager") || User.IsInRole("Administrator");
    [HttpGet] public async Task<IActionResult> List(CancellationToken ct) => Ok(await db.PlanningWorkflows.AsNoTracking().Where(x => Manager || x.ResourceRequest.Project.AssignedEngineerId == Actor).OrderByDescending(x => x.CreatedAt).Select(x => new { x.Id, x.ResourceRequestId, x.Status, x.Error, x.CreatedAt, objective = x.ResourceRequest.Objective }).ToListAsync(ct));
    [HttpGet("{id:guid}")] public Task<WorkflowDto> Get(Guid id, CancellationToken ct) => operations.GetWorkflowAsync(id, Actor, Manager, ct);
    [HttpPost("{id:guid}/execute")] public Task<WorkflowDto> Execute(Guid id, CancellationToken ct) => service.ExecuteAsync(id, Actor, Manager, ct);
    [HttpPost("{id:guid}/decision"), Authorize(Roles = "ProjectManager,Administrator")] public Task<WorkflowDto> Decision(Guid id, ApprovalWriteDto dto, CancellationToken ct) => service.DecideAsync(id, dto, Actor, ct);
}
