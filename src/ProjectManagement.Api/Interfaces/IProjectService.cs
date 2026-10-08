using ProjectManagement.Api.DTOs;

namespace ProjectManagement.Api.Interfaces;

public interface IProjectService
{
    Task<PagedResponse<ProjectResponse>> ListAsync(ProjectQuery query, CancellationToken token);
    Task<ProjectResponse> GetAsync(int id, CancellationToken token);
    Task<ProjectResponse> CreateAsync(ProjectRequest request, CancellationToken token);
    Task<ProjectResponse> UpdateAsync(int id, ProjectRequest request, CancellationToken token);
    Task DeleteAsync(int id, CancellationToken token);
}
