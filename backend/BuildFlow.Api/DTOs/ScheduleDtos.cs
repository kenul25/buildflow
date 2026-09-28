using System.ComponentModel.DataAnnotations;

namespace BuildFlow.Api.DTOs;

public sealed class ScheduleWriteDto
{
    [Required]
    public Guid ActivityId { get; set; }

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset EndTime { get; set; }

    [MaxLength(32)]
    public string Status { get; set; } = "Proposed";

    [MaxLength(64)]
    public string ApprovalStatus { get; set; } = "PendingProjectManagerApproval";

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public sealed record ScheduleDto(
    Guid Id,
    Guid ActivityId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string Status,
    string ApprovalStatus,
    string? Notes,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
