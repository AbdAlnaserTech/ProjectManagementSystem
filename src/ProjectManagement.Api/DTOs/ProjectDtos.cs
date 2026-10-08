using System.ComponentModel.DataAnnotations;
using ProjectManagement.Api.Models;

namespace ProjectManagement.Api.DTOs;

public class ProjectRequest : IValidatableObject
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    [Required] public DateTimeOffset? StartDate { get; init; }
    [Required] public DateTimeOffset? EndDate { get; init; }
    [Required, EnumDataType(typeof(ProjectStatus))] public ProjectStatus? Status { get; init; }
    [Required, Range(1, int.MaxValue)] public int? ManagerId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        foreach (var error in InputRules.RequiredText(Name, 200, nameof(Name))) yield return error;
        foreach (var error in InputRules.OptionalText(Description, 2000, nameof(Description))) yield return error;
        if (StartDate.HasValue && EndDate.HasValue && EndDate < StartDate)
            yield return new ValidationResult("EndDate must be on or after StartDate.", [nameof(EndDate)]);
    }
}

public record ProjectResponse(int Id, string Name, string? Description, DateTimeOffset StartDate,
    DateTimeOffset EndDate, ProjectStatus Status, int ManagerId, EmployeeResponse Manager);
public record ProjectSummary(int Id, string Name, ProjectStatus Status);

public class ProjectQuery : PageQuery
{
    public string? Search { get; init; }
    [EnumDataType(typeof(ProjectStatus))] public ProjectStatus? Status { get; init; }
    [Range(1, int.MaxValue)] public int? ManagerId { get; init; }
}
