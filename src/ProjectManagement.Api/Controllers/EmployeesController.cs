using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Api.DTOs;
using ProjectManagement.Api.Interfaces;

namespace ProjectManagement.Api.Controllers;

[ApiController]
[Route("api/v1/employees")]
public class EmployeesController(IEmployeeService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<EmployeeResponse>>> List([FromQuery] EmployeeQuery query, CancellationToken token)
        => Ok(await service.ListAsync(query, token));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeResponse>> Get(int id, CancellationToken token)
        => Ok(await service.GetAsync(id, token));

    [HttpPost]
    public async Task<ActionResult<EmployeeResponse>> Create(EmployeeRequest request, CancellationToken token)
    {
        var response = await service.CreateAsync(request, token);
        return CreatedAtAction(nameof(Get), new { id = response.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<EmployeeResponse>> Update(int id, EmployeeRequest request, CancellationToken token)
        => Ok(await service.UpdateAsync(id, request, token));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken token)
    {
        await service.DeleteAsync(id, token);
        return NoContent();
    }
}
