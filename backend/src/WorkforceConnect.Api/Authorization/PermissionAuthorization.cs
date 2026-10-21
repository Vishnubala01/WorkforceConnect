using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using WorkforceConnect.Application.Interfaces;

namespace WorkforceConnect.Api.Authorization;

/// <summary>
/// Protects an endpoint with a permission, for example [HasPermission(Permissions.Organization.Create)].
/// The check reads the user's permissions from the database (User -> Role -> Permissions), so
/// no role name or user id appears anywhere in controller code.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Permission:";

    public HasPermissionAttribute(string permission)
    {
        Permission = permission;
        Policy = PolicyPrefix + permission;
    }

    public string Permission { get; }
}

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

/// <summary>Builds a policy on demand for any "Permission:Xyz" name so permissions never need pre-registering.</summary>
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(HasPermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            var permission = policyName[HasPermissionAttribute.PolicyPrefix.Length..];

            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
        }

        return await base.GetPolicyAsync(policyName);
    }
}

public sealed class PermissionAuthorizationHandler(IPermissionChecker permissions, ICurrentUser currentUser)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!currentUser.IsAuthenticated)
        {
            return;
        }

        if (await permissions.HasPermissionAsync(currentUser.UserId, requirement.Permission))
        {
            context.Succeed(requirement);
        }
    }
}
