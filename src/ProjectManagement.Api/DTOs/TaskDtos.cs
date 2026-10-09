using System.ComponentModel.DataAnnotations;
using ProjectManagement.Api.Models;

namespace ProjectManagement.Api.DTOs;

public class TaskRequest : IValidatableObject
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    [Required, EnumDataType(typeof(TaskPriority))] public TaskPriority? Priority { get; init; }
    [Required, EnumDataType(typeof(ProjectTaskStatus))] public ProjectTaskStatus? Status { get; init; }
    [Required] public DateTimeOffset? DueDate { get; init; }
    [Required, Range(1, int.MaxValue)] public int? ProjectId { get; init; }
    [Required, Range(1, int.MaxValue)] public int? AssignedEmployeeId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        foreach (var error in InputRules.RequiredText(Title, 200, nameof(Title))) yield return error;
        foreach (var error in InputRules.OptionalText(Description, 2000, nameof(Description))) yield return error;
    }
}

public record TaskResponse(int Id, string Title, string? Description, TaskPriority Priority,
    ProjectTaskStatus Status, DateTimeOffset DueDate, int ProjectId, int AssignedEmployeeId,
    ProjectSummary Project, EmployeeResponse AssignedEmployee);

public class TaskQuery : PageQuery
{
    [Range(1, int.MaxValue)] public int? ProjectId { get; init; }
    [Range(1, int.MaxValue)] public int? AssignedEmployeeId { get; init; }
    [EnumDataType(typeof(ProjectTaskStatus))] public ProjectTaskStatus? Status { get; init; }
    [EnumDataType(typeof(TaskPriority))] public TaskPriority? Priority { get; init; }
}
