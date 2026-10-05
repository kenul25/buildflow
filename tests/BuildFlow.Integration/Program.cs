using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Models;
using BuildFlow.Api.Repositories;
using BuildFlow.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Text.Json;
using System.Text.Json.Nodes;
using BuildFlow.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using BuildFlow.Api.Configuration;
using Microsoft.Extensions.Options;

// Always creates its own disposable database. Never migrates the configured application DB.
var configuration = new ConfigurationBuilder().AddJsonFile(Path.GetFullPath("backend/BuildFlow.Api/appsettings.json"), true)
    .AddUserSecrets("1e423a1e-65a2-4452-94d0-4566928104f5").AddEnvironmentVariables().Build();
var source = Environment.GetEnvironmentVariable("BUILDFLOW_TEST_CONNECTION_STRING") ?? configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=postgres;Username=postgres;Password=postgres";
var settings = new NpgsqlConnectionStringBuilder(source) { Database = "postgres", Pooling = false };
var testName = "buildflow_test_" + Guid.NewGuid().ToString("N");
await using var admin = new NpgsqlConnection(settings.ConnectionString);
await admin.OpenAsync();
await new NpgsqlCommand($"CREATE DATABASE \"{testName}\"", admin).ExecuteNonQueryAsync();
settings.Database = testName;
var mainOptions = new DbContextOptionsBuilder<BuildFlowDbContext>().UseNpgsql(settings.ConnectionString).Options;
var procurementOptions = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(settings.ConnectionString).Options;
int checks = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception(name); Console.WriteLine("PASS " + name); checks++; }
async Task Reject(Func<Task> operation, string name) {
    try { await operation(); } catch (ApiException ex) when (ex.StatusCode is 400 or 403 or 409) { Check(true, name); return; }
    throw new Exception("Expected rejection: " + name);
}
try {
    await using var db = new BuildFlowDbContext(mainOptions);
    await using var procurementDb = new AppDbContext(procurementOptions);
    await db.Database.MigrateAsync();
    await procurementDb.GetService<IMigrator>().MigrateAsync("20260925093158_CheckSupplierMaterial");
    var legacyWarehouse = Guid.NewGuid(); var legacyMaterial = Guid.NewGuid();
    await procurementDb.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"Warehouse\" (\"Id\",\"Name\",\"Location\",\"CreatedAt\",\"UpdatedAt\") VALUES ({legacyWarehouse},'Legacy warehouse','Legacy location',NOW(),NOW())");
    await procurementDb.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"Material\" (\"Id\",\"Name\",\"Category\",\"Unit\",\"UnitPrice\",\"CurrentStock\",\"ReservedStock\",\"WarehouseId\",\"CreatedAt\",\"UpdatedAt\") VALUES ({legacyMaterial},'Legacy material','Legacy category','kg',3,12,0,{legacyWarehouse},NOW(),NOW())");
    await procurementDb.Database.MigrateAsync();
    Check(true, "Both database contexts migrate successfully on a fresh database");
    Check(await db.Materials.AnyAsync(x => x.Id == legacyMaterial && x.CurrentStock == 12), "Legacy singular inventory data is preserved in the authoritative tables");
    var actor = Guid.NewGuid();
    db.Users.Add(new AppUser { Id = actor, FullName = "Test Engineer", Email = "test@example.invalid", NormalizedEmail = "TEST@EXAMPLE.INVALID", PasswordHash = "test" });
    var project = new Project { Id = Guid.NewGuid(), Name = "Test project", Code = "TEST", AssignedEngineerId = actor, EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)) };
    var site = new Site { Id = Guid.NewGuid(), Name = "Test site", Project = project };
    var phase = new ConstructionPhase { Id = Guid.NewGuid(), Name = "Test phase", Site = site };
    var activity = new ConstructionActivity { Id = Guid.NewGuid(), Name = "Test activity", Phase = phase };
    db.Add(activity); await db.SaveChangesAsync();
    // Launch only this build, on its own port and disposable DB, to exercise real authorization and routing.
    var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
    var jwt = new JwtOptions { Issuer = "integration-test", Audience = "integration-test", Secret = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N") };
    var startInfo = new ProcessStartInfo("dotnet") { WorkingDirectory = Path.GetFullPath("backend/BuildFlow.Api"), UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
    var buildConfiguration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
    startInfo.ArgumentList.Add(Path.GetFullPath($"backend/BuildFlow.Api/bin/{buildConfiguration}/net8.0/BuildFlow.Api.dll")); startInfo.ArgumentList.Add("--urls"); startInfo.ArgumentList.Add($"http://127.0.0.1:{port}");
    startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
    startInfo.Environment["ConnectionStrings__DefaultConnection"] = settings.ConnectionString;
    startInfo.Environment["Jwt__Issuer"] = jwt.Issuer; startInfo.Environment["Jwt__Audience"] = jwt.Audience; startInfo.Environment["Jwt__Secret"] = jwt.Secret;
    startInfo.Environment["InitialAdmin__Email"] = "admin@example.invalid"; startInfo.Environment["InitialAdmin__Password"] = "Test-only-Admin-123!";
    startInfo.Environment["Cors__AllowedOrigins__0"] = "http://localhost:5173";
    using (var server = Process.Start(startInfo) ?? throw new Exception("Could not start isolated API")) {
        var output = server.StandardOutput.ReadToEndAsync(); var errors = server.StandardError.ReadToEndAsync();
        try {
            using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}/api/"), Timeout = TimeSpan.FromSeconds(5) };
            bool ready = false;
            for (int attempt = 0; attempt < 40; attempt++) {
                if (server.HasExited) throw new Exception("Isolated API exited during startup.");
                try { ready = (await http.GetAsync($"http://127.0.0.1:{port}/health")).IsSuccessStatusCode; } catch (HttpRequestException) { }
                if (ready) break; await Task.Delay(250);
            }
            Check(ready, "Isolated API starts successfully");
            Check((await http.GetAsync("PurchaseOrders")).StatusCode == HttpStatusCode.Unauthorized, "Anonymous purchase-order access is denied");
            Check((await http.GetAsync("Deliveries")).StatusCode == HttpStatusCode.Unauthorized, "Anonymous delivery access is denied");
            Check((await http.PostAsJsonAsync("Procurement/compare", new { materialName = "Cement", requiredQuantity = 1 })).StatusCode == HttpStatusCode.Unauthorized, "Anonymous quotation comparison is denied");
            var testUser = await db.Users.SingleAsync(x => x.Id == actor);
            var tokens = new TokenService(Options.Create(jwt), TimeProvider.System);
            http.DefaultRequestHeaders.Authorization = new("Bearer", tokens.CreateAccessToken(testUser, ["SiteEngineer"]).Token);
            Check((await http.GetAsync("purchase-requests")).IsSuccessStatusCode, "Mobile purchase-request route accepts assigned site users");
            Check((await http.GetAsync("scheduling/skills?pageSize=100")).IsSuccessStatusCode && (await http.GetAsync("scheduling/equipment?pageSize=100")).IsSuccessStatusCode, "Site users can search workforce and equipment catalogs");
            var usageCreate = await http.PostAsJsonAsync("construction/resource-requests", new {
                projectId = project.Id, siteId = site.Id, activityId = activity.Id, objective = "Equipment usage verification",
                items = new[] { new { kind = "Equipment", name = "Crane", quantity = 1.5m, unit = "hours", resourceCount = 1 } }
            });
            Check(usageCreate.StatusCode == HttpStatusCode.Created, "Resource requests accept fractional effort with explicit machine count");
            var usageId = (await usageCreate.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
            var usageRead = await (await http.GetAsync($"construction/resource-requests/{usageId}")).Content.ReadFromJsonAsync<JsonElement>();
            Check(usageRead.GetProperty("items")[0].GetProperty("resourceCount").GetInt32() == 1 && usageRead.GetProperty("items")[0].GetProperty("quantity").GetDecimal() == 1.5m, "Saved request preserves resource count and effort independently");
            Check((await http.PostAsJsonAsync("construction/resource-requests", new {
                projectId = project.Id, siteId = site.Id, activityId = activity.Id, objective = "Invalid equipment unit verification",
                items = new[] { new { kind = "Equipment", name = "Crane", quantity = 1m, unit = "bags", resourceCount = 1 } }
            })).StatusCode == HttpStatusCode.BadRequest, "Equipment usage rejects material units");
            Check((await http.PostAsJsonAsync("scheduling/workers", new { name = "Forbidden worker" })).StatusCode == HttpStatusCode.Forbidden, "Site users cannot create workers through HTTP");
            http.DefaultRequestHeaders.Authorization = new("Bearer", tokens.CreateAccessToken(testUser, ["ProjectManager"]).Token);
            var projectCreate = await http.PostAsJsonAsync("projects", new { name = "Delete verification project", code = "DELETE-TEST", status = "Planned" });
            Check(projectCreate.StatusCode == HttpStatusCode.Created, "Disposable project created for Delete verification");
            var deleteProjectId = (await projectCreate.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
            var siteCreate = await http.PostAsJsonAsync("sites", new { name = "Delete verification site", parentId = deleteProjectId, address = "Disposable site" });
            Check(siteCreate.StatusCode == HttpStatusCode.Created, "Disposable child site created for deletion guard");
            var deleteSiteId = (await siteCreate.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
            Check((await http.DeleteAsync($"projects/{deleteProjectId}")).StatusCode == HttpStatusCode.Conflict, "Project Delete rejects active child sites");
            Check((await http.DeleteAsync($"sites/{deleteSiteId}")).StatusCode == HttpStatusCode.NoContent, "Site Delete removes an eligible site");
            http.DefaultRequestHeaders.Authorization = new("Bearer", tokens.CreateAccessToken(testUser, ["SiteEngineer"]).Token);
            Check((await http.DeleteAsync($"projects/{deleteProjectId}")).StatusCode == HttpStatusCode.Forbidden, "Project Delete rejects site-user access");
            http.DefaultRequestHeaders.Authorization = new("Bearer", tokens.CreateAccessToken(testUser, ["ProjectManager"]).Token);
            Check((await http.DeleteAsync($"projects/{deleteProjectId}")).StatusCode == HttpStatusCode.NoContent, "Project Delete succeeds after removing active children");
            var activeProjects = await (await http.GetAsync("projects?pageSize=100")).Content.ReadFromJsonAsync<JsonElement>();
            Check(!activeProjects.GetProperty("items").EnumerateArray().Any(x => x.GetProperty("id").GetGuid() == deleteProjectId), "Deleted project disappears from active lists");
            var deletedProject = await db.Projects.AsNoTracking().SingleAsync(x => x.Id == deleteProjectId);
            Check(deletedProject.IsArchived && deletedProject.ArchivedAt != null && deletedProject.UpdatedById == actor, "Project Delete retains audited history");
            var created = await http.PostAsJsonAsync("scheduling/workers", new { name = "HTTP worker" });
            Check(created.StatusCode == HttpStatusCode.Created, "Member 04 CRUD routes create records through HTTP");
            var httpWorker = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
            Check((await http.PutAsJsonAsync($"scheduling/workers/{httpWorker}", new { name = "Updated HTTP worker" })).IsSuccessStatusCode, "Member 04 update route validates and saves records");
            Check((await http.DeleteAsync($"scheduling/workers/{httpWorker}")).StatusCode == HttpStatusCode.NoContent, "Member 04 delete route archives records");
            http.DefaultRequestHeaders.Add("Origin", "http://localhost:5173");
            var supplierList = await http.GetAsync("Suppliers");
            Check(supplierList.IsSuccessStatusCode && supplierList.Headers.GetValues("Access-Control-Expose-Headers").Any(x => x.Contains("X-Total-Count", StringComparison.OrdinalIgnoreCase)), "Cross-origin clients can read procurement pagination totals");
            Check((await http.GetAsync("dashboard")).IsSuccessStatusCode, "Dashboard summary returns operational metrics");
        } finally { if (!server.HasExited) server.Kill(entireProcessTree: true); await server.WaitForExitAsync(); await Task.WhenAll(output, errors); }
    }
    var inventory = new InventoryService(db, new InventoryRepository(db));
    var warehouse = await inventory.CreateWarehouseAsync(new() { Name = "Test warehouse" }, default);
    var material = await inventory.CreateMaterialAsync(new() { Name = "Cement", Category = "Concrete", Unit = "kg", WarehouseId = warehouse.Id, CurrentStock = 10 }, default);
    await Reject(() => inventory.UpdateMaterialAsync(material.Id, new() { Name = "Cement", Category = "Concrete", Unit = "kg", WarehouseId = warehouse.Id, CurrentStock = 100 }, default), "Stock cannot be changed through material metadata");
    db.ChangeTracker.Clear();
    var reservation = await inventory.ReserveAsync(material.Id, new() { ProjectId = project.Id, Quantity = 4 }, default);
    await inventory.ReleaseReservationAsync(reservation.Id, default);
    await inventory.ReleaseReservationAsync(reservation.Id, default);
    Check((await inventory.GetMaterialAsync(material.Id, default)).ReservedStock == 0, "Reservation release is idempotent");
    await Reject(() => inventory.DeleteMaterialAsync(material.Id, default), "Material with stock cannot be archived");
    db.ChangeTracker.Clear();
    var scheduling = new SchedulingService(db);
    async Task Crud<T>(T row, SchedulingWriteDto dto) where T : SchedulingRecord, new() {
        var read = await scheduling.GetAsync<T>(row.Id, actor, true, default);
        dto.Notes = "Updated during regression test";
        var updated = await scheduling.WriteAsync<T>(row.Id, dto, actor, true, default);
        Check(read.Id == updated.Id && updated.Notes == dto.Notes, typeof(T).Name + " create/read/update contract");
    }
    var worker = await scheduling.WriteAsync<Worker>(null, new() { Name = "Test worker" }, actor, true, default);
    var skill = await scheduling.WriteAsync<Skill>(null, new() { Name = "Masonry" }, actor, true, default);
    var link = await scheduling.WriteAsync<WorkerSkill>(null, new() { Name = "Masonry qualification", WorkerId = worker.Id, SkillId = skill.Id }, actor, true, default);
    var start = DateTimeOffset.UtcNow.AddDays(2); var end = start.AddHours(2);
    var shift = await scheduling.WriteAsync<Shift>(null, new() { Name = "Day shift", WorkerId = worker.Id, StartTime = start, EndTime = end }, actor, true, default);
    var equipment = await scheduling.WriteAsync<Equipment>(null, new() { Name = "Crane", Category = "Lifting", Code = "CR-01" }, actor, true, default);
    var schedule = await scheduling.WriteAsync<WorkSchedule>(null, new() { Name = "Test schedule", ActivityId = activity.Id, StartTime = start, EndTime = end, Status = "Approved" }, actor, true, default);
    var assignment = await scheduling.WriteAsync<WorkerAssignment>(null, new() { Name = "Masonry assignment", WorkerId = worker.Id, RequiredSkillId = skill.Id, ScheduleId = schedule.Id, StartTime = start, EndTime = end }, actor, true, default);
    var booking = await scheduling.WriteAsync<EquipmentReservation>(null, new() { Name = "Crane booking", EquipmentId = equipment.Id, ScheduleId = schedule.Id, StartTime = start, EndTime = end }, actor, true, default);
    var request = await scheduling.WriteAsync<EquipmentRequest>(null, new() { Name = "Crane request", ActivityId = activity.Id, EquipmentId = equipment.Id }, actor, false, default);
    var issue = await scheduling.WriteAsync<SiteIssue>(null, new() { Name = "Site delay", ActivityId = activity.Id }, actor, false, default);
    await Crud(worker, new() { Name = worker.Name });
    await Crud(skill, new() { Name = skill.Name });
    await Crud(link, new() { Name = link.Name, WorkerId = worker.Id, SkillId = skill.Id });
    await Crud(shift, new() { Name = shift.Name, WorkerId = worker.Id, StartTime = start, EndTime = end });
    await Crud(equipment, new() { Name = equipment.Name, Code = equipment.Code, Category = equipment.Category });
    await Crud(schedule, new() { Name = schedule.Name, ActivityId = activity.Id, StartTime = start, EndTime = end, Status = "Approved" });
    await Crud(assignment, new() { Name = assignment.Name, WorkerId = worker.Id, RequiredSkillId = skill.Id, ScheduleId = schedule.Id, StartTime = start, EndTime = end });
    await Crud(booking, new() { Name = booking.Name, EquipmentId = equipment.Id, ScheduleId = schedule.Id, StartTime = start, EndTime = end });
    await Crud(request, new() { Name = request.Name, ActivityId = activity.Id, EquipmentId = equipment.Id });
    await Crud(issue, new() { Name = issue.Name, ActivityId = activity.Id });
    Check((await scheduling.ListAsync<WorkSchedule>(new(), actor, false, default)).Total == 1, "Assigned engineer can read the schedule");
    Check((await scheduling.ListAsync<WorkSchedule>(new(), Guid.NewGuid(), false, default)).Total == 0, "Other engineers cannot read the schedule");
    await Reject(() => scheduling.WriteAsync<Worker>(worker.Id, new() { Name = "Changed worker" }, actor, false, default), "Site users cannot alter management records");
    await Reject(() => scheduling.WriteAsync<WorkerAssignment>(null, new() { Name = "Overlap", WorkerId = worker.Id, RequiredSkillId = skill.Id, ScheduleId = schedule.Id, StartTime = start, EndTime = end }, actor, true, default), "Overlapping worker assignments are rejected");
    await Reject(() => scheduling.WriteAsync<EquipmentReservation>(null, new() { Name = "Overlap", EquipmentId = equipment.Id, ScheduleId = schedule.Id, StartTime = start, EndTime = end }, actor, true, default), "Overlapping equipment reservations are rejected");
    await Reject(() => scheduling.ArchiveAsync<WorkerSkill>(link.Id, actor, true, default), "Skills needed by active assignments cannot be removed");
    await Reject(() => scheduling.ArchiveAsync<Shift>(shift.Id, actor, true, default), "Shifts covering active assignments cannot be removed");
    db.ChangeTracker.Clear();
    var service = new ProcurementService(procurementDb, db);
    var supplier = new Supplier { Name = "Test supplier", IsActive = true };
    procurementDb.Suppliers.Add(supplier); await procurementDb.SaveChangesAsync();
    procurementDb.SupplierMaterials.Add(new SupplierMaterial { SupplierId = supplier.Id, MaterialId = material.Id, UnitPrice = 2, AvailableQuantity = 100, IsActive = true });
    await procurementDb.SaveChangesAsync();
    var legacyRequest = new PurchaseRequest { MaterialName = "Cement", Quantity = 1, Status = "Approved", RequiredByDate = DateTime.UtcNow.AddDays(5) };
    procurementDb.PurchaseRequests.Add(legacyRequest); await procurementDb.SaveChangesAsync();
    var legacyOrder = new PurchaseOrder { PurchaseRequestId = legacyRequest.Id, SupplierId = supplier.Id, MaterialName = "Cement", Quantity = 1, UnitPrice = 2, TotalCost = 2, DeliveryDate = DateTime.UtcNow.AddDays(3) };
    procurementDb.PurchaseOrders.Add(legacyOrder); await procurementDb.SaveChangesAsync();
    await service.LinkLegacyRequestAsync(legacyRequest.Id, new(project.Id, material.Id), actor, default);
    Check(legacyOrder.MaterialId == material.Id && legacyOrder.ProjectId == project.Id && legacyOrder.TotalCost == 2 && legacyRequest.Status == "Approved", "Legacy procurement links are repaired without changing financial history");
    var purchase = await service.WriteRequestAsync(null, new() { ProjectId = project.Id, MaterialId = material.Id, Quantity = 2.5m, RequiredByDate = DateTime.UtcNow.AddDays(5) }, actor, default);
    await Reject(() => service.WriteOrderAsync(null, new() { PurchaseRequestId = purchase.Id, SupplierId = supplier.Id, UnitPrice = 2, DeliveryDate = DateTime.UtcNow.AddDays(3) }, actor, default), "Orders require approved requests");
    await service.RequestDecisionAsync(purchase.Id, true, actor, default);
    var order = await service.WriteOrderAsync(null, new() { PurchaseRequestId = purchase.Id, SupplierId = supplier.Id, UnitPrice = 2, DeliveryDate = DateTime.UtcNow.AddDays(3) }, actor, default);
    Check(order.Quantity == 2.5m && order.TotalCost == 5, "Fractional procurement quantities and costs are preserved");
    await Reject(() => service.WriteOrderAsync(null, new() { PurchaseRequestId = purchase.Id, SupplierId = supplier.Id, UnitPrice = 2, DeliveryDate = DateTime.UtcNow.AddDays(3) }, actor, default), "Duplicate active purchase orders are rejected");
    var delivery = await service.WriteDeliveryAsync(null, order.Id, 1.5m, DateTime.UtcNow.AddDays(2), "", actor, default);
    await Reject(() => service.WriteDeliveryAsync(null, order.Id, 2, DateTime.UtcNow.AddDays(2), "", actor, default), "Cumulative deliveries cannot exceed order quantity");
    await service.DeliveryStatusAsync(delivery.Id, "Received", actor, false, default);
    await service.DeliveryStatusAsync(delivery.Id, "Received", actor, false, default);
    db.ChangeTracker.Clear();
    Check((await inventory.GetMaterialAsync(material.Id, default)).CurrentStock == 11.5m, "Delivery receipt updates shared inventory exactly once");
    await service.DeliveryStatusAsync(delivery.Id, "Completed", actor, false, default);
    await Reject(() => service.DeliveryStatusAsync(delivery.Id, "Pending", actor, true, default), "Completed deliveries cannot reopen");
    var tooLarge = await service.WriteRequestAsync(null, new() { ProjectId = project.Id, MaterialId = material.Id, Quantity = 100, RequiredByDate = DateTime.UtcNow.AddDays(5) }, actor, default);
    await service.RequestDecisionAsync(tooLarge.Id, true, actor, default);
    await Reject(() => service.WriteOrderAsync(null, new() { PurchaseRequestId = tooLarge.Id, SupplierId = supplier.Id, UnitPrice = 2, DeliveryDate = DateTime.UtcNow.AddDays(3) }, actor, default), "Outstanding orders prevent supplier stock from being committed twice");
    procurementDb.SupplierQuotations.Add(new SupplierQuotation { SupplierId = supplier.Id, MaterialId = material.Id, MaterialName = "Cement", Unit = "kg", Quantity = 50, UnitPrice = 2, TotalPrice = 100, DeliveryDate = DateTime.UtcNow.AddDays(2), ValidUntil = DateTime.UtcNow.AddDays(4) });
    await procurementDb.SaveChangesAsync();
    var compare = new ProcurementController(procurementDb);
    var comparison = await compare.CompareQuotations(new() { MaterialName = "Cement", RequiredQuantity = 2.5m });
    Check(comparison is OkObjectResult result && JsonSerializer.SerializeToElement(result.Value).GetProperty("quotationCount").GetInt32() == 1, "Quotation comparison validates supplier capacity through database queries");
    // Run the full backend workflow using deterministic contract responses, without an external LLM.
    var planStart = start.AddDays(2); var planEnd = planStart.AddHours(1);
    await scheduling.WriteAsync<Shift>(null, new() { Name = "Planning shift", WorkerId = worker.Id, StartTime = planStart, EndTime = planEnd }, actor, true, default);
    var planner = new ContractPlanner(activity.Id, worker.Id, skill.Id, equipment.Id, planStart, planEnd);
    var operations = new ConstructionOperationsService(db, planner, null!);
    var resource = await operations.SubmitRequestAsync(new() { ProjectId = project.Id, SiteId = site.Id, ActivityId = activity.Id, Objective = "Complete masonry with a crane", RequiredBy = DateOnly.FromDateTime(planEnd.UtcDateTime), Items = [new() { Kind = "Material", Name = "Cement", Unit = "kg", Quantity = 1 }, new() { Kind = "Workforce", Name = "Masonry", Unit = "people", Quantity = 1 }, new() { Kind = "Equipment", Name = "Crane", Unit = "units", Quantity = 1 }] }, actor, true, default);
    var workflow = await operations.StartPlanningAsync(resource.Id, actor, true, default);
    var executor = new WorkflowExecutionService(db, planner, operations, scheduling, inventory);
    workflow = await executor.ExecuteAsync(workflow.Id, actor, true, default);
    Check(workflow.Status == "PendingProjectManagerApproval", "Agent execution passes backend validation and waits for manager approval");
    Check(!await db.Set<WorkSchedule>().AnyAsync(x => x.WorkflowId == workflow.Id), "Proposal execution creates no bookings before approval");
    var storedWorkflow = await db.PlanningWorkflows.SingleAsync(x => x.Id == workflow.Id);
    var validProposal = storedWorkflow.PlanJson; storedWorkflow.PlanJson = "{\"tasks\":[]}";
    await Reject(() => executor.ValidateAsync(storedWorkflow, default), "Malformed agent proposals fail backend schema validation");
    storedWorkflow.PlanJson = validProposal;
    var expiredPlan = JsonNode.Parse(validProposal!)!;
    var expiredSchedule = expiredPlan["tasks"]!.AsArray().Single(x => x!["agent"]!.GetValue<string>() == "SchedulingValidationAgent")!["output"]!;
    var expiredStart = DateTimeOffset.UtcNow.AddHours(-2);
    expiredSchedule["startTime"] = expiredStart.ToString("O");
    expiredSchedule["endTime"] = expiredStart.AddHours(1).ToString("O");
    storedWorkflow.PlanJson = expiredPlan.ToJsonString(); await db.SaveChangesAsync();
    await Reject(() => executor.DecideAsync(workflow.Id, new("Approved", "Expired plan"), actor, default), "Expired start cannot be approved without a new schedule time");
    await Reject(() => executor.DecideAsync(workflow.Id, new("Approved", "Outside covering shift", planStart.AddMinutes(15)), actor, default), "Rescheduled approval rechecks covering worker shifts");
    db.ChangeTracker.Clear();
    await Reject(() => executor.DecideAsync(workflow.Id, new("Approved", "Beyond deadline", planStart.AddDays(2)), actor, default), "Rescheduled approval still enforces the required date");
    db.ChangeTracker.Clear();
    var unapproved = await db.PlanningWorkflows.AsNoTracking().SingleAsync(x => x.Id == workflow.Id);
    Check(unapproved.Status == "PendingProjectManagerApproval" && !await db.Set<WorkSchedule>().AnyAsync(x => x.WorkflowId == workflow.Id), "Failed time adjustments leave the saved proposal unapproved and create no bookings");
    workflow = await executor.DecideAsync(workflow.Id, new("Approved", "Reviewed resources and updated dates", planStart), actor, default);
    Check(workflow.Status == "Approved" && await db.Set<WorkSchedule>().AnyAsync(x => x.WorkflowId == workflow.Id && x.Status == "Approved"), "Manager approval creates the validated schedule atomically");
    Check(workflow.Plan?.GetProperty("scheduleAdjustment").GetProperty("startTime").GetDateTimeOffset() == planStart, "Approval records the manager's time adjustment for audit");
    Check((await inventory.GetMaterialAsync(material.Id, default)).ReservedStock == 1, "Manager approval reserves required stock");
    await Reject(() => executor.DecideAsync(workflow.Id, new("Approved", "Duplicate"), actor, default), "Approval cannot be applied twice");
    var approvedSchedule = await db.Set<WorkSchedule>().SingleAsync(x => x.WorkflowId == workflow.Id);
    foreach (var row in await db.Set<WorkerAssignment>().Where(x => x.ScheduleId == approvedSchedule.Id).ToListAsync()) await scheduling.ArchiveAsync<WorkerAssignment>(row.Id, actor, true, default);
    foreach (var row in await db.Set<EquipmentReservation>().Where(x => x.ScheduleId == approvedSchedule.Id).ToListAsync()) await scheduling.ArchiveAsync<EquipmentReservation>(row.Id, actor, true, default);
    await scheduling.ArchiveAsync<WorkSchedule>(approvedSchedule.Id, actor, true, default);
    Check((await inventory.GetMaterialAsync(material.Id, default)).ReservedStock == 0, "Schedule cancellation releases its linked material reservations");
    var failingOperations = new ConstructionOperationsService(db, new TimeoutPlanner(), null!);
    var failingRequest = await failingOperations.SubmitRequestAsync(new() { ProjectId = project.Id, SiteId = site.Id, ActivityId = activity.Id, Objective = "Exercise a safe planner timeout", Items = [new() { Kind = "Material", Name = "Cement", Unit = "kg", Quantity = 1 }] }, actor, true, default);
    var failedWorkflow = await failingOperations.StartPlanningAsync(failingRequest.Id, actor, true, default);
    Check(failedWorkflow.Status == "Failed" && failedWorkflow.Error == "planning_timeout", "Planner timeout persists a safe failure state");
    var retriedWorkflow = await failingOperations.StartPlanningAsync(failingRequest.Id, actor, true, default);
    Check(retriedWorkflow.Plan?.GetProperty("executionHistory").GetArrayLength() > 0, "Planning retry preserves prior execution history");
    await scheduling.WriteAsync<SiteIssue>(issue.Id, new() { Name = "Resolved delay", ActivityId = activity.Id, Status = "Resolved" }, actor, false, default);
    await scheduling.ArchiveAsync<SiteIssue>(issue.Id, actor, false, default);
    await scheduling.ArchiveAsync<EquipmentRequest>(request.Id, actor, false, default);
    await scheduling.ArchiveAsync<WorkerAssignment>(assignment.Id, actor, true, default);
    await scheduling.ArchiveAsync<EquipmentReservation>(booking.Id, actor, true, default);
    await scheduling.ArchiveAsync<WorkSchedule>(schedule.Id, actor, true, default);
    await scheduling.ArchiveAsync<WorkerSkill>(link.Id, actor, true, default);
    await scheduling.ArchiveAsync<Shift>(shift.Id, actor, true, default);
    await scheduling.ArchiveAsync<Worker>(worker.Id, actor, true, default);
    await scheduling.ArchiveAsync<Skill>(skill.Id, actor, true, default);
    await scheduling.ArchiveAsync<Equipment>(equipment.Id, actor, true, default);
    Check((await scheduling.ListAsync<Worker>(new(), actor, true, default)).Total == 0, "Member 04 archive operations preserve history and remove active rows");
    var concurrent = await inventory.CreateMaterialAsync(new() { Name = "Concurrency material", Category = "Test", Unit = "kg", WarehouseId = warehouse.Id, CurrentStock = 5 }, default);
    async Task<Guid?> ReserveAttempt() {
        await using var session = new BuildFlowDbContext(mainOptions);
        var service = new InventoryService(session, new InventoryRepository(session));
        try { return (await service.ReserveAsync(concurrent.Id, new() { Quantity = 4, ProjectId = project.Id }, default)).Id; }
        catch (ApiException ex) when (ex.StatusCode == 409) { return null; }
    }
    var attempts = await Task.WhenAll(ReserveAttempt(), ReserveAttempt());
    Check(attempts.Count(x => x != null) == 1, "Concurrent reservations cannot overbook stock");
    async Task ReleaseAttempt() {
        await using var session = new BuildFlowDbContext(mainOptions);
        await new InventoryService(session, new InventoryRepository(session)).ReleaseReservationAsync(attempts.Single(x => x != null)!.Value, default);
    }
    await Task.WhenAll(ReleaseAttempt(), ReleaseAttempt()); db.ChangeTracker.Clear();
    Check((await inventory.GetMaterialAsync(concurrent.Id, default)).ReservedStock == 0, "Concurrent releases subtract reservation stock only once");
    Console.WriteLine($"SUCCESS: {checks} integration checks.");
} finally {
    NpgsqlConnection.ClearAllPools();
    await new NpgsqlCommand($"DROP DATABASE \"{testName}\" WITH (FORCE)", admin).ExecuteNonQueryAsync();
    Console.WriteLine("Disposable database removed.");
}

sealed class ContractPlanner(Guid activity, Guid worker, Guid skill, Guid equipment, DateTimeOffset start, DateTimeOffset end) : IPlanningClient {
    public Task<JsonElement> PlanAsync(object request, CancellationToken ct) {
        var input = JsonSerializer.SerializeToNode(request, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        var ids = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray();
        var names = new[] { "InventoryAgent", "ProcurementAgent", "SchedulingValidationAgent" };
        var actions = new[] { "AnalyzeAvailability", "RecommendProcurement", "ProposeAndValidateSchedule" };
        var tasks = names.Select((agent, index) => new { task_id = ids[index], agent, action = actions[index], status = "Pending", depends_on = ids.Take(index), input = input.DeepClone() }).ToArray();
        return Task.FromResult(JsonSerializer.SerializeToElement(new { schemaVersion = "1.0", workflowId = input["workflowId"]!.GetValue<string>(), status = "AwaitingAgents", approvalRequired = true, tasks }));
    }
    public Task<JsonElement> ExecuteAsync(object task, CancellationToken ct) {
        var node = JsonSerializer.SerializeToNode(task)!;
        var agent = node["agent"]!.GetValue<string>();
        object output = agent switch {
            "InventoryAgent" => new { items = new[] { new { name = "Cement", unit = "kg", shortageQuantity = 0 } } },
            "ProcurementAgent" => new { recommendations = Array.Empty<object>(), estimatedTotal = 0 },
            _ => new { status = "READY_FOR_APPROVAL", activityId = activity, startTime = start, endTime = end, workers = new[] { new { workerId = worker, requiredSkillId = skill } }, equipment = new[] { new { equipmentId = equipment } }, validation = new { isValid = true } }
        };
        return Task.FromResult(JsonSerializer.SerializeToElement(new { schemaVersion = "1.0", task_id = node["task_id"]!.GetValue<string>(), agent, status = "Completed", output }));
    }
}
sealed class TimeoutPlanner : IPlanningClient {
    public Task<JsonElement> PlanAsync(object request, CancellationToken ct) => throw new TaskCanceledException("Test timeout");
    public Task<JsonElement> ExecuteAsync(object task, CancellationToken ct) => throw new TaskCanceledException("Test timeout");
}
