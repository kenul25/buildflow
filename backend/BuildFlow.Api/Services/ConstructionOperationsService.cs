using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Services;

public interface IPlanningClient
{
    Task<JsonElement> PlanAsync(object request, CancellationToken ct);
}

public sealed class PlanningServiceException(string code, JsonElement? executionSummary = null) : HttpRequestException(code)
{
    public string Code { get; } = code;
    public JsonElement? ExecutionSummary { get; } = executionSummary;
}

public sealed class PlanningClient(HttpClient http, IConfiguration config) : IPlanningClient
{
    public async Task<JsonElement> PlanAsync(object request, CancellationToken ct)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "internal/plans") { Content = JsonContent.Create(request) };
        message.Headers.Add("X-BuildFlow-Key", config["Planning:InternalKey"] ?? throw new InvalidOperationException("Planning:InternalKey is required."));
        using var response = await http.SendAsync(message, ct);
        if (!response.IsSuccessStatusCode)
        {
            try
            {
                var failure = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
                var code = failure.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String
                    ? error.GetString() ?? "planning_service_error" : "planning_service_error";
                var summary = failure.TryGetProperty("executionSummary", out var item) && item.ValueKind == JsonValueKind.Object
                    ? item.Clone() : (JsonElement?)null;
                throw new PlanningServiceException(code, summary);
            }
            catch (JsonException)
            {
                throw new PlanningServiceException("planning_service_error");
            }
        }
        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
    }
}

public interface IConstructionOperationsService
{
    Task<ProgressDto> UpdateProgressAsync(Guid activityId, ProgressWriteDto dto, Guid actorId, bool manager, CancellationToken ct);
    Task<IReadOnlyList<ProgressDto>> ProgressHistoryAsync(Guid activityId, Guid actorId, bool manager, CancellationToken ct);
    Task<ResourceRequestDto> SubmitRequestAsync(ResourceRequestWriteDto dto, Guid actorId, bool manager, CancellationToken ct);
    Task<IReadOnlyList<ResourceRequestSummaryDto>> ListRequestsAsync(Guid actorId, bool manager, CancellationToken ct);
    Task<WorkflowDto> StartPlanningAsync(Guid requestId, Guid actorId, bool manager, CancellationToken ct);
    Task<WorkflowDto> GetWorkflowAsync(Guid id, Guid actorId, bool manager, CancellationToken ct);
    Task<WorkflowDto> ApplyAgentResultAsync(Guid id, AgentResultWriteDto result, CancellationToken ct);
    Task<(byte[] Content, string ContentType)> GetPhotoAsync(Guid id, Guid actorId, bool manager, CancellationToken ct);
    Task<Guid> UploadPhotoAsync(Guid activityId, IFormFile file, Guid actorId, bool manager, CancellationToken ct);
}

public sealed class ConstructionOperationsService(BuildFlowDbContext db, IPlanningClient planner, IWebHostEnvironment environment) : IConstructionOperationsService
{
    private static ApiException Missing() => new(404, "not_found", "The record was not found.");
    private async Task<ConstructionActivity> ActivityAsync(Guid id, Guid actor, bool manager, CancellationToken ct)
    {
        var activity = await db.ConstructionActivities.Include(x => x.Phase).ThenInclude(x => x.Site).ThenInclude(x => x.Project).FirstOrDefaultAsync(x => x.Id == id && !x.IsArchived, ct) ?? throw Missing();
        if (activity.Phase.IsArchived || activity.Phase.Site.IsArchived || activity.Phase.Site.Project.IsArchived || !manager && activity.Phase.Site.Project.AssignedEngineerId != actor) throw Missing();
        return activity;
    }

    public async Task<ProgressDto> UpdateProgressAsync(Guid activityId, ProgressWriteDto dto, Guid actorId, bool manager, CancellationToken ct)
    {
        var activity = await ActivityAsync(activityId, actorId, manager, ct);
        if (dto.ProgressPercent < activity.ProgressPercent) throw new ApiException(409, "progress_regression", "Progress cannot decrease.");
        var update = new ProgressUpdate { Id = Guid.NewGuid(), ActivityId = activityId, ProgressPercent = dto.ProgressPercent, WorkCompleted = dto.WorkCompleted.Trim(), Blockers = dto.Blockers?.Trim(), SubmittedById = actorId };
        activity.ProgressPercent = dto.ProgressPercent;
        activity.Status = dto.ProgressPercent == 100 ? "Completed" : dto.ProgressPercent > 0 ? "InProgress" : "Planned";
        activity.UpdatedById = actorId;
        db.ProgressUpdates.Add(update);
        await db.SaveChangesAsync(ct);
        return new(update.Id, activityId, update.ProgressPercent, update.WorkCompleted, update.Blockers, actorId, update.CreatedAt);
    }

