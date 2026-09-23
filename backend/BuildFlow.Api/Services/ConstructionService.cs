using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Models;
using BuildFlow.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Services;

public interface IConstructionService
{
    Task<PageResult<ConstructionDto>> ListAsync<T>(PageQuery query, Guid? engineerId, CancellationToken ct) where T : ConstructionRecord;
    Task<ConstructionDto> GetAsync<T>(Guid id, Guid? engineerId, CancellationToken ct) where T : ConstructionRecord;
    Task<ConstructionDto> CreateAsync<T>(ConstructionWriteDto dto, Guid actorId, CancellationToken ct) where T : ConstructionRecord, new();
    Task<ConstructionDto> UpdateAsync<T>(Guid id, ConstructionWriteDto dto, Guid actorId, CancellationToken ct) where T : ConstructionRecord;
    Task ArchiveAsync<T>(Guid id, Guid actorId, CancellationToken ct) where T : ConstructionRecord;
}

public sealed class ConstructionService(IConstructionRepository repository, BuildFlowDbContext db) : IConstructionService
{
    private static ApiException Bad(string message) => new(400, "invalid_construction_data", message);
    private static ApiException Missing() => new(404, "not_found", "The record was not found.");

    public async Task<PageResult<ConstructionDto>> ListAsync<T>(PageQuery query, Guid? engineerId, CancellationToken ct) where T : ConstructionRecord
    {
        if (query.Page < 1 || query.PageSize is < 1 or > 100) throw Bad("Page must be positive and pageSize must be between 1 and 100.");
        IQueryable<T> rows = repository.Query<T>().AsNoTracking();
        if (!query.IncludeArchived) rows = rows.Where(x => !x.IsArchived);
        if (!string.IsNullOrWhiteSpace(query.Status) && (typeof(T) == typeof(Project) || typeof(T) == typeof(ConstructionActivity)))
            rows = rows.Where(x => EF.Property<string>(x, "Status") == query.Status);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            rows = rows.Where(x => EF.Functions.ILike(x.Name, $"%{term}%"));
        }
        if (query.ParentId is Guid parent && typeof(T) != typeof(Project))
        {
            var key = typeof(T) == typeof(Site) ? "ProjectId" : typeof(T) == typeof(ConstructionPhase) ? "SiteId" : "PhaseId";
            rows = rows.Where(x => EF.Property<Guid>(x, key) == parent);
        }
        if (engineerId is Guid engineer)
        {
            var projects = db.Projects.Where(p => p.AssignedEngineerId == engineer).Select(p => p.Id);
            if (typeof(T) == typeof(Project)) rows = rows.Where(x => EF.Property<Guid?>(x, "AssignedEngineerId") == engineer);
            else if (typeof(T) == typeof(Site)) rows = rows.Where(x => projects.Contains(EF.Property<Guid>(x, "ProjectId")));
            else if (typeof(T) == typeof(ConstructionPhase)) rows = rows.Where(x => db.Sites.Where(s => projects.Contains(s.ProjectId)).Select(s => s.Id).Contains(EF.Property<Guid>(x, "SiteId")));
            else rows = rows.Where(x => db.ConstructionPhases.Where(p => db.Sites.Where(s => projects.Contains(s.ProjectId)).Select(s => s.Id).Contains(p.SiteId)).Select(p => p.Id).Contains(EF.Property<Guid>(x, "PhaseId")));
        }
        var total = await rows.CountAsync(ct);
        rows = query.Sort?.ToLowerInvariant() switch
        {
            "name" => query.Desc ? rows.OrderByDescending(x => x.Name) : rows.OrderBy(x => x.Name),
            "updatedat" => query.Desc ? rows.OrderByDescending(x => x.UpdatedAt) : rows.OrderBy(x => x.UpdatedAt),
            _ => query.Desc ? rows.OrderByDescending(x => x.CreatedAt) : rows.OrderBy(x => x.CreatedAt)
        };
        var items = await rows.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);
        return new(items.Select(Map).ToList(), total, query.Page, query.PageSize);
    }

    public async Task<ConstructionDto> GetAsync<T>(Guid id, Guid? engineerId, CancellationToken ct) where T : ConstructionRecord
    {
        var entity = await repository.Query<T>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw Missing();
        if (engineerId is Guid engineer && !await CanAccessAsync(entity, engineer, ct)) throw Missing();
        return Map(entity);
    }

    public async Task<ConstructionDto> CreateAsync<T>(ConstructionWriteDto dto, Guid actorId, CancellationToken ct) where T : ConstructionRecord, new()
    {
        var entity = new T { Id = Guid.NewGuid(), CreatedById = actorId, UpdatedById = actorId };
        await ApplyAsync(entity, dto, ct);
        repository.Add(entity);
        await repository.SaveAsync(ct);
        return Map(entity);
    }

    public async Task<ConstructionDto> UpdateAsync<T>(Guid id, ConstructionWriteDto dto, Guid actorId, CancellationToken ct) where T : ConstructionRecord
    {
        var entity = await repository.FindAsync<T>(id, ct) ?? throw Missing();
        if (entity.IsArchived) throw new ApiException(409, "archived", "Archived records cannot be edited.");
        var originalParent = entity switch { Site s => s.ProjectId, ConstructionPhase p => p.SiteId, ConstructionActivity a => a.PhaseId, _ => (Guid?)null };
        if (originalParent is not null && dto.ParentId != originalParent)
            throw new ApiException(409, "immutable_parent", "Move between parents is not supported because existing site records may reference this hierarchy.");
        await ApplyAsync(entity, dto, ct);
        entity.UpdatedById = actorId;
        await repository.SaveAsync(ct);
        return Map(entity);
    }

    public async Task ArchiveAsync<T>(Guid id, Guid actorId, CancellationToken ct) where T : ConstructionRecord
    {
        var entity = await repository.FindAsync<T>(id, ct) ?? throw Missing();
        if (entity is Project && await db.Sites.AnyAsync(x => x.ProjectId == id && !x.IsArchived, ct) ||
            entity is Site && await db.ConstructionPhases.AnyAsync(x => x.SiteId == id && !x.IsArchived, ct) ||
            entity is ConstructionPhase && await db.ConstructionActivities.AnyAsync(x => x.PhaseId == id && !x.IsArchived, ct) ||
            entity is ConstructionActivity && await db.ResourceRequests.AnyAsync(r => r.ActivityId == id && db.PlanningWorkflows.Any(w => w.ResourceRequestId == r.Id && (w.Status == "Queued" || w.Status == "AwaitingAgents")), ct))
            throw new ApiException(409, "active_children", "Archive active child records first.");
        entity.IsArchived = true;
        entity.ArchivedAt = DateTimeOffset.UtcNow;
        entity.UpdatedById = actorId;
        await repository.SaveAsync(ct);
    }

    private async Task ApplyAsync(ConstructionRecord entity, ConstructionWriteDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Trim().Length < 2) throw Bad("Name must contain at least two characters.");
        entity.Name = dto.Name.Trim();
        entity.Description = dto.Description?.Trim();
        if (dto.StartDate > dto.EndDate) throw Bad("Start date must not be after end date.");
        switch (entity)
        {
            case Project project:
                if (string.IsNullOrWhiteSpace(dto.Code)) throw Bad("Project code is required.");
                project.Code = dto.Code.Trim().ToUpperInvariant();
                if (project.Code.Length > 32) throw Bad("Project code must be 32 characters or fewer.");
                if (await db.Projects.AnyAsync(x => x.Code == project.Code && x.Id != project.Id, ct)) throw new ApiException(409, "duplicate_code", "Project code already exists.");
                if (dto.Status is not null && dto.Status is not ("Planned" or "Active" or "OnHold" or "Completed")) throw Bad("Project status is invalid.");
                project.Status = dto.Status ?? "Planned";
                project.StartDate = dto.StartDate; project.EndDate = dto.EndDate;
                if (dto.AssignedEngineerId is Guid engineer && !await db.Users.AnyAsync(x => x.Id == engineer && x.IsActive && x.UserRoles.Any(r => r.Role.Name == SystemRoles.SiteEngineer), ct)) throw Bad("Assigned engineer must be an active Site Engineer.");
                project.AssignedEngineerId = dto.AssignedEngineerId;
                break;
            case Site site:
                if (dto.ParentId is not Guid projectId || !await db.Projects.AnyAsync(x => x.Id == projectId && !x.IsArchived, ct)) throw Bad("Active project is required.");
                if (string.IsNullOrWhiteSpace(dto.Address)) throw Bad("Site address is required.");
                site.ProjectId = projectId; site.Address = dto.Address.Trim();
                break;
            case ConstructionPhase phase:
                if (dto.ParentId is not Guid siteId || !await db.Sites.AnyAsync(x => x.Id == siteId && !x.IsArchived && !x.Project.IsArchived, ct)) throw Bad("Active site is required.");
                phase.SiteId = siteId; phase.Sequence = dto.Sequence ?? 0; phase.StartDate = dto.StartDate; phase.EndDate = dto.EndDate;
                break;
            case ConstructionActivity activity:
                if (dto.ParentId is not Guid phaseId || !await db.ConstructionPhases.AnyAsync(x => x.Id == phaseId && !x.IsArchived && !x.Site.IsArchived && !x.Site.Project.IsArchived, ct)) throw Bad("Active phase is required.");
                if (dto.Status is not null && dto.Status is not ("Planned" or "InProgress" or "OnHold" or "Completed")) throw Bad("Activity status is invalid.");
                activity.PhaseId = phaseId; activity.Status = dto.Status ?? "Planned"; activity.DueDate = dto.DueDate;
                break;
        }
    }

    private async Task<bool> CanAccessAsync(ConstructionRecord entity, Guid engineer, CancellationToken ct) => entity switch
    {
        Project p => p.AssignedEngineerId == engineer,
        Site s => await db.Projects.AnyAsync(p => p.Id == s.ProjectId && p.AssignedEngineerId == engineer, ct),
        ConstructionPhase p => await db.Sites.AnyAsync(s => s.Id == p.SiteId && s.Project.AssignedEngineerId == engineer, ct),
        ConstructionActivity a => await db.ConstructionPhases.AnyAsync(p => p.Id == a.PhaseId && p.Site.Project.AssignedEngineerId == engineer, ct),
        _ => false
    };

    private static ConstructionDto Map(ConstructionRecord x) => x switch
    {
        Project p => new(p.Id,p.Name,p.Description,p.IsArchived,p.CreatedAt,p.UpdatedAt,p.CreatedById,p.UpdatedById,null,p.Code,p.Status,null,null,p.StartDate,p.EndDate,null,null,p.AssignedEngineerId),
        Site s => new(s.Id,s.Name,s.Description,s.IsArchived,s.CreatedAt,s.UpdatedAt,s.CreatedById,s.UpdatedById,s.ProjectId,null,null,s.Address,null,null,null,null,null,null),
        ConstructionPhase p => new(p.Id,p.Name,p.Description,p.IsArchived,p.CreatedAt,p.UpdatedAt,p.CreatedById,p.UpdatedById,p.SiteId,null,null,null,p.Sequence,p.StartDate,p.EndDate,null,null,null),
        ConstructionActivity a => new(a.Id,a.Name,a.Description,a.IsArchived,a.CreatedAt,a.UpdatedAt,a.CreatedById,a.UpdatedById,a.PhaseId,null,a.Status,null,null,null,null,a.DueDate,a.ProgressPercent,null),
        _ => throw new InvalidOperationException("Unsupported construction record")
    };
}
