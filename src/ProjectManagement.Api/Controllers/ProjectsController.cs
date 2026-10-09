using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Api.DTOs;
using ProjectManagement.Api.Interfaces;

namespace ProjectManagement.Api.Controllers;

[ApiController]
[Route("api/v1/projects")]
public class ProjectsController(IProjectService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ProjectResponse>>> List([FromQuery] ProjectQuery query, CancellationToken token)
        => Ok(await service.ListAsync(query, token));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjectResponse>> Get(int id, CancellationToken token)
        => Ok(await service.GetAsync(id, token));

    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> Create(ProjectRequest request, CancellationToken token)
    {
        var response = await service.CreateAsync(request, token);
        return CreatedAtAction(nameof(Get), new { id = response.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProjectResponse>> Update(int id, ProjectRequest request, CancellationToken token)
        => Ok(await service.UpdateAsync(id, request, token));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken token)
    {
        await service.DeleteAsync(id, token);
        return NoContent();
    }
}
