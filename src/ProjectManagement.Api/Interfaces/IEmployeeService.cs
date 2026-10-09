using ProjectManagement.Api.DTOs;

namespace ProjectManagement.Api.Interfaces;

public interface IEmployeeService
{
    Task<PagedResponse<EmployeeResponse>> ListAsync(EmployeeQuery query, CancellationToken token);
    Task<EmployeeResponse> GetAsync(int id, CancellationToken token);
    Task<EmployeeResponse> CreateAsync(EmployeeRequest request, CancellationToken token);
    Task<EmployeeResponse> UpdateAsync(int id, EmployeeRequest request, CancellationToken token);
    Task DeleteAsync(int id, CancellationToken token);
}
