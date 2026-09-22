using BuildFlow.Api.DTOs;

namespace BuildFlow.Api.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, string ipAddress, CancellationToken cancellationToken);
    Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress, CancellationToken cancellationToken);
    Task<AuthResponse> RefreshAsync(string refreshToken, string ipAddress, CancellationToken cancellationToken);
    Task<UserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
    Task LogoutAsync(Guid userId, CancellationToken cancellationToken);
}
