using System.Text.Json;
using System.Text.Json.Nodes;
using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Services;

public sealed class WorkflowExecutionService(BuildFlowDbContext db, IPlanningClient client, IConstructionOperationsService operations, SchedulingService scheduling, InventoryService inventory)
{
    private static JsonNode Node(object value) => JsonSerializer.SerializeToNode(value, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    private static ApiException Invalid(string message) => new(409, "invalid_proposal", message);
    private async Task<PlanningWorkflow> Workflow(Guid id, Guid actor, bool manager, CancellationToken ct)
    {
        var row = await db.PlanningWorkflows.Include(x => x.ResourceRequest).ThenInclude(x => x.Items).SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ApiException(404, "not_found", "Workflow not found.");
        if (!await scheduling.CanAccessActivityAsync(row.ResourceRequest.ActivityId, actor, manager, ct)) throw new ApiException(404, "not_found", "Workflow not found.");
        return row;
    }
    public async Task<WorkflowDto> ExecuteAsync(Guid id, Guid actor, bool manager, CancellationToken ct)
    {
        await inventory.RefreshExpiryAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct); await scheduling.LockAsync(ct);
        var workflow = await Workflow(id, actor, manager, ct);
        if (workflow.Status != "AwaitingAgents") throw Invalid("Workflow is not awaiting agents.");
        var plan = JsonNode.Parse(workflow.PlanJson!)!.AsObject();
        try
        {
            foreach (var task in plan["tasks"]!.AsArray().Select(x => x!.AsObject()))
            {
                if (task["status"]!.GetValue<string>() == "Completed") continue;
                var input = task["input"]!.AsObject();
                var prior = plan["tasks"]!.AsArray().Where(x => x!["status"]!.GetValue<string>() == "Completed").ToList();
                input["items"] = Node(workflow.ResourceRequest.Items.Select(x => new { x.Kind, x.Name, x.Quantity, x.Unit, x.ResourceCount }));
                input["inventorySnapshot"] = Node(await db.Materials.AsNoTracking().Where(x => !x.IsArchived).Select(x => new { materialId = x.Id, x.Name, x.Unit, x.CurrentStock, x.ReservedStock }).ToListAsync(ct));
                input["inventoryResult"] = prior.FirstOrDefault(x => x!["agent"]!.GetValue<string>() == "InventoryAgent")?["output"]?.DeepClone();
                input["procurementResult"] = prior.FirstOrDefault(x => x!["agent"]!.GetValue<string>() == "ProcurementAgent")?["output"]?.DeepClone();
                input["quotations"] = Node(await db.Set<SupplierQuotation>().Where(x => x.IsActive && x.Supplier.IsActive && x.ValidUntil != null).Join(db.Set<SupplierMaterial>(), q => new { q.SupplierId, q.MaterialId }, s => new { s.SupplierId, MaterialId = (Guid?)s.MaterialId }, (q, s) => new { quotationId = q.Id, q.SupplierId, supplierName = q.Supplier.Name, q.MaterialId, q.MaterialName, q.Unit, q.Quantity, q.UnitPrice, q.DeliveryDate, q.ValidUntil, isActive = s.IsActive, s.AvailableQuantity }).ToListAsync(ct));
                foreach (var quote in input["quotations"]!.AsArray())
                {
                    var supplier = quote!["supplierId"]!.GetValue<int>(); var material = Guid.Parse(quote["materialId"]!.GetValue<string>());
                    var supply = await db.Set<SupplierMaterial>().SingleAsync(x => x.SupplierId == supplier && x.MaterialId == material, ct);
                    quote["availableQuantity"] = await ProcurementService.RemainingSupplyAsync(supply, db.Set<PurchaseOrder>(), db.Set<Delivery>(), null, ct);
                }
                input["schedulingSnapshot"] = Node(new
                {
                    now = DateTimeOffset.UtcNow,
                    workers = await db.Set<Worker>().AsNoTracking().Where(x => !x.IsArchived && x.IsActive).ToListAsync(ct),
                    skills = await db.Set<Skill>().AsNoTracking().Where(x => !x.IsArchived).ToListAsync(ct),
                    workerSkills = await db.Set<WorkerSkill>().AsNoTracking().Where(x => !x.IsArchived).ToListAsync(ct),
                    shifts = await db.Set<Shift>().AsNoTracking().Where(x => !x.IsArchived && x.EndTime > DateTimeOffset.UtcNow).ToListAsync(ct),
                    equipment = await db.Set<Equipment>().AsNoTracking().Where(x => !x.IsArchived).ToListAsync(ct),
                    assignments = await db.Set<WorkerAssignment>().AsNoTracking().Where(x => !x.IsArchived && x.Status != "Cancelled").ToListAsync(ct),
                    reservations = await db.Set<EquipmentReservation>().AsNoTracking().Where(x => !x.IsArchived && x.Status != "Cancelled").ToListAsync(ct)
                });
                var result = await client.ExecuteAsync(task, ct);
                var dto = JsonSerializer.Deserialize<AgentResultWriteDto>(result.GetRawText(), new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? throw Invalid("Agent response is empty.");
                await operations.ApplyAgentResultAsync(id, dto, ct);
                task["status"] = dto.Status; task["output"] = JsonNode.Parse(dto.Output.GetRawText()); task["error"] = dto.Error;
                if (dto.Status == "Failed") break;
            }
            if (workflow.Status == "AwaitingBackendValidation")
            {
                await ValidateAsync(workflow, ct); workflow.Status = "PendingProjectManagerApproval";
                var finalPlan = JsonNode.Parse(workflow.PlanJson!)!.AsObject(); finalPlan["status"] = workflow.Status; finalPlan["backendValidation"] = Node(new { isValid = true, checkedAt = DateTimeOffset.UtcNow }); workflow.PlanJson = finalPlan.ToJsonString();
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or ApiException or FormatException or InvalidOperationException)
        {
            workflow.Status = "Failed"; workflow.Error = ex is ApiException ? ex.Message : "Agent execution failed safely; retry planning."; workflow.CompletedAt = DateTimeOffset.UtcNow;
        }
        await db.SaveChangesAsync(CancellationToken.None); await tx.CommitAsync(CancellationToken.None);
        return await operations.GetWorkflowAsync(id, actor, manager, CancellationToken.None);
    }

    private static JsonObject Output(PlanningWorkflow workflow, string agent) => JsonNode.Parse(workflow.PlanJson!)!["tasks"]!.AsArray().Select(x => x!.AsObject()).Single(x => x["agent"]!.GetValue<string>() == agent)["output"]!.AsObject();
    public async Task ValidateAsync(PlanningWorkflow workflow, CancellationToken ct)
    {
        try { await ValidateProposalAsync(workflow, ct); }
        catch (Exception ex) when (ex is JsonException or FormatException or InvalidOperationException or ArgumentException or NullReferenceException or KeyNotFoundException or OverflowException)
        { throw Invalid("Proposal schema is invalid. Request a revised plan."); }
    }
    private async Task ValidateProposalAsync(PlanningWorkflow workflow, CancellationToken ct)
    {
        var request = workflow.ResourceRequest;
        var proposal = Output(workflow, "SchedulingValidationAgent");
        if (proposal["status"]?.GetValue<string>() != "READY_FOR_APPROVAL" || proposal["validation"]?["isValid"]?.GetValue<bool>() != true || proposal["activityId"]?.GetValue<string>() != request.ActivityId.ToString()) throw Invalid("Scheduling proposal is not valid for this activity.");
        var start = DateTimeOffset.Parse(proposal["startTime"]!.GetValue<string>()).ToUniversalTime(); var end = DateTimeOffset.Parse(proposal["endTime"]!.GetValue<string>()).ToUniversalTime();
        if (start < DateTimeOffset.UtcNow) throw Invalid("Proposed start time has passed. Choose a future schedule start before approving.");
        if (request.RequiredBy is DateOnly required && DateOnly.FromDateTime(end.UtcDateTime) > required) throw Invalid("Proposed schedule exceeds the required date. Choose an earlier start or request a revised plan.");
        var requiredHours = request.Items.Where(x => x.Kind != "Material").Select(ResourceUsage.Hours).DefaultIfEmpty(1).Max();
        if ((end - start).TotalSeconds + 1 < (double)(requiredHours * 3600)) throw Invalid("Proposed schedule does not cover the requested resource usage.");
        await scheduling.ValidateScheduleAsync(new WorkSchedule { ActivityId = request.ActivityId, StartTime = start, EndTime = end }, ct);
        var workers = proposal["workers"]!.AsArray(); var equipment = proposal["equipment"]!.AsArray();
        if (workers.Select(x => x!["workerId"]!.GetValue<string>()).Distinct().Count() != workers.Count || equipment.Select(x => x!["equipmentId"]!.GetValue<string>()).Distinct().Count() != equipment.Count) throw Invalid("Proposal contains duplicate resources.");
        var skills = await db.Set<Skill>().Where(x => !x.IsArchived).ToListAsync(ct);
        foreach (var item in request.Items.Where(x => x.Kind == "Workforce").GroupBy(x => x.Name.ToUpperInvariant()))
        {
            var skill = skills.SingleOrDefault(x => x.Name.Equals(item.Key, StringComparison.OrdinalIgnoreCase)) ?? throw Invalid("Required skill is missing.");
            if (workers.Count(x => x!["requiredSkillId"]?.GetValue<string>() == skill.Id.ToString()) != item.Sum(ResourceUsage.Count)) throw Invalid("Workforce quantities do not match the request.");
        }
        if (workers.Count != request.Items.Where(x => x.Kind == "Workforce").Sum(ResourceUsage.Count)) throw Invalid("Unexpected worker allocations.");
        foreach (var worker in workers)
        {
            var workerId = Guid.Parse(worker!["workerId"]!.GetValue<string>()); var skillId = Guid.Parse(worker["requiredSkillId"]!.GetValue<string>());
            if (!await db.Set<Worker>().AnyAsync(x => x.Id == workerId && x.IsActive && !x.IsArchived, ct) || !await db.Set<WorkerSkill>().AnyAsync(x => x.WorkerId == workerId && x.SkillId == skillId && !x.IsArchived, ct) || !await db.Set<Shift>().AnyAsync(x => x.WorkerId == workerId && !x.IsArchived && x.StartTime <= start && x.EndTime >= end, ct) || await db.Set<WorkerAssignment>().AnyAsync(x => x.WorkerId == workerId && !x.IsArchived && x.Status != "Cancelled" && x.StartTime < end && start < x.EndTime, ct)) throw Invalid("Worker skill, shift or availability changed.");
        }
        var allocated = new List<Equipment>();
        foreach (var item in equipment)
        {
            var equipmentId = Guid.Parse(item!["equipmentId"]!.GetValue<string>());
            var record = await db.Set<Equipment>().SingleOrDefaultAsync(x => x.Id == equipmentId && !x.IsArchived && x.Status == "Operational", ct) ?? throw Invalid("Equipment is not operational.");
            if (await db.Set<EquipmentReservation>().AnyAsync(x => x.EquipmentId == equipmentId && !x.IsArchived && x.Status != "Cancelled" && x.StartTime < end && start < x.EndTime, ct)) throw Invalid("Equipment availability changed."); allocated.Add(record);
        }
        foreach (var requirement in request.Items.Where(x => x.Kind == "Equipment").GroupBy(x => x.Name.ToUpperInvariant()))
        {
            var matching = allocated.Where(x => x.Name.Equals(requirement.Key, StringComparison.OrdinalIgnoreCase) || x.Category.Equals(requirement.Key, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matching.Count < requirement.Sum(ResourceUsage.Count)) throw Invalid("Equipment quantities do not match the request.");
            foreach (var x in matching.Take((int)requirement.Sum(ResourceUsage.Count))) allocated.Remove(x);
        }
        if (allocated.Count > 0) throw Invalid("Unexpected equipment allocations.");
        var recommendations = Output(workflow, "ProcurementAgent")["recommendations"]!.AsArray(); decimal total = 0;
        var requestedMaterials = request.Items.Where(x => x.Kind == "Material").GroupBy(x => new { Name = x.Name.ToUpperInvariant(), Unit = x.Unit.ToUpperInvariant() }).ToDictionary(x => x.Key, x => x.Sum(i => i.Quantity));
        var recommendedKeys = recommendations.Select(x => new { Name = x!["materialName"]!.GetValue<string>().ToUpperInvariant(), Unit = x["unit"]!.GetValue<string>().ToUpperInvariant() }).ToList();
        if (recommendedKeys.Any(x => !requestedMaterials.ContainsKey(x)) || recommendedKeys.Distinct().Count() != recommendedKeys.Count) throw Invalid("Procurement contains duplicate or unrequested materials.");
        foreach (var recommendation in recommendations)
        {
            var selected = recommendation!["recommendedSupplier"]!; var id = selected["quotationId"]!.GetValue<int>(); var qty = recommendation["shortageQuantity"]!.GetValue<decimal>();
            var quote = await db.Set<SupplierQuotation>().Include(x => x.Supplier).SingleOrDefaultAsync(x => x.Id == id && x.IsActive, ct) ?? throw Invalid("Quotation not found.");
            var supply = await db.Set<SupplierMaterial>().SingleOrDefaultAsync(x => x.SupplierId == quote.SupplierId && x.MaterialId == quote.MaterialId && x.IsActive, ct);
            if (!quote.Supplier.IsActive || quote.ValidUntil == null || quote.ValidUntil < DateTime.UtcNow || quote.DeliveryDate > start.UtcDateTime || quote.DeliveryDate < DateTime.UtcNow || quote.Quantity < qty || supply == null || await ProcurementService.RemainingSupplyAsync(supply, db.Set<PurchaseOrder>(), db.Set<Delivery>(), null, ct) < qty || qty <= 0 || selected["unitPrice"]!.GetValue<decimal>() != quote.UnitPrice || !quote.MaterialName.Equals(recommendation["materialName"]!.GetValue<string>(), StringComparison.OrdinalIgnoreCase) || !quote.Unit.Equals(recommendation["unit"]!.GetValue<string>(), StringComparison.OrdinalIgnoreCase) || !await db.Materials.AnyAsync(x => x.Id == quote.MaterialId && !x.IsArchived && x.Name == quote.MaterialName && x.Unit == quote.Unit, ct)) throw Invalid("Quotation validity, price, availability or delivery changed.");
            total += quote.UnitPrice * qty;
        }
        if (request.BudgetLimit is decimal budget && total > budget) throw Invalid("Procurement exceeds the request budget.");
        foreach (var group in request.Items.Where(x => x.Kind == "Material").GroupBy(x => new { Name = x.Name.ToUpperInvariant(), Unit = x.Unit.ToUpperInvariant() }))
        {
            var available = await db.Materials.Where(x => !x.IsArchived && x.Name.ToUpper() == group.Key.Name && x.Unit.ToUpper() == group.Key.Unit).SumAsync(x => x.CurrentStock - x.ReservedStock, ct);
            var purchased = recommendations.Where(x => x!["materialName"]!.GetValue<string>().ToUpperInvariant() == group.Key.Name && x["unit"]!.GetValue<string>().ToUpperInvariant() == group.Key.Unit).Sum(x => x!["shortageQuantity"]!.GetValue<decimal>());
            if (purchased > group.Sum(x => x.Quantity)) throw Invalid("Procurement exceeds requested material quantity.");
            if (available + purchased < group.Sum(x => x.Quantity)) throw Invalid("Material shortage is not fully covered.");
        }
    }

    public async Task<WorkflowDto> DecideAsync(Guid id, ApprovalWriteDto dto, Guid actor, CancellationToken ct)
    {
        await inventory.RefreshExpiryAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct); await scheduling.LockAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(42003001)", ct); await inventory.InventoryLockAsync(ct);
        var workflow = await Workflow(id, actor, true, ct);
        if (workflow.Status != "PendingProjectManagerApproval") throw Invalid("Workflow is not awaiting manager approval.");
        if (dto.Decision is not ("Approved" or "Rejected" or "RevisionRequested") || string.IsNullOrWhiteSpace(dto.Reason)) throw Invalid("Choose an approval decision and provide a reason.");
        if (dto.Decision == "Approved")
        {
            if (dto.ScheduleStart is DateTimeOffset selectedStart)
            {
                var adjustedPlan = JsonNode.Parse(workflow.PlanJson!)!.AsObject();
                var adjustedProposal = adjustedPlan["tasks"]!.AsArray().Select(x => x!.AsObject()).Single(x => x["agent"]!.GetValue<string>() == "SchedulingValidationAgent")["output"]!.AsObject();
                var originalStart = DateTimeOffset.Parse(adjustedProposal["startTime"]!.GetValue<string>());
                var originalEnd = DateTimeOffset.Parse(adjustedProposal["endTime"]!.GetValue<string>());
                if (originalEnd <= originalStart) throw Invalid("Proposed schedule duration is invalid. Request a revised plan.");
                var newStart = selectedStart.ToUniversalTime();
                var newEnd = newStart + (originalEnd - originalStart);
                adjustedProposal["startTime"] = newStart.ToString("O");
                adjustedProposal["endTime"] = newEnd.ToString("O");
                adjustedPlan["scheduleAdjustment"] = Node(new { originalStart, originalEnd, startTime = newStart, endTime = newEnd, actor, adjustedAt = DateTimeOffset.UtcNow });
                workflow.PlanJson = adjustedPlan.ToJsonString();
            }
            await ValidateAsync(workflow, ct);
            var proposal = Output(workflow, "SchedulingValidationAgent"); var request = workflow.ResourceRequest;
            var schedule = new WorkSchedule { Id = Guid.NewGuid(), Name = request.Objective[..Math.Min(160, request.Objective.Length)], ActivityId = request.ActivityId, WorkflowId = id, StartTime = DateTimeOffset.Parse(proposal["startTime"]!.GetValue<string>()).ToUniversalTime(), EndTime = DateTimeOffset.Parse(proposal["endTime"]!.GetValue<string>()).ToUniversalTime(), Status = "Approved", CreatedById = actor, UpdatedById = actor };
            db.Add(schedule);
            foreach (var worker in proposal["workers"]!.AsArray()) db.Add(new WorkerAssignment { Id = Guid.NewGuid(), Name = "Approved resource plan", WorkerId = Guid.Parse(worker!["workerId"]!.GetValue<string>()), RequiredSkillId = Guid.Parse(worker["requiredSkillId"]!.GetValue<string>()), ScheduleId = schedule.Id, StartTime = schedule.StartTime, EndTime = schedule.EndTime, CreatedById = actor });
            foreach (var equipment in proposal["equipment"]!.AsArray()) db.Add(new EquipmentReservation { Id = Guid.NewGuid(), Name = "Approved resource plan", EquipmentId = Guid.Parse(equipment!["equipmentId"]!.GetValue<string>()), ScheduleId = schedule.Id, StartTime = schedule.StartTime, EndTime = schedule.EndTime, CreatedById = actor });
            foreach (var group in request.Items.Where(x => x.Kind == "Material").GroupBy(x => new { Name = x.Name.ToUpperInvariant(), Unit = x.Unit.ToUpperInvariant() }))
            {
                var remaining = group.Sum(x => x.Quantity);
                var materials = await db.Materials.Where(x => !x.IsArchived && x.Name.ToUpper() == group.Key.Name && x.Unit.ToUpper() == group.Key.Unit).OrderBy(x => x.Id).ToListAsync(ct);
                foreach (var material in materials)
                {
                    var quantity = Math.Min(remaining, material.AvailableStock); if (quantity <= 0) continue;
                    material.ReservedStock += quantity; remaining -= quantity;
                    db.InventoryReservations.Add(new InventoryReservation { Id = Guid.NewGuid(), ScheduleId = schedule.Id, MaterialId = material.Id, ProjectId = request.ProjectId, Quantity = quantity, ExpiresAt = schedule.EndTime });
                }
            }
            foreach (var recommendation in Output(workflow, "ProcurementAgent")["recommendations"]!.AsArray())
            {
                var selected = recommendation!["recommendedSupplier"]!; var quote = await db.Set<SupplierQuotation>().SingleAsync(x => x.Id == selected["quotationId"]!.GetValue<int>(), ct);
                var quantity = recommendation["shortageQuantity"]!.GetValue<decimal>();
                var purchase = new PurchaseRequest { ProjectId = request.ProjectId, MaterialId = quote.MaterialId, MaterialName = quote.MaterialName, Unit = quote.Unit, Quantity = quantity, BudgetLimit = quote.UnitPrice * quantity, RequiredByDate = schedule.StartTime.UtcDateTime, Status = "ConvertedToOrder", EstimatedUnitPrice = quote.UnitPrice, EstimatedTotalCost = quote.UnitPrice * quantity, CreatedById = actor, UpdatedById = actor };
                db.Add(purchase); await db.SaveChangesAsync(ct);
                db.Add(new PurchaseOrder { PurchaseRequestId = purchase.Id, ProjectId = request.ProjectId, MaterialId = quote.MaterialId, SupplierId = quote.SupplierId, QuotationId = quote.Id, MaterialName = quote.MaterialName, Unit = quote.Unit, Quantity = quantity, UnitPrice = quote.UnitPrice, TotalCost = quote.UnitPrice * quantity, DeliveryDate = quote.DeliveryDate, CreatedById = actor, UpdatedById = actor });
            }
        }
        workflow.Status = dto.Decision; workflow.CompletedAt = DateTimeOffset.UtcNow;
        var plan = JsonNode.Parse(workflow.PlanJson!)!.AsObject(); plan["status"] = workflow.Status; plan["approval"] = Node(new { decision = dto.Decision, reason = dto.Reason.Trim(), actor, decidedAt = DateTimeOffset.UtcNow }); workflow.PlanJson = plan.ToJsonString();
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return await operations.GetWorkflowAsync(id, actor, true, ct);
    }
}
