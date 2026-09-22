using System.ComponentModel.DataAnnotations;
using BuildFlow.Api.Validators;

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

public sealed class CreateAdminUserRequest
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string FullName { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;

    [Required, PasswordRequirements]
    public string Password { get; init; } = string.Empty;

    [Required]
    public string Role { get; init; } = string.Empty;

    public bool IsActive { get; init; } = true;
}

public sealed class UpdateAdminUserRequest
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string FullName { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;

    [PasswordRequirements]
    public string? Password { get; init; }

    [Required]
    public string Role { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}
