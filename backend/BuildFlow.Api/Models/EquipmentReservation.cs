using BuildFlow.Api.Models;

namespace BuildFlow.Api.Models;

public sealed class EquipmentReservation : BaseEntity
{
    public Guid EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;

    public Guid ActivityId { get; set; }
    public ConstructionActivity Activity { get; set; } = null!;

    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }

    public string Status { get; set; } = "Planned";
    public string? Notes { get; set; }

    public bool IsArchived { get; set; }
}
