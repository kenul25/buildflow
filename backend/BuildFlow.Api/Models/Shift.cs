namespace BuildFlow.Api.Models;

public sealed class Shift : BaseEntity
{
    public string Name { get; set; } = "";

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsArchived { get; set; }
}