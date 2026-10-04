using System.ComponentModel.DataAnnotations;

namespace BuildFlow.Api.DTOs;

public sealed class SchedulingWriteDto
{
    [Required, StringLength(160, MinimumLength = 2)] public string Name { get; set; } = "";
    [StringLength(2000)] public string? Notes { get; set; }
    public Guid? UserId { get; set; }
    [StringLength(30)] public string Phone { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public Guid? WorkerId { get; set; }
    public Guid? SkillId { get; set; }
    public Guid? RequiredSkillId { get; set; }
    public Guid? EquipmentId { get; set; }
    public Guid? ActivityId { get; set; }
    public Guid? ScheduleId { get; set; }
    public Guid? DependencyId { get; set; }
    [StringLength(40)] public string Code { get; set; } = "";
    [StringLength(80)] public string Category { get; set; } = "";
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    [StringLength(32)] public string? Status { get; set; }
    [RegularExpression("Normal|High|Critical")] public string Severity { get; set; } = "Normal";
}
public sealed record SchedulingQuery(string? Search = null, string? Status = null, Guid? ParentId = null,
    string Sort = "name", bool Desc = false, int Page = 1, int PageSize = 20, bool IncludeArchived = false);
public sealed record StatusWriteDto([Required, StringLength(32)] string Status, [StringLength(2000)] string? Notes = null);
public sealed record ApprovalWriteDto([Required] string Decision, [Required, StringLength(2000)] string Reason);
public sealed record AvailabilityQuery(DateTimeOffset StartTime, DateTimeOffset EndTime, Guid? SkillId = null);
