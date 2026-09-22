using BuildFlow.Api.DTOs;

namespace BuildFlow.Api.Interfaces;

public interface IAdminService
{
    Task<IReadOnlyCollection<AdminUserResponse>> GetUsersAsync(
        string? search,
        CancellationToken cancellationToken);

    Task<AdminUserResponse> GetUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<AdminUserResponse> CreateUserAsync(
        CreateAdminUserRequest request,
        CancellationToken cancellationToken);

    Task<AdminUserResponse> UpdateUserAsync(
        Guid userId,
        UpdateAdminUserRequest request,
        CancellationToken cancellationToken);

    Task<AdminUserResponse> AssignRoleAsync(
        Guid userId,
        string role,
        CancellationToken cancellationToken);

    Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken);
}
