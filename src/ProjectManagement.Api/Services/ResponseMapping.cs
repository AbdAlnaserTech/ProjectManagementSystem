using System.Linq.Expressions;
using ProjectManagement.Api.DTOs;
using ProjectManagement.Api.Models;

namespace ProjectManagement.Api.Services;

// Explicit SQL projections, with scalar UTC reconstruction at the DTO boundary.
// Replace/integrate mapping with AutoMapper only in the designated phase.
internal static class ResponseMapping
{
    public static readonly Expression<Func<Employee, EmployeeResponse>> Employee = e =>
        new EmployeeResponse(e.Id, e.FullName, e.Email, e.IsActive);

    public static readonly Expression<Func<Project, ProjectResponse>> Project = p =>
        new ProjectResponse(p.Id, p.Name, p.Description, Utc(p.StartDate), Utc(p.EndDate), p.Status, p.ManagerId,
            new EmployeeResponse(p.Manager.Id, p.Manager.FullName, p.Manager.Email, p.Manager.IsActive));

    public static readonly Expression<Func<ProjectTask, TaskResponse>> Task = t =>
        new TaskResponse(t.Id, t.Title, t.Description, t.Priority, t.Status, Utc(t.DueDate), t.ProjectId,
            t.AssignedEmployeeId, new ProjectSummary(t.Project.Id, t.Project.Name, t.Project.Status),
            new EmployeeResponse(t.AssignedEmployee.Id, t.AssignedEmployee.FullName, t.AssignedEmployee.Email, t.AssignedEmployee.IsActive));

    private static DateTimeOffset Utc(DateTime date) => new(DateTime.SpecifyKind(date, DateTimeKind.Utc));
}
