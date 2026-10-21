using Microsoft.AspNetCore.Mvc;
using WorkforceConnect.Api.Authorization;
using WorkforceConnect.Application.DTOs;
using WorkforceConnect.Application.Interfaces;
using WorkforceConnect.Domain.Security;

namespace WorkforceConnect.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class TeamsController(ITeamService teams) : ControllerBase
{
    /// <summary>The teams of an organization. Read from the database, so a newly added team appears with no frontend change.</summary>
    [HttpGet("organizations/{organizationId:int}/teams")]
    [HasPermission(Permissions.Team.View)]
    public async Task<ActionResult<IReadOnlyList<TeamResponse>>> ListByOrganization(int organizationId, CancellationToken ct) =>
        Ok(await teams.ListByOrganizationAsync(organizationId, ct));

    [HttpGet("teams/{id:int}")]
    [HasPermission(Permissions.Team.View)]
    public async Task<ActionResult<TeamResponse>> Get(int id, CancellationToken ct) =>
        Ok(await teams.GetAsync(id, ct));

    [HttpPost("teams")]
    [HasPermission(Permissions.Team.Create)]
    public async Task<ActionResult<TeamResponse>> Create(CreateTeamRequest request, CancellationToken ct)
    {
        var created = await teams.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("teams/{id:int}")]
    [HasPermission(Permissions.Team.Edit)]
    public async Task<ActionResult<TeamResponse>> Update(int id, UpdateTeamRequest request, CancellationToken ct) =>
        Ok(await teams.UpdateAsync(id, request, ct));

    [HttpDelete("teams/{id:int}")]
    [HasPermission(Permissions.Team.Delete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await teams.DeleteAsync(id, ct);
        return NoContent();
    }
}
