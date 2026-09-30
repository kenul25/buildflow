using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BuildFlow.Api.DTOs;

public sealed record PageQuery(string? Search = null, string? Status = null, string? Sort = null, bool Desc = false, int Page = 1, int PageSize = 20, bool IncludeArchived = false, Guid? ParentId = null);
public sealed record PageResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

public sealed class ConstructionWriteDto
{
    [Required, StringLength(160, MinimumLength = 2)] public string Name { get; set; } = "";
    [StringLength(2000)] public string? Description { get; set; }
    [StringLength(32)] public string? Code { get; set; }
    [StringLength(32)] public string? Status { get; set; }
    public Guid? ParentId { get; set; }
    [StringLength(500)] public string? Address { get; set; }
    [Range(0, 10000)] public int? Sequence { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public Guid? AssignedEngineerId { get; set; }
}

public abstract class ConstructionNameWriteDto
{
    [Required, StringLength(160, MinimumLength = 2)] public string Name { get; set; } = "";
    [StringLength(2000)] public string? Description { get; set; }
}
public sealed class ProjectWriteDto : ConstructionNameWriteDto
{
    [Required, StringLength(32)] public string Code { get; set; } = "";
    [RegularExpression("Planned|Active|OnHold|Completed")] public string? Status { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public Guid? AssignedEngineerId { get; set; }
    public ConstructionWriteDto ToServiceDto() => new() { Name = Name, Description = Description, Code = Code, Status = Status, StartDate = StartDate, EndDate = EndDate, AssignedEngineerId = AssignedEngineerId };
}
public sealed class SiteWriteDto : ConstructionNameWriteDto
{
    [Required] public Guid ParentId { get; set; }
    [Required, StringLength(500)] public string Address { get; set; } = "";
    public ConstructionWriteDto ToServiceDto() => new() { Name = Name, Description = Description, ParentId = ParentId, Address = Address };
}
public sealed class PhaseWriteDto : ConstructionNameWriteDto
{
    [Required] public Guid ParentId { get; set; }
    [Range(0, 10000)] public int Sequence { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public ConstructionWriteDto ToServiceDto() => new() { Name = Name, Description = Description, ParentId = ParentId, Sequence = Sequence, StartDate = StartDate, EndDate = EndDate };
}
public sealed class ActivityWriteDto : ConstructionNameWriteDto
{
    [Required] public Guid ParentId { get; set; }
    [RegularExpression("Planned|InProgress|OnHold|Completed")] public string? Status { get; set; }
    public DateOnly? DueDate { get; set; }
    public ConstructionWriteDto ToServiceDto() => new() { Name = Name, Description = Description, ParentId = ParentId, Status = Status, DueDate = DueDate };
}

public sealed record ConstructionDto(Guid Id, string Name, string? Description, bool IsArchived, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, Guid? CreatedById, Guid? UpdatedById, Guid? ParentId, string? Code, string? Status, string? Address, int? Sequence, DateOnly? StartDate, DateOnly? EndDate, DateOnly? DueDate, int? ProgressPercent, Guid? AssignedEngineerId);

public sealed class ProgressWriteDto
{
    [Range(0, 100)] public int ProgressPercent { get; set; }
    [Required, StringLength(2000, MinimumLength = 2)] public string WorkCompleted { get; set; } = "";
    [StringLength(2000)] public string? Blockers { get; set; }
}
public sealed record ProgressDto(Guid Id, Guid ActivityId, int ProgressPercent, string WorkCompleted, string? Blockers, Guid SubmittedById, DateTimeOffset CreatedAt);
public sealed class ResourceItemWriteDto
{
    [Required, RegularExpression("Material|Equipment|Workforce")] public string Kind { get; set; } = "Material";
    [Required, StringLength(160, MinimumLength = 2)] public string Name { get; set; } = "";
    [Range(typeof(decimal), "0.001", "999999999")] public decimal Quantity { get; set; }
    [Required, StringLength(32)] public string Unit { get; set; } = "";
}
public sealed class ResourceRequestWriteDto
{
    [Required] public Guid ProjectId { get; set; }
    [Required] public Guid SiteId { get; set; }
    [Required] public Guid ActivityId { get; set; }
    [Required, StringLength(2000, MinimumLength = 10)] public string Objective { get; set; } = "";
    public DateOnly? RequiredBy { get; set; }
    [Range(typeof(decimal), "0", "999999999999")] public decimal? BudgetLimit { get; set; }
    [StringLength(2000)] public string? Notes { get; set; }
    [MinLength(1)] public List<ResourceItemWriteDto> Items { get; set; } = [];
}
public sealed record ResourceItemDto(string Kind, string Name, decimal Quantity, string Unit);
public sealed record ResourceRequestDto(Guid Id, Guid ProjectId, Guid SiteId, Guid ActivityId, string Objective, DateOnly? RequiredBy, decimal? BudgetLimit, string? Notes, IReadOnlyList<ResourceItemDto> Items, Guid SubmittedById, DateTimeOffset CreatedAt);
public sealed record ResourceRequestSummaryDto(Guid Id, Guid ProjectId, Guid ActivityId, string Objective, DateTimeOffset CreatedAt, Guid? WorkflowId, string? WorkflowStatus);
public sealed record WorkflowDto(Guid Id, Guid ResourceRequestId, string Status, JsonElement? Plan, string? Error, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt);
public sealed class AgentResultWriteDto
{
    [Required] public string SchemaVersion { get; set; } = "";
    [JsonPropertyName("task_id")]
    [Required] public Guid TaskId { get; set; }
    [Required] public string Agent { get; set; } = "";
    [Required] public string Status { get; set; } = "";
    [Required] public JsonElement Output { get; set; }
    [StringLength(1000)] public string? Error { get; set; }
}
