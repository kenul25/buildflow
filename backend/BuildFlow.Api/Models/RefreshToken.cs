namespace BuildFlow.Api.Models;

public sealed class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string CreatedByIp { get; set; } = string.Empty;
    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;
}
