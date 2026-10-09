using Microsoft.EntityFrameworkCore;
using ProjectManagement.Api.Data;
using ProjectManagement.Api.DTOs;
using ProjectManagement.Api.Errors;
using ProjectManagement.Api.Interfaces;
using ProjectManagement.Api.Models;

namespace ProjectManagement.Api.Services;

public class TaskService(ProjectManagementDbContext db) : ITaskService
{
    public async Task<PagedResponse<TaskResponse>> ListAsync(TaskQuery filter, CancellationToken token)
    {
        var query = db.ProjectTasks.AsNoTracking();
        if (filter.ProjectId.HasValue) query = query.Where(t => t.ProjectId == filter.ProjectId);
        if (filter.AssignedEmployeeId.HasValue) query = query.Where(t => t.AssignedEmployeeId == filter.AssignedEmployeeId);
        if (filter.Status.HasValue) query = query.Where(t => t.Status == filter.Status);
        if (filter.Priority.HasValue) query = query.Where(t => t.Priority == filter.Priority);
        var count = await query.CountAsync(token);
        return await Pagination.ReadAsync(query.OrderBy(t => t.Id).Select(ResponseMapping.Task), count, filter, token);
    }

    public async Task<TaskResponse> GetAsync(int id, CancellationToken token)
        => await db.ProjectTasks.AsNoTracking().Where(t => t.Id == id).Select(ResponseMapping.Task).SingleOrDefaultAsync(token)
            ?? throw CrudException.NotFound("Task");

    public async Task<TaskResponse> CreateAsync(TaskRequest request, CancellationToken token)
    {
        await EnsureReferencesAsync(request, true, token);
        var task = new ProjectTask();
        Apply(task, request);
        db.ProjectTasks.Add(task);
        await db.SaveChangesAsync(token);
        return await GetAsync(task.Id, token);
    }

    public async Task<TaskResponse> UpdateAsync(int id, TaskRequest request, CancellationToken token)
    {
        var task = await db.ProjectTasks.SingleOrDefaultAsync(t => t.Id == id, token)
            ?? throw CrudException.NotFound("Task");
        await EnsureReferencesAsync(request, task.AssignedEmployeeId != request.AssignedEmployeeId, token);
        Apply(task, request);
        await db.SaveChangesAsync(token);
        return await GetAsync(id, token);
    }

    public async Task DeleteAsync(int id, CancellationToken token)
    {
        var task = await db.ProjectTasks.SingleOrDefaultAsync(t => t.Id == id, token)
            ?? throw CrudException.NotFound("Task");
        db.ProjectTasks.Remove(task);
        await db.SaveChangesAsync(token);
    }

    private async Task EnsureReferencesAsync(TaskRequest request, bool requireActive, CancellationToken token)
    {
        if (!await db.Projects.AsNoTracking().AnyAsync(p => p.Id == request.ProjectId, token))
            throw CrudException.Invalid("ProjectId must reference an existing project.");
        var employee = await db.Employees.AsNoTracking().Where(e => e.Id == request.AssignedEmployeeId)
            .Select(e => new { e.IsActive }).SingleOrDefaultAsync(token);
        if (employee is null) throw CrudException.Invalid("AssignedEmployeeId must reference an existing employee.");
        if (requireActive && !employee.IsActive) throw CrudException.Invalid("A new task assignment requires an active employee.");
    }

    private static void Apply(ProjectTask task, TaskRequest request)
    {
        task.Title = request.Title!.Trim();
        task.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        task.Priority = request.Priority!.Value;
        task.Status = request.Status!.Value;
        task.DueDate = request.DueDate!.Value.UtcDateTime;
        task.ProjectId = request.ProjectId!.Value;
        task.AssignedEmployeeId = request.AssignedEmployeeId!.Value;
    }
}
