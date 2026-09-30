using BuildFlow.Api.Models;

namespace BuildFlow.Api.Interfaces;

public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAt);
public sealed record RefreshTokenResult(string RawToken, string TokenHash, DateTimeOffset ExpiresAt);

public interface ITokenService
{
    AccessTokenResult CreateAccessToken(AppUser user, IReadOnlyCollection<string> roles);
    RefreshTokenResult CreateRefreshToken();
    string HashRefreshToken(string token);
}
