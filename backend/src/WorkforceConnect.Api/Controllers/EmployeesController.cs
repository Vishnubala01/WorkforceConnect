using Microsoft.AspNetCore.Mvc;
using WorkforceConnect.Api.Authorization;
using WorkforceConnect.Application.DTOs;
using WorkforceConnect.Application.Interfaces;
using WorkforceConnect.Domain.Security;

namespace WorkforceConnect.Api.Controllers;

[ApiController]
[Route("api/employees")]
public sealed class EmployeesController(IEmployeeService employees) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Employee.View)]
    public async Task<ActionResult<PagedResponse<EmployeeResponse>>> List([FromQuery] EmployeeListQuery query, CancellationToken ct) =>
        Ok(await employees.ListAsync(query, ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.Employee.View)]
    public async Task<ActionResult<EmployeeResponse>> Get(int id, CancellationToken ct) =>
        Ok(await employees.GetAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.Employee.Create)]
    public async Task<ActionResult<EmployeeResponse>> Create(SaveEmployeeRequest request, CancellationToken ct)
    {
        var created = await employees.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Employee.Edit)]
    public async Task<ActionResult<EmployeeResponse>> Update(int id, SaveEmployeeRequest request, CancellationToken ct) =>
        Ok(await employees.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Employee.Delete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await employees.DeleteAsync(id, ct);
        return NoContent();
    }
}
