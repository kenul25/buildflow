using System.ComponentModel.DataAnnotations;

namespace BuildFlow.Api.DTOs;

public sealed class EquipmentDto
{
    public Guid Id { get; set; }
    public string EquipmentCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class EquipmentWriteDto
{
    [Required]
    [MaxLength(50)]
    public string EquipmentCode { get; set; } = "";

    [Required]
    [MaxLength(160)]
    public string Name { get; set; } = "";

    [Required]
    [MaxLength(100)]
    public string Type { get; set; } = "";

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Available";

    [MaxLength(500)]
    public string? Description { get; set; }
}