// Mirrors the API's DTOs (WorkforceConnect.Application/DTOs). Keep in sync with the backend.

export interface NamedRef {
  id: number;
  name: string;
}

export interface DataScope {
  level: string;
  organizationIds: number[];
  teamIds: number[];
}

export interface CurrentUser {
  id: number;
  name: string;
  email: string;
  role: string;
  permissions: string[];
  dataScope: DataScope;
  employeeId: number | null;
  organization: NamedRef | null;
  team: NamedRef | null;
}

export interface LoginResponse {
  accessToken: string;
  tokenType: string;
  expiresAt: string;
  user: CurrentUser;
}

export interface Organization {
  id: number;
  name: string;
  description: string | null;
  isActive: boolean;
  teamCount: number;
  employeeCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface SaveOrganization {
  name: string;
  description: string | null;
}

export interface Team {
  id: number;
  organizationId: number;
  organizationName: string;
  name: string;
  description: string | null;
  isActive: boolean;
  employeeCount: number;
  projectLeadId: number | null;
  projectLeadName: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateTeam {
  organizationId: number;
  name: string;
  description: string | null;
}

export interface UpdateTeam {
  name: string;
  description: string | null;
}

export interface Employee {
  id: number;
  employeeCode: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  jobTitle?: string;
  organizationId: number;
  organizationName: string;
  teamId: number | null;
  teamName: string | null;
  roleName: string | null;
  projectLeadName: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  secondaryOrganizationIds?: number[];
  secondaryTeamIds?: number[];
}

export interface SaveEmployee {
  employeeCode: string;
  firstName: string;
  lastName: string;
  email: string;
  jobTitle?: string;
  organizationId: number;
  teamId: number | null;
  roleId: number | null;
  secondaryOrganizationIds?: number[];
  secondaryTeamIds?: number[];
}

export interface EmployeeQuery {
  organizationId?: number;
  teamId?: number;
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface Paged<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface Role {
  id: number;
  name: string;
  description: string | null;
  dataScope: string;
  isSystem: boolean;
  userCount: number;
  permissions: string[];
}

export interface Permission {
  id: number;
  name: string;
  module: string;
  description: string | null;
}
