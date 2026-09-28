using System.ComponentModel.DataAnnotations;

namespace BuildFlow.Api.DTOs;

public sealed class ShiftDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsActive { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class ShiftWriteDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = "";

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }
}