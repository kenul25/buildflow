using System.ComponentModel.DataAnnotations;

namespace BuildFlow.Api.Validators;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class PasswordRequirementsAttribute : ValidationAttribute
{
    public PasswordRequirementsAttribute()
        : base("Password must contain uppercase, lowercase, number, and special characters.") { }

    public override bool IsValid(object? value)
    {
        if (value is null)
            return true;

        if (value is not string password || password.Length < 8 || password.Length > 128)
            return false;

        return password.Any(char.IsUpper) &&
               password.Any(char.IsLower) &&
               password.Any(char.IsDigit) &&
               password.Any(character => !char.IsLetterOrDigit(character));
    }
}
