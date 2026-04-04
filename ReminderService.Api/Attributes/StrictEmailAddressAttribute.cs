using System.ComponentModel.DataAnnotations;

namespace ReminderService.Api.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class StrictEmailAddressAttribute : ValidationAttribute
{
    private const string EmailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null or "")
        {
            return ValidationResult.Success;
        }

        if (value is string email && System.Text.RegularExpressions.Regex.IsMatch(email, EmailPattern))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult($"'{value}' is not a valid email address.");
    }
}
