using System.ComponentModel.DataAnnotations;

namespace BuildFlow.Api.DTOs;

public sealed record AdminUserResponse(
    Guid Id,
    string FullName,
    string Email,
    bool IsActive,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<string> Roles);

public sealed class AssignRoleRequest
{
    [Required]
    public string Role { get; init; } = string.Empty;
}