    public async Task<IReadOnlyList<ProgressDto>> ProgressHistoryAsync(Guid activityId, Guid actorId, bool manager, CancellationToken ct)
    {
        await ActivityAsync(activityId, actorId, manager, ct);
        return await db.ProgressUpdates.AsNoTracking().Where(x => x.ActivityId == activityId).OrderByDescending(x => x.CreatedAt)
            .Select(x => new ProgressDto(x.Id, x.ActivityId, x.ProgressPercent, x.WorkCompleted, x.Blockers, x.SubmittedById, x.CreatedAt)).ToListAsync(ct);
    }

    public async Task<ResourceRequestDto> SubmitRequestAsync(ResourceRequestWriteDto dto, Guid actorId, bool manager, CancellationToken ct)
    {
        var activity = await ActivityAsync(dto.ActivityId, actorId, manager, ct);
        if (activity.Phase.SiteId != dto.SiteId || activity.Phase.Site.ProjectId != dto.ProjectId) throw new ApiException(400, "invalid_relationship", "Project, site and activity must belong to the same hierarchy.");
        if (dto.Items.Count == 0 || dto.Items.Count > 50) throw new ApiException(400, "invalid_items", "Provide between 1 and 50 resource items.");
        if (dto.Items.Any(i => i.Kind is not ("Material" or "Equipment" or "Workforce") || string.IsNullOrWhiteSpace(i.Name) || i.Name.Length > 160 || i.Quantity <= 0 || i.Quantity > 999999999 || string.IsNullOrWhiteSpace(i.Unit) || i.Unit.Length > 32))
            throw new ApiException(400, "invalid_items", "Each resource needs a valid type, name, quantity and unit.");
        if (dto.RequiredBy < DateOnly.FromDateTime(DateTime.UtcNow)) throw new ApiException(400, "invalid_date", "Required date cannot be in the past.");
        var request = new ResourceRequest { Id = Guid.NewGuid(), ProjectId = dto.ProjectId, SiteId = dto.SiteId, ActivityId = dto.ActivityId, Objective = dto.Objective.Trim(), RequiredBy = dto.RequiredBy, BudgetLimit = dto.BudgetLimit, Notes = dto.Notes?.Trim(), SubmittedById = actorId,
            Items = dto.Items.Select(i => new ResourceRequestItem { Id = Guid.NewGuid(), Kind = i.Kind, Name = i.Name.Trim(), Quantity = i.Quantity, Unit = i.Unit.Trim() }).ToList() };
        db.ResourceRequests.Add(request);
        await db.SaveChangesAsync(ct);
        return Map(request);
    }

    public async Task<IReadOnlyList<ResourceRequestSummaryDto>> ListRequestsAsync(Guid actorId, bool manager, CancellationToken ct)
    {
        var query = db.ResourceRequests.AsNoTracking();
        if (!manager) query = query.Where(r => r.Project.AssignedEngineerId == actorId);
        return await query.OrderByDescending(r => r.CreatedAt).Take(100).Select(r => new ResourceRequestSummaryDto(
            r.Id, r.ProjectId, r.ActivityId, r.Objective, r.CreatedAt,
            db.PlanningWorkflows.Where(w => w.ResourceRequestId == r.Id).Select(w => (Guid?)w.Id).FirstOrDefault(),
            db.PlanningWorkflows.Where(w => w.ResourceRequestId == r.Id).Select(w => w.Status).FirstOrDefault()
        )).ToListAsync(ct);
    }

