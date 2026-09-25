namespace BuildFlow.Api.Models;

public sealed class WorkerSkill : BaseEntity
{
    public Guid WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;

    public Guid SkillId { get; set; }
    public Skill Skill { get; set; } = null!;

    public string? ProficiencyLevel { get; set; }
}