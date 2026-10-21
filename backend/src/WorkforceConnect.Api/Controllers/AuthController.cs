using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WorkforceConnect.Api.Extensions;
using WorkforceConnect.Application.DTOs;
using WorkforceConnect.Application.Interfaces;

namespace WorkforceConnect.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService auth, ICurrentUser currentUser) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(ServiceCollectionExtensions.LoginRateLimitPolicy)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct) =>
        Ok(await auth.LoginAsync(request, ct));

    /// <summary>The signed-in user's identity, role, permissions and data scope. The frontend builds its UI from this.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken ct) =>
        Ok(await auth.GetCurrentUserAsync(currentUser.UserId, ct));
}
