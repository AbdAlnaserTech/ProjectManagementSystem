namespace ProjectManagement.Api.Models;

public class ProjectTask
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskPriority Priority { get; set; }
    public ProjectTaskStatus Status { get; set; }
    public DateTime DueDate { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public int AssignedEmployeeId { get; set; }
    public Employee AssignedEmployee { get; set; } = null!;
}
