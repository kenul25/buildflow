namespace BuildFlow.Api.Models;

public sealed class Equipment : BaseEntity
{
    public string EquipmentCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Status { get; set; } = "Available";
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsArchived { get; set; }
}