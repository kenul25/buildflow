namespace BuildFlow.Api.Models;

public abstract class ConstructionRecord : BaseEntity
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public Guid? UpdatedById { get; set; }
}

public sealed class Project : ConstructionRecord
{
    public string Code { get; set; } = "";
    public string Status { get; set; } = "Planned";
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public Guid? AssignedEngineerId { get; set; }
    public AppUser? AssignedEngineer { get; set; }
    public ICollection<Site> Sites { get; set; } = new List<Site>();
}

public sealed class Site : ConstructionRecord
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public string Address { get; set; } = "";
    public ICollection<ConstructionPhase> Phases { get; set; } = new List<ConstructionPhase>();
}

public sealed class ConstructionPhase : ConstructionRecord
{
    public Guid SiteId { get; set; }
    public Site Site { get; set; } = null!;
    public int Sequence { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public ICollection<ConstructionActivity> Activities { get; set; } = new List<ConstructionActivity>();
}

public sealed class ConstructionActivity : ConstructionRecord
{
    public Guid PhaseId { get; set; }
    public ConstructionPhase Phase { get; set; } = null!;
    public string Status { get; set; } = "Planned";
    public int ProgressPercent { get; set; }
    public DateOnly? DueDate { get; set; }
    public ICollection<ProgressUpdate> ProgressUpdates { get; set; } = new List<ProgressUpdate>();
}

public sealed class ProgressUpdate : BaseEntity
{
    public Guid ActivityId { get; set; }
    public ConstructionActivity Activity { get; set; } = null!;
    public int ProgressPercent { get; set; }
    public string WorkCompleted { get; set; } = "";
    public string? Blockers { get; set; }
    public Guid SubmittedById { get; set; }
}

public sealed class SitePhoto : BaseEntity
{
    public Guid ActivityId { get; set; }
    public ConstructionActivity Activity { get; set; } = null!;
    public string StorageName { get; set; } = "";
    public string OriginalName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Length { get; set; }
    public Guid UploadedById { get; set; }
}

public sealed class ResourceRequest : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public Guid SiteId { get; set; }
    public Site Site { get; set; } = null!;
    public Guid ActivityId { get; set; }
    public ConstructionActivity Activity { get; set; } = null!;
    public string Objective { get; set; } = "";
    public DateOnly? RequiredBy { get; set; }
    public decimal? BudgetLimit { get; set; }
    public string? Notes { get; set; }
    public Guid SubmittedById { get; set; }
    public ICollection<ResourceRequestItem> Items { get; set; } = new List<ResourceRequestItem>();
}

public sealed class ResourceRequestItem : BaseEntity
{
    public Guid ResourceRequestId { get; set; }
    public ResourceRequest ResourceRequest { get; set; } = null!;
    public string Kind { get; set; } = "Material";
    public string Name { get; set; } = "";
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "";
}

public sealed class PlanningWorkflow : BaseEntity
{
    public Guid ResourceRequestId { get; set; }
    public ResourceRequest ResourceRequest { get; set; } = null!;
    public string Status { get; set; } = "Queued";
    public string? PlanJson { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
