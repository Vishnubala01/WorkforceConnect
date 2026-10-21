using WorkforceConnect.Application.DTOs;

namespace WorkforceConnect.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

    Task<CurrentUserResponse> GetCurrentUserAsync(int userId, CancellationToken ct = default);
}

public interface IOrganizationService
{
    Task<IReadOnlyList<OrganizationResponse>> ListAsync(CancellationToken ct = default);

    Task<OrganizationResponse> GetAsync(int id, CancellationToken ct = default);

    Task<OrganizationResponse> CreateAsync(SaveOrganizationRequest request, CancellationToken ct = default);

    Task<OrganizationResponse> UpdateAsync(int id, SaveOrganizationRequest request, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);
}

public interface ITeamService
{
    Task<IReadOnlyList<TeamResponse>> ListByOrganizationAsync(int organizationId, CancellationToken ct = default);

    Task<TeamResponse> GetAsync(int id, CancellationToken ct = default);

    Task<TeamResponse> CreateAsync(CreateTeamRequest request, CancellationToken ct = default);

    Task<TeamResponse> UpdateAsync(int id, UpdateTeamRequest request, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);
}

public interface IEmployeeService
{
    Task<PagedResponse<EmployeeResponse>> ListAsync(EmployeeListQuery query, CancellationToken ct = default);

    Task<EmployeeResponse> GetAsync(int id, CancellationToken ct = default);

    Task<EmployeeResponse> CreateAsync(SaveEmployeeRequest request, CancellationToken ct = default);

    Task<EmployeeResponse> UpdateAsync(int id, SaveEmployeeRequest request, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);
}

public interface IRoleService
{
    Task<IReadOnlyList<RoleResponse>> ListRolesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<PermissionResponse>> ListPermissionsAsync(CancellationToken ct = default);
}
