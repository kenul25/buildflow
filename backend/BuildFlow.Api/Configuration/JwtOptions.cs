namespace BuildFlow.Api.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string Secret { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 7;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience are required.");
        if (Secret.Length < 32 || Secret.StartsWith("SET_", StringComparison.Ordinal))
            throw new InvalidOperationException("Jwt:Secret must be configured with at least 32 characters.");
        if (AccessTokenMinutes is < 1 or > 60 || RefreshTokenDays is < 1 or > 30)
            throw new InvalidOperationException("JWT token lifetimes are outside the supported range.");
    }
}
