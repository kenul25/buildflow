namespace BuildFlow.Api.DTOs;

public sealed record UserResponse(Guid Id, string FullName, string Email, IReadOnlyCollection<string> Roles);

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    UserResponse User);
