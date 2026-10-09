using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Api.DTOs;

// Temporary basic validation. Integrate FluentValidation in its planned phase.
internal static class InputRules
{
    public static IEnumerable<ValidationResult> RequiredText(string? value, int maximum, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            yield return new ValidationResult($"{field} is required.", [field]);
        else if (value.Trim().Length > maximum)
            yield return new ValidationResult($"{field} must not exceed {maximum} characters.", [field]);
    }

    public static IEnumerable<ValidationResult> OptionalText(string? value, int maximum, string field)
    {
        if (value?.Trim().Length > maximum)
            yield return new ValidationResult($"{field} must not exceed {maximum} characters.", [field]);
    }
}
