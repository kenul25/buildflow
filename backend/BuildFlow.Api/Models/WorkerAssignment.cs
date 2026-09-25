namespace BuildFlow.Api.Models;

public sealed class WorkerAssignment : BaseEntity
{
    public Guid WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;

    public Guid ActivityId { get; set; }
    public ConstructionActivity Activity { get; set; } = null!;

    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }

    public string Status { get; set; } = "Planned";

    public string? Notes { get; set; }

    public bool IsArchived { get; set; }
}