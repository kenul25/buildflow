using BuildFlow.Api.DTOs;

namespace BuildFlow.Api.Interfaces;

public interface IAdminService
{
    Task<IReadOnlyCollection<AdminUserResponse>> GetUsersAsync(
        string? search,
        CancellationToken cancellationToken);

    Task<AdminUserResponse> AssignRoleAsync(
        Guid userId,
        string role,
        CancellationToken cancellationToken);
}
