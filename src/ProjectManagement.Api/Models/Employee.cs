namespace ProjectManagement.Api.Models;

public class Employee
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<Project> ManagedProjects { get; set; } = new List<Project>();
    public ICollection<ProjectTask> AssignedTasks { get; set; } = new List<ProjectTask>();
}
