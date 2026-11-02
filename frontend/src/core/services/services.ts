import { api } from '@/core/http/apiClient';
import type {
  CreateTeam,
  CurrentUser,
  Employee,
  EmployeeQuery,
  LoginResponse,
  Organization,
  Paged,
  Permission,
  Role,
  SaveEmployee,
  SaveOrganization,
  Team,
  UpdateTeam,
} from '@/core/models/types';

// Every list the UI shows (organizations, teams, roles) is fetched from the API. Nothing is hard-coded.

export const authService = {
  login: (email: string, password: string) => api.post<LoginResponse>('/api/auth/login', { email, password }),
  me: (signal?: AbortSignal) => api.get<CurrentUser>('/api/auth/me', { signal }),
};

export const organizationService = {
  list: (signal?: AbortSignal) => api.get<Organization[]>('/api/organizations', { signal }),
  get: (id: number, signal?: AbortSignal) => api.get<Organization>(`/api/organizations/${id}`, { signal }),
  create: (body: SaveOrganization) => api.post<Organization>('/api/organizations', body),
  update: (id: number, body: SaveOrganization) => api.put<Organization>(`/api/organizations/${id}`, body),
  remove: (id: number) => api.delete(`/api/organizations/${id}`),
};

export const teamService = {
  listByOrganization: (organizationId: number, signal?: AbortSignal) =>
    api.get<Team[]>(`/api/organizations/${organizationId}/teams`, { signal }),
  get: (id: number, signal?: AbortSignal) => api.get<Team>(`/api/teams/${id}`, { signal }),
  create: (body: CreateTeam) => api.post<Team>('/api/teams', body),
  update: (id: number, body: UpdateTeam) => api.put<Team>(`/api/teams/${id}`, body),
  remove: (id: number) => api.delete(`/api/teams/${id}`),
};

export const employeeService = {
  list: (query: EmployeeQuery, signal?: AbortSignal) =>
    api.get<Paged<Employee>>('/api/employees', { query: { ...query }, signal }),
  create: (body: SaveEmployee) => api.post<Employee>('/api/employees', body),
  update: (id: number, body: SaveEmployee) => api.put<Employee>(`/api/employees/${id}`, body),
  remove: (id: number) => api.delete(`/api/employees/${id}`),
};

export const roleService = {
  listRoles: (signal?: AbortSignal) => api.get<Role[]>('/api/roles', { signal }),
  listPermissions: (signal?: AbortSignal) => api.get<Permission[]>('/api/permissions', { signal }),
};

export const dailyStatusService = {
  list: (query: import('@/core/models/types').DailyStatusQuery, signal?: AbortSignal) =>
    api.get<import('@/core/models/types').Paged<import('@/core/models/types').DailyStatus>>('/api/dailystatus', {
      query: { ...query },
      signal,
    }),
  get: (id: number, signal?: AbortSignal) =>
    api.get<import('@/core/models/types').DailyStatus>(`/api/dailystatus/${id}`, { signal }),
  create: (body: import('@/core/models/types').SaveDailyStatus) =>
    api.post<import('@/core/models/types').DailyStatus>('/api/dailystatus', body),
  update: (id: number, body: import('@/core/models/types').SaveDailyStatus) =>
    api.put<import('@/core/models/types').DailyStatus>(`/api/dailystatus/${id}`, body),
  remove: (id: number) => api.delete(`/api/dailystatus/${id}`),
  exportExcel: async (query: import('@/core/models/types').DailyStatusQuery) => {
    const { blob, filename } = await api.downloadBlob('/api/dailystatus/export/excel', query as Record<string, string | number>);
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename || 'Team_Daily_Status_Tracker.xls';
    document.body.appendChild(a);
    a.click();
    a.remove();
    window.URL.revokeObjectURL(url);
  },
  exportCsv: async (query: import('@/core/models/types').DailyStatusQuery) => {
    const { blob, filename } = await api.downloadBlob('/api/dailystatus/export/csv', query as Record<string, string | number>);
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename || 'Team_Daily_Status_Tracker.csv';
    document.body.appendChild(a);
    a.click();
    a.remove();
    window.URL.revokeObjectURL(url);
  },
};
