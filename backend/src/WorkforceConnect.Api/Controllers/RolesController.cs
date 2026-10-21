using Microsoft.AspNetCore.Mvc;
using WorkforceConnect.Api.Authorization;
using WorkforceConnect.Application.DTOs;
using WorkforceConnect.Application.Interfaces;
using WorkforceConnect.Domain.Security;

namespace WorkforceConnect.Api.Controllers;

[ApiController]
public sealed class RolesController(IRoleService roles) : ControllerBase
{
    [HttpGet("api/roles")]
    [HasPermission(Permissions.Role.View)]
    public async Task<ActionResult<IReadOnlyList<RoleResponse>>> ListRoles(CancellationToken ct) =>
        Ok(await roles.ListRolesAsync(ct));

    [HttpGet("api/permissions")]
    [HasPermission(Permissions.Role.View)]
    public async Task<ActionResult<IReadOnlyList<PermissionResponse>>> ListPermissions(CancellationToken ct) =>
        Ok(await roles.ListPermissionsAsync(ct));
}
