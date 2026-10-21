using Microsoft.Extensions.DependencyInjection;
using WorkforceConnect.Application.Interfaces;
using WorkforceConnect.Application.Security;
using WorkforceConnect.Application.Services;

namespace WorkforceConnect.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        // Scoped: both cache their result for the duration of one request.
        services.AddScoped<IPermissionChecker, PermissionChecker>();
        services.AddScoped<IDataScopeService, DataScopeService>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<ITeamService, TeamService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IRoleService, RoleService>();

        return services;
    }
}
