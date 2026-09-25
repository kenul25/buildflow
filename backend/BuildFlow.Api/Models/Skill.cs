namespace BuildFlow.Api.Models;

public sealed class Skill : BaseEntity
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsArchived { get; set; }

    public ICollection<WorkerSkill> WorkerSkills { get; set; } = new List<WorkerSkill>();
}