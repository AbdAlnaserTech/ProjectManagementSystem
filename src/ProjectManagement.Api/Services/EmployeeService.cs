using Microsoft.EntityFrameworkCore;
using ProjectManagement.Api.Data;
using ProjectManagement.Api.DTOs;
using ProjectManagement.Api.Errors;
using ProjectManagement.Api.Interfaces;
using ProjectManagement.Api.Models;

namespace ProjectManagement.Api.Services;

public class EmployeeService(ProjectManagementDbContext db) : IEmployeeService
{
    public async Task<PagedResponse<EmployeeResponse>> ListAsync(EmployeeQuery filter, CancellationToken token)
    {
        var query = db.Employees.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(e => e.FullName.Contains(search) || e.Email.Contains(search));
        }
        if (filter.IsActive.HasValue) query = query.Where(e => e.IsActive == filter.IsActive);
        var count = await query.CountAsync(token);
        return await Pagination.ReadAsync(query.OrderBy(e => e.Id).Select(ResponseMapping.Employee), count, filter, token);
    }

    public async Task<EmployeeResponse> GetAsync(int id, CancellationToken token)
        => await db.Employees.AsNoTracking().Where(e => e.Id == id).Select(ResponseMapping.Employee).SingleOrDefaultAsync(token)
            ?? throw CrudException.NotFound("Employee");

    public async Task<EmployeeResponse> CreateAsync(EmployeeRequest request, CancellationToken token)
    {
        var email = NormalizeEmail(request.Email!);
        await EnsureEmailAvailableAsync(email, null, token);
        var employee = new Employee { FullName = request.FullName!.Trim(), Email = email, IsActive = request.IsActive };
        db.Employees.Add(employee);
        await db.SaveChangesAsync(token);
        return await GetAsync(employee.Id, token);
    }

    public async Task<EmployeeResponse> UpdateAsync(int id, EmployeeRequest request, CancellationToken token)
    {
        var employee = await db.Employees.SingleOrDefaultAsync(e => e.Id == id, token)
            ?? throw CrudException.NotFound("Employee");
        var email = NormalizeEmail(request.Email!);
        await EnsureEmailAvailableAsync(email, id, token);
        employee.FullName = request.FullName!.Trim();
        employee.Email = email;
        employee.IsActive = request.IsActive;
        await db.SaveChangesAsync(token);
        return await GetAsync(id, token);
    }

    public async Task DeleteAsync(int id, CancellationToken token)
    {
        var employee = await db.Employees.SingleOrDefaultAsync(e => e.Id == id, token)
            ?? throw CrudException.NotFound("Employee");
        if (await db.Projects.AnyAsync(p => p.ManagerId == id, token)
            || await db.ProjectTasks.AnyAsync(t => t.AssignedEmployeeId == id, token))
            throw CrudException.Conflict("An employee referenced by a project or task cannot be deleted.");
        db.Employees.Remove(employee);
        await db.SaveChangesAsync(token);
    }

    private async Task EnsureEmailAvailableAsync(string email, int? excludingId, CancellationToken token)
    {
        if (await db.Employees.AsNoTracking().AnyAsync(e => e.Email == email && (!excludingId.HasValue || e.Id != excludingId), token))
            throw CrudException.Conflict("An employee with this email already exists.");
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
