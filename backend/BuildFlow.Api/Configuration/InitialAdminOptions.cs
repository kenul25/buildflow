namespace BuildFlow.Api.Configuration;

public sealed class InitialAdminOptions
{
    public const string SectionName = "InitialAdmin";

    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
