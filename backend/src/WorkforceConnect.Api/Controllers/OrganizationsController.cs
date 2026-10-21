using Microsoft.AspNetCore.Mvc;
using WorkforceConnect.Api.Authorization;
using WorkforceConnect.Application.DTOs;
using WorkforceConnect.Application.Interfaces;
using WorkforceConnect.Domain.Security;

namespace WorkforceConnect.Api.Controllers;

/// <summary>
/// Each action requires a permission (checked here) and the service additionally applies the
/// caller's data scope (checked there). Both must pass.
/// </summary>
[ApiController]
[Route("api/organizations")]
public sealed class OrganizationsController(IOrganizationService organizations) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Organization.View)]
    public async Task<ActionResult<IReadOnlyList<OrganizationResponse>>> List(CancellationToken ct) =>
        Ok(await organizations.ListAsync(ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.Organization.View)]
    public async Task<ActionResult<OrganizationResponse>> Get(int id, CancellationToken ct) =>
        Ok(await organizations.GetAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.Organization.Create)]
    public async Task<ActionResult<OrganizationResponse>> Create(SaveOrganizationRequest request, CancellationToken ct)
    {
        var created = await organizations.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Organization.Edit)]
    public async Task<ActionResult<OrganizationResponse>> Update(int id, SaveOrganizationRequest request, CancellationToken ct) =>
        Ok(await organizations.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Organization.Delete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await organizations.DeleteAsync(id, ct);
        return NoContent();
    }
}
