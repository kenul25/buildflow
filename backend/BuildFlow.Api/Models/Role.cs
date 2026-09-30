namespace BuildFlow.Api.Models;

public sealed class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public ICollection<UserRole> UserRoles { get; set; } = [];
}
