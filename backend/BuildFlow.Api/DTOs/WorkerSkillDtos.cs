using System.ComponentModel.DataAnnotations;

namespace BuildFlow.Api.DTOs;

public sealed class WorkerSkillDto
{
    public Guid Id { get; set; }
    public Guid WorkerId { get; set; }
    public Guid SkillId { get; set; }
    public string? ProficiencyLevel { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class WorkerSkillWriteDto
{
    [Required]
    public Guid WorkerId { get; set; }

    [Required]
    public Guid SkillId { get; set; }

    [MaxLength(50)]
    public string? ProficiencyLevel { get; set; }
}