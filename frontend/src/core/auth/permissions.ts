// Permission names, matching WorkforceConnect.Domain.Security.Permissions on the backend.
// These are names of capabilities, not of roles: which role holds which permission is data from the API.
export const PERMISSIONS = {
  Organization: {
    View: 'Organization.View',
    Create: 'Organization.Create',
    Edit: 'Organization.Edit',
    Delete: 'Organization.Delete',
  },
  Team: {
    View: 'Team.View',
    Create: 'Team.Create',
    Edit: 'Team.Edit',
    Delete: 'Team.Delete',
  },
  Employee: {
    View: 'Employee.View',
    Create: 'Employee.Create',
    Edit: 'Employee.Edit',
    Delete: 'Employee.Delete',
  },
  Role: {
    View: 'Role.View',
    Create: 'Role.Create',
    Edit: 'Role.Edit',
    Delete: 'Role.Delete',
  },
} as const;
