using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Api.DTOs;
using ProjectManagement.Api.Interfaces;

namespace ProjectManagement.Api.Controllers;

[ApiController]
[Route("api/v1/tasks")]
public class TasksController(ITaskService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<TaskResponse>>> List([FromQuery] TaskQuery query, CancellationToken token)
        => Ok(await service.ListAsync(query, token));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TaskResponse>> Get(int id, CancellationToken token)
        => Ok(await service.GetAsync(id, token));

    [HttpPost]
    public async Task<ActionResult<TaskResponse>> Create(TaskRequest request, CancellationToken token)
    {
        var response = await service.CreateAsync(request, token);
        return CreatedAtAction(nameof(Get), new { id = response.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TaskResponse>> Update(int id, TaskRequest request, CancellationToken token)
        => Ok(await service.UpdateAsync(id, request, token));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken token)
    {
        await service.DeleteAsync(id, token);
        return NoContent();
    }
}
