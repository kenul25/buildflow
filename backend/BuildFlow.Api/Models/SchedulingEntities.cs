namespace BuildFlow.Api.Models;

public abstract class SchedulingRecord : BaseEntity
{
    public string Name { get; set; } = "";
    public string? Notes { get; set; }
    public bool IsArchived { get; set; }
    public Guid? CreatedById { get; set; }
    public Guid? UpdatedById { get; set; }
}
public sealed class Worker : SchedulingRecord
{
    public Guid? UserId { get; set; }
    public string Phone { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
public sealed class Skill : SchedulingRecord { }
public sealed class WorkerSkill : SchedulingRecord
{
    public Guid WorkerId { get; set; }
    public Guid SkillId { get; set; }
}
public sealed class Shift : SchedulingRecord
{
    public Guid WorkerId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
}
public sealed class Equipment : SchedulingRecord
{
    public string Code { get; set; } = "";
    public string Category { get; set; } = "";
    public string Status { get; set; } = "Operational";
}
public sealed class WorkSchedule : SchedulingRecord
{
    public Guid ActivityId { get; set; }
    public Guid? DependencyId { get; set; }
    public Guid? WorkflowId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public string Status { get; set; } = "Draft";
}
public sealed class WorkerAssignment : SchedulingRecord
{
    public Guid WorkerId { get; set; }
    public Guid ScheduleId { get; set; }
    public Guid? RequiredSkillId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public string Status { get; set; } = "Upcoming";
}
public sealed class EquipmentReservation : SchedulingRecord
{
    public Guid EquipmentId { get; set; }
    public Guid ScheduleId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public string Status { get; set; } = "Reserved";
}
public sealed class EquipmentRequest : SchedulingRecord
{
    public Guid ActivityId { get; set; }
    public Guid EquipmentId { get; set; }
    public string Status { get; set; } = "Draft";
}
public sealed class SiteIssue : SchedulingRecord
{
    public Guid ActivityId { get; set; }
    public Guid? EquipmentId { get; set; }
    public string Status { get; set; } = "Open";
    public string Severity { get; set; } = "Normal";
}
