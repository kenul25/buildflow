using System.ComponentModel.DataAnnotations;

namespace BuildFlow.Api.DTOs;

public sealed class WorkerAssignmentDto
{
    public Guid Id { get; set; }
    public Guid WorkerId { get; set; }
    public Guid ActivityId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public string Status { get; set; } = "";
    public string? Notes { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class WorkerAssignmentWriteDto
{
    [Required]
    public Guid WorkerId { get; set; }

    [Required]
    public Guid ActivityId { get; set; }

    [Required]
    public DateTimeOffset StartTime { get; set; }

    [Required]
    public DateTimeOffset EndTime { get; set; }

    [MaxLength(32)]
    public string Status { get; set; } = "Planned";

    [MaxLength(500)]
    public string? Notes { get; set; }
}