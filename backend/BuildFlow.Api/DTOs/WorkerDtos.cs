using System.ComponentModel.DataAnnotations;

namespace BuildFlow.Api.DTOs;

public sealed class WorkerDto
{
    public Guid Id { get; set; }
    public string EmployeeCode { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = "";
    public bool IsActive { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class WorkerWriteDto
{
    [Required]
    [MaxLength(50)]
    public string EmployeeCode { get; set; } = "";

    [Required]
    [MaxLength(120)]
    public string FullName { get; set; } = "";

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    [Required]
    [MaxLength(100)]
    public string Role { get; set; } = "";
}