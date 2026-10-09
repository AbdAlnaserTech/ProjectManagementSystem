using Microsoft.EntityFrameworkCore;
using ProjectManagement.Api.Data;
using ProjectManagement.Api.DTOs;
using ProjectManagement.Api.Errors;
using ProjectManagement.Api.Interfaces;
using ProjectManagement.Api.Models;

namespace ProjectManagement.Api.Services;

public class ProjectService(ProjectManagementDbContext db) : IProjectService
{
    public async Task<PagedResponse<ProjectResponse>> ListAsync(ProjectQuery filter, CancellationToken token)
    {
        var query = db.Projects.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(p => p.Name.Contains(search) || (p.Description != null && p.Description.Contains(search)));
        }
        if (filter.Status.HasValue) query = query.Where(p => p.Status == filter.Status);
        if (filter.ManagerId.HasValue) query = query.Where(p => p.ManagerId == filter.ManagerId);
        var count = await query.CountAsync(token);
        return await Pagination.ReadAsync(query.OrderBy(p => p.Id).Select(ResponseMapping.Project), count, filter, token);
    }

    public async Task<ProjectResponse> GetAsync(int id, CancellationToken token)
        => await db.Projects.AsNoTracking().Where(p => p.Id == id).Select(ResponseMapping.Project).SingleOrDefaultAsync(token)
            ?? throw CrudException.NotFound("Project");

    public async Task<ProjectResponse> CreateAsync(ProjectRequest request, CancellationToken token)
    {
        await EnsureManagerAsync(request.ManagerId!.Value, true, token);
        var project = new Project();
        Apply(project, request);
        db.Projects.Add(project);
        await db.SaveChangesAsync(token);
        return await GetAsync(project.Id, token);
    }

    public async Task<ProjectResponse> UpdateAsync(int id, ProjectRequest request, CancellationToken token)
    {
        var project = await db.Projects.SingleOrDefaultAsync(p => p.Id == id, token)
            ?? throw CrudException.NotFound("Project");
        await EnsureManagerAsync(request.ManagerId!.Value, project.ManagerId != request.ManagerId, token);
        Apply(project, request);
        await db.SaveChangesAsync(token);
        return await GetAsync(id, token);
    }

    public async Task DeleteAsync(int id, CancellationToken token)
    {
        var project = await db.Projects.SingleOrDefaultAsync(p => p.Id == id, token)
            ?? throw CrudException.NotFound("Project");
        db.Projects.Remove(project);
        // Tasks are intentionally not loaded: SQL Server applies the existing CASCADE rule.
        await db.SaveChangesAsync(token);
    }

    private async Task EnsureManagerAsync(int id, bool requireActive, CancellationToken token)
    {
        var manager = await db.Employees.AsNoTracking().Where(e => e.Id == id).Select(e => new { e.IsActive }).SingleOrDefaultAsync(token);
        if (manager is null) throw CrudException.Invalid("ManagerId must reference an existing employee.");
        if (requireActive && !manager.IsActive) throw CrudException.Invalid("A new manager assignment requires an active employee.");
    }

    private static void Apply(Project project, ProjectRequest request)
    {
        project.Name = request.Name!.Trim();
        project.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        project.StartDate = request.StartDate!.Value.UtcDateTime;
        project.EndDate = request.EndDate!.Value.UtcDateTime;
        project.Status = request.Status!.Value;
        project.ManagerId = request.ManagerId!.Value;
    }
}
