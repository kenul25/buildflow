using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Interfaces;
using BuildFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Services;

public sealed class AuthService(
    BuildFlowDbContext dbContext,
    IPasswordService passwordService,
    ITokenService tokenService,
    TimeProvider timeProvider) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        string ipAddress,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        if (await dbContext.Users.AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken))
            throw new ApiException(StatusCodes.Status409Conflict, "email_in_use", "An account already uses this email address.");

        var siteEngineerRole = await dbContext.Roles.SingleAsync(
            role => role.NormalizedName == SystemRoles.SiteEngineer.ToUpperInvariant(), cancellationToken);
        var user = new AppUser
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            NormalizedEmail = normalizedEmail,
            PasswordHash = passwordService.Hash(request.Password),
            UserRoles = [new UserRole { RoleId = siteEngineerRole.Id }]
        };
        dbContext.Users.Add(user);
        var response = CreateSession(user, [siteEngineerRole.Name], ipAddress);
        await dbContext.SaveChangesAsync(cancellationToken);
        return response;
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        string ipAddress,
        CancellationToken cancellationToken)
    {
        var user = await FindUserWithRolesAsync(NormalizeEmail(request.Email), cancellationToken);
        if (user is null || !passwordService.Verify(request.Password, user.PasswordHash))
            throw new ApiException(StatusCodes.Status401Unauthorized, "invalid_credentials", "Email or password is incorrect.");
        if (!user.IsActive)
            throw new ApiException(StatusCodes.Status403Forbidden, "account_disabled", "This account has been disabled.");

        var response = CreateSession(user, GetRoleNames(user), ipAddress);
        await dbContext.SaveChangesAsync(cancellationToken);
        return response;
    }

    public async Task<AuthResponse> RefreshAsync(
        string refreshToken,
        string ipAddress,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var tokenHash = tokenService.HashRefreshToken(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .Include(token => token.User)
            .ThenInclude(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null || !storedToken.IsActive(now))
            throw new ApiException(StatusCodes.Status401Unauthorized, "invalid_refresh_token", "The refresh token is invalid or expired.");
        if (!storedToken.User.IsActive)
            throw new ApiException(StatusCodes.Status403Forbidden, "account_disabled", "This account has been disabled.");

        var replacement = tokenService.CreateRefreshToken();
        storedToken.RevokedAt = now;
        storedToken.ReplacedByTokenHash = replacement.TokenHash;
        storedToken.User.RefreshTokens.Add(CreateStoredToken(replacement, ipAddress));
        var accessToken = tokenService.CreateAccessToken(storedToken.User, GetRoleNames(storedToken.User));
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken.Token,
            accessToken.ExpiresAt,
            replacement.RawToken,
            ToResponse(storedToken.User));
    }

    public async Task<UserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking()
            .Include(item => item.UserRoles)
            .ThenInclude(item => item.Role)
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "user_not_found", "The current user was not found.");

        return ToResponse(user);
    }

    public async Task LogoutAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var user = await dbContext.Users
            .Include(item => item.RefreshTokens)
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "user_not_found", "The current user was not found.");

        user.TokenVersion++;
        foreach (var token in user.RefreshTokens.Where(token => token.IsActive(now)))
            token.RevokedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private AuthResponse CreateSession(AppUser user, IReadOnlyCollection<string> roles, string ipAddress)
    {
        var accessToken = tokenService.CreateAccessToken(user, roles);
        var refreshToken = tokenService.CreateRefreshToken();
        user.RefreshTokens.Add(CreateStoredToken(refreshToken, ipAddress));
        return new AuthResponse(
            accessToken.Token,
            accessToken.ExpiresAt,
            refreshToken.RawToken,
            new UserResponse(user.Id, user.FullName, user.Email, roles));
    }

    private static RefreshToken CreateStoredToken(RefreshTokenResult token, string ipAddress) => new()
    {
        TokenHash = token.TokenHash,
        ExpiresAt = token.ExpiresAt,
        CreatedByIp = string.IsNullOrWhiteSpace(ipAddress) ? "unknown" : ipAddress
    };

    private Task<AppUser?> FindUserWithRolesAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        dbContext.Users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .SingleOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);

    private static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    private static IReadOnlyCollection<string> GetRoleNames(AppUser user) =>
        user.UserRoles.Select(userRole => userRole.Role.Name).Order().ToArray();

    private static UserResponse ToResponse(AppUser user) =>
        new(user.Id, user.FullName, user.Email, GetRoleNames(user));
}
