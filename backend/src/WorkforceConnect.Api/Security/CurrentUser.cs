using System.Security.Claims;
using WorkforceConnect.Application.Common;
using WorkforceConnect.Application.Interfaces;

namespace WorkforceConnect.Api.Security;

/// <summary>The caller's identity, read only from the validated token and never from the request body or query.</summary>
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public bool IsAuthenticated =>
        accessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public int UserId
    {
        get
        {
            var user = accessor.HttpContext?.User;
            var value = user?.FindFirst(AppClaims.UserId)?.Value
                        ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(value, out var id) ? id : throw new UnauthenticatedException();
        }
    }
}
