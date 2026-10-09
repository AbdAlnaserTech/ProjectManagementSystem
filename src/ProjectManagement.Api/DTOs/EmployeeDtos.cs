using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Api.DTOs;

public class EmployeeRequest : IValidatableObject
{
    public string? FullName { get; init; }
    public string? Email { get; init; }
    public bool IsActive { get; init; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        foreach (var error in InputRules.RequiredText(FullName, 200, nameof(FullName))) yield return error;
        foreach (var error in InputRules.RequiredText(Email, 320, nameof(Email))) yield return error;
        if (!string.IsNullOrWhiteSpace(Email) && !new EmailAddressAttribute().IsValid(Email.Trim()))
            yield return new ValidationResult("A valid email address is required.", [nameof(Email)]);
    }
}

public record EmployeeResponse(int Id, string FullName, string Email, bool IsActive);

public class EmployeeQuery : PageQuery
{
    public string? Search { get; init; }
    public bool? IsActive { get; init; }
}
