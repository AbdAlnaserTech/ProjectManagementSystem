using ProjectManagement.Api.DTOs;

namespace ProjectManagement.Api.Interfaces;

public interface ITaskService
{
    Task<PagedResponse<TaskResponse>> ListAsync(TaskQuery query, CancellationToken token);
    Task<TaskResponse> GetAsync(int id, CancellationToken token);
    Task<TaskResponse> CreateAsync(TaskRequest request, CancellationToken token);
    Task<TaskResponse> UpdateAsync(int id, TaskRequest request, CancellationToken token);
    Task DeleteAsync(int id, CancellationToken token);
}