    public async Task<WorkflowDto> StartPlanningAsync(Guid requestId, Guid actorId, bool manager, CancellationToken ct)
    {
        var request = await db.ResourceRequests.Include(r => r.Items).Include(r => r.Project).Include(r => r.Site).Include(r => r.Activity).FirstOrDefaultAsync(r => r.Id == requestId, ct) ?? throw Missing();
        await ActivityAsync(request.ActivityId, actorId, manager, ct);
        var existing = await db.PlanningWorkflows.FirstOrDefaultAsync(w => w.ResourceRequestId == requestId, ct);
        if (existing is not null && existing.Status != "Failed") return Map(existing);
        var priorHistory = ReadExecutionHistory(existing?.PlanJson, existing?.Error);
        var workflow = existing ?? new PlanningWorkflow { Id = Guid.NewGuid(), ResourceRequestId = requestId };
        workflow.Status = "Queued";
        workflow.Error = null;
        workflow.PlanJson = null;
        workflow.CompletedAt = null;
        if (existing is null) db.PlanningWorkflows.Add(workflow);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) when (existing is null)
        {
            db.Entry(workflow).State = EntityState.Detached;
            var concurrent = await db.PlanningWorkflows.AsNoTracking().FirstOrDefaultAsync(w => w.ResourceRequestId == requestId, ct);
            if (concurrent is not null) return Map(concurrent);
            throw;
        }
        var attemptStarted = DateTimeOffset.UtcNow;
        try
        {
            var result = await planner.PlanAsync(new { workflowId = workflow.Id, requestId = request.Id, request.ProjectId, request.SiteId, request.ActivityId, request.Objective, request.RequiredBy, request.BudgetLimit,
                projectName = request.Project.Name, siteName = request.Site.Name, siteAddress = request.Site.Address, activityName = request.Activity.Name, activityDueDate = request.Activity.DueDate,
                items = request.Items.Select(i => new { i.Kind, i.Name, i.Quantity, i.Unit }) }, ct);
            if (!result.TryGetProperty("schemaVersion", out var version) || version.GetString() != "1.0" ||
                !result.TryGetProperty("workflowId", out var workflowId) || workflowId.GetString() != workflow.Id.ToString() ||
                !result.TryGetProperty("status", out var status) || status.GetString() != "AwaitingAgents" ||
                !result.TryGetProperty("approvalRequired", out var approval) || approval.ValueKind != JsonValueKind.True ||
                !result.TryGetProperty("tasks", out var tasks) || tasks.ValueKind != JsonValueKind.Array || tasks.GetArrayLength() != 3)
                throw new JsonException("Planning response did not match the contract.");
            var expectedAgents = new[] { "InventoryAgent", "ProcurementAgent", "SchedulingValidationAgent" };
            var taskIds = new Guid[3];
            for (var index = 0; index < expectedAgents.Length; index++)
            {
                var task = tasks[index];
                if (!task.TryGetProperty("agent", out var agent) || agent.GetString() != expectedAgents[index] ||
                    !task.TryGetProperty("status", out var taskStatus) || taskStatus.GetString() != "Pending" ||
                    !task.TryGetProperty("task_id", out var taskId) || !Guid.TryParse(taskId.GetString(), out taskIds[index]) ||
                    !task.TryGetProperty("depends_on", out var dependencies) || dependencies.ValueKind != JsonValueKind.Array ||
                    dependencies.GetArrayLength() != index)
                    throw new JsonException("Planning tasks did not match the contract.");
                for (var predecessor = 0; predecessor < index; predecessor++)
                    if (dependencies[predecessor].GetString() != taskIds[predecessor].ToString())
                        throw new JsonException("Planning task dependencies did not match the contract.");
            }
            var acceptedPlan = JsonNode.Parse(result.GetRawText())!.AsObject();
            if (priorHistory.Count > 0) acceptedPlan["executionHistory"] = priorHistory;
            workflow.PlanJson = acceptedPlan.ToJsonString();
            workflow.Status = "AwaitingAgents";
        }
        catch (PlanningServiceException ex)
        {
            workflow.Status = "Failed";
            workflow.Error = ex.Code.Length <= 120 ? ex.Code : "planning_service_error";
            workflow.CompletedAt = DateTimeOffset.UtcNow;
            var summary = ValidatedFailureSummary(ex.ExecutionSummary, workflow.Id)
                ?? LocalFailureSummary(workflow.Id, attemptStarted, workflow.CompletedAt.Value, workflow.Error);
            workflow.PlanJson = FailureEnvelope(workflow.Id, summary, priorHistory);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            workflow.Status = "Failed";
            workflow.Error = ex is TaskCanceledException ? "planning_timeout" : "planning_service_error";
            workflow.CompletedAt = DateTimeOffset.UtcNow;
            workflow.PlanJson = FailureEnvelope(workflow.Id,
                LocalFailureSummary(workflow.Id, attemptStarted, workflow.CompletedAt.Value, workflow.Error), priorHistory);
        }
        await db.SaveChangesAsync(ct);
        return Map(workflow);
    }

    public async Task<WorkflowDto> GetWorkflowAsync(Guid id, Guid actorId, bool manager, CancellationToken ct)
    {
        var workflow = await db.PlanningWorkflows.Include(w => w.ResourceRequest).AsNoTracking().FirstOrDefaultAsync(w => w.Id == id, ct) ?? throw Missing();
        await ActivityAsync(workflow.ResourceRequest.ActivityId, actorId, manager, ct);
        return Map(workflow);
    }

    public async Task<WorkflowDto> ApplyAgentResultAsync(Guid id, AgentResultWriteDto result, CancellationToken ct)
    {
        if (result.SchemaVersion != "1.0" || result.Status is not ("Completed" or "Failed") || result.Output.ValueKind != JsonValueKind.Object)
            throw new ApiException(400, "invalid_agent_result", "Agent result does not match version 1.0.");
        var workflow = await db.PlanningWorkflows.FirstOrDefaultAsync(w => w.Id == id, ct) ?? throw Missing();
        if (workflow.Status != "AwaitingAgents" || workflow.PlanJson is null)
            throw new ApiException(409, "invalid_workflow_state", "Workflow is not awaiting agent results.");
        var plan = JsonNode.Parse(workflow.PlanJson)!.AsObject();
        var tasks = plan["tasks"]!.AsArray();
        var task = tasks.Select(x => x!.AsObject()).FirstOrDefault(x => x["task_id"]?.GetValue<string>() == result.TaskId.ToString());
        if (task is null || task["agent"]?.GetValue<string>() != result.Agent || task["status"]?.GetValue<string>() != "Pending")
            throw new ApiException(400, "invalid_agent_result", "Task ID, agent or task state does not match the workflow.");
        var completed = tasks.Where(x => x!["status"]?.GetValue<string>() == "Completed").Select(x => x!["task_id"]!.GetValue<string>()).ToHashSet();
        if (task["depends_on"]!.AsArray().Any(x => !completed.Contains(x!.GetValue<string>())))
            throw new ApiException(409, "task_dependencies", "Complete predecessor tasks first.");
        task["status"] = result.Status;
        task["output"] = JsonNode.Parse(result.Output.GetRawText());
        task["error"] = result.Error;
        if (result.Status == "Failed")
        {
            workflow.Status = "Failed";
            workflow.Error = result.Error ?? "An external agent failed.";
            workflow.CompletedAt = DateTimeOffset.UtcNow;
        }
        else if (tasks.All(x => x!["status"]?.GetValue<string>() == "Completed"))
            workflow.Status = "AwaitingBackendValidation";
        plan["status"] = workflow.Status;
        workflow.PlanJson = plan.ToJsonString();
        await db.SaveChangesAsync(ct);
        return Map(workflow);
    }

    public async Task<Guid> UploadPhotoAsync(Guid activityId, IFormFile file, Guid actorId, bool manager, CancellationToken ct)
    {
        await ActivityAsync(activityId, actorId, manager, ct);
        if (file.Length is <= 0 or > 5_000_000 || file.ContentType is not ("image/jpeg" or "image/png" or "image/webp")) throw new ApiException(400, "invalid_photo", "Upload a JPEG, PNG or WebP image up to 5 MB.");
        await using var input = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer, ct);
        var bytes = buffer.ToArray();
        var valid = file.ContentType switch
        {
            "image/jpeg" => bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
            "image/png" => bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137,80,78,71,13,10,26,10 }),
            _ => bytes.Length >= 12 && System.Text.Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP"
        };
        if (!valid) throw new ApiException(400, "invalid_photo", "Image content does not match its type.");
        var id = Guid.NewGuid();
        var directory = Path.Combine(environment.ContentRootPath, "uploads", "site-photos");
        Directory.CreateDirectory(directory);
        var name = id.ToString("N");
        await File.WriteAllBytesAsync(Path.Combine(directory, name), bytes, ct);
        db.SitePhotos.Add(new SitePhoto { Id = id, ActivityId = activityId, StorageName = name, OriginalName = Path.GetFileName(file.FileName)[..Math.Min(Path.GetFileName(file.FileName).Length, 255)], ContentType = file.ContentType, Length = bytes.Length, UploadedById = actorId });
        await db.SaveChangesAsync(ct);
        return id;
    }

    public async Task<(byte[] Content, string ContentType)> GetPhotoAsync(Guid id, Guid actorId, bool manager, CancellationToken ct)
    {
        var photo = await db.SitePhotos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw Missing();
        await ActivityAsync(photo.ActivityId, actorId, manager, ct);
        return (await File.ReadAllBytesAsync(Path.Combine(environment.ContentRootPath, "uploads", "site-photos", photo.StorageName), ct), photo.ContentType);
    }

    private static ResourceRequestDto Map(ResourceRequest r) => new(r.Id,r.ProjectId,r.SiteId,r.ActivityId,r.Objective,r.RequiredBy,r.BudgetLimit,r.Notes,r.Items.Select(i => new ResourceItemDto(i.Kind,i.Name,i.Quantity,i.Unit)).ToList(),r.SubmittedById,r.CreatedAt);

    private static JsonArray ReadExecutionHistory(string? previousPlanJson, string? previousError)
    {
        var history = new JsonArray();
        if (previousPlanJson is null) return history;
        try
        {
            using var document = JsonDocument.Parse(previousPlanJson);
            var root = document.RootElement;
            if (root.TryGetProperty("executionHistory", out var older) && older.ValueKind == JsonValueKind.Array)
                foreach (var entry in older.EnumerateArray()) history.Add(JsonNode.Parse(entry.GetRawText()));
            if (root.TryGetProperty("executionSummary", out var summary) && summary.ValueKind == JsonValueKind.Object)
                history.Add(JsonNode.Parse(summary.GetRawText()));
            if (!string.IsNullOrWhiteSpace(previousError))
                history.Add(new JsonObject { ["workflowStatus"] = "Failed", ["error"] = previousError });
        }
        catch (JsonException) { /* Keep the new attempt recoverable if old audit JSON is damaged. */ }
        return history;
    }

    private static JsonNode? ValidatedFailureSummary(JsonElement? candidate, Guid workflowId)
    {
        if (candidate is not { ValueKind: JsonValueKind.Object } summary) return null;
        if (!summary.TryGetProperty("agentName", out var name) || name.GetString() != "PlanningAgent" ||
            !summary.TryGetProperty("workflowId", out var id) || id.GetString() != workflowId.ToString() ||
            !summary.TryGetProperty("finalPlanningStatus", out var status) || status.GetString() != "Failed") return null;
        return JsonNode.Parse(summary.GetRawText());
    }

    private static JsonNode LocalFailureSummary(Guid workflowId, DateTimeOffset started, DateTimeOffset completed, string code) =>
        JsonSerializer.SerializeToNode(new
        {
            agentName = "PlanningAgent", workflowId, model = "gemini-3.5-flash-lite", generatedPlan = (object?)null,
            validationResult = new { schemaValid = false, businessRulesValid = false, accepted = false },
            startedAt = started, completedAt = completed, durationMs = Math.Max(0, (int)(completed - started).TotalMilliseconds),
            retryCount = 0, errors = new[] { code }, finalPlanningStatus = "Failed"
        })!;

    private static string FailureEnvelope(Guid workflowId, JsonNode summary, JsonArray history)
    {
        var envelope = new JsonObject
        {
            ["schemaVersion"] = "1.0", ["workflowId"] = workflowId.ToString(), ["status"] = "Failed",
            ["executionSummary"] = summary
        };
        if (history.Count > 0) envelope["executionHistory"] = history;
        return envelope.ToJsonString();
    }
    private static WorkflowDto Map(PlanningWorkflow w)
    {
        JsonElement? plan = null;
        if (w.PlanJson is not null)
        {
            using var document = JsonDocument.Parse(w.PlanJson);
            plan = document.RootElement.Clone();
        }
        return new(w.Id,w.ResourceRequestId,w.Status,plan,w.Error,w.CreatedAt,w.CompletedAt);
    }
}
