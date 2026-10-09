using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Api.DTOs;

public class PageQuery : IValidatableObject
{
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 20;

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if ((long)(Page - 1) * PageSize > int.MaxValue)
            yield return new ValidationResult("The requested page offset is too large.", [nameof(Page)]);
    }
}

public record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);
