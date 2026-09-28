using System.ComponentModel.DataAnnotations;

namespace BuildFlow.Api.DTOs;

public sealed record EquipmentReservationDto(
    Guid Id,
    Guid EquipmentId,
    Guid ActivityId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string Status,
    string? Notes,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed class EquipmentReservationWriteDto
{
    [Required]
    public Guid EquipmentId { get; set; }

    [Required]
    public Guid ActivityId { get; set; }

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset EndTime { get; set; }

    [MaxLength(32)]
    public string Status { get; set; } = "Planned";

    [MaxLength(500)]
    public string? Notes { get; set; }
}
