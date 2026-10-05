namespace BuildFlow.Api.Models;

public sealed class NotificationRead
{
    public Guid UserId { get; set; }
    public Guid NotificationId { get; set; }
    public DateTimeOffset ReadAt { get; set; }
}
