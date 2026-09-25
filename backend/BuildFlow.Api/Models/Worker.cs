namespace BuildFlow.Api.Models;

public sealed class Worker : BaseEntity
{
    public string EmployeeCode { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public bool IsArchived { get; set; }
    public ICollection<WorkerSkill> WorkerSkills { get; set; } = new List<WorkerSkill>();
}