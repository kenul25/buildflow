using BuildFlow.Api.Models;

namespace BuildFlow.Api.Models;

public sealed class Schedule : BaseEntity
{
    public Guid ActivityId { get; set; }
    public ConstructionActivity Activity { get; set; } = null!;

    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }

    public string Status { get; set; } = "Proposed";
    public string ApprovalStatus { get; set; } = "PendingProjectManagerApproval";

    public string? Notes { get; set; }

    public bool IsArchived { get; set; }
}
