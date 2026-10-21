import { PERMISSIONS } from '@/core/auth/permissions';

export interface NavItem {
  to: string;
  label: string;
  group: string;
  /** Item is shown only when the user holds this permission. Omit for items every signed-in user sees. */
  permission?: string;
}

// New features add an entry here (with the permission that unlocks it) and a route in app/App.tsx.
export const NAV_ITEMS: NavItem[] = [
  { to: '/organizations', label: 'Organizations', group: 'Workspace', permission: PERMISSIONS.Organization.View },
  { to: '/employees', label: 'Employees', group: 'Workspace', permission: PERMISSIONS.Employee.View },
  { to: '/roles', label: 'Roles and permissions', group: 'Administration', permission: PERMISSIONS.Role.View },
  { to: '/account', label: 'My account', group: 'Account' },
];
