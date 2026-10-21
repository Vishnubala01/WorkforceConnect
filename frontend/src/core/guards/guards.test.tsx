import { screen } from '@testing-library/react';
import { Route, Routes } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { PERMISSIONS } from '@/core/auth/permissions';
import { AppShell } from '@/layout/AppShell';
import { renderAs } from '@/test/helpers';
import { RequirePermission } from './guards';

describe('RequirePermission', () => {
  it('renders the page when the permission is held', () => {
    renderAs(
      <RequirePermission permission={PERMISSIONS.Role.View}>
        <p>Roles page</p>
      </RequirePermission>,
      [PERMISSIONS.Role.View],
    );
    expect(screen.getByText('Roles page')).toBeInTheDocument();
  });

  it('shows an access message instead of the page when it is not', () => {
    renderAs(
      <RequirePermission permission={PERMISSIONS.Role.View}>
        <p>Roles page</p>
      </RequirePermission>,
      [PERMISSIONS.Employee.View],
    );
    expect(screen.queryByText('Roles page')).not.toBeInTheDocument();
    expect(screen.getByRole('alert')).toHaveTextContent("You don't have access to this page");
  });
});

describe('navigation generated from permissions', () => {
  function shell(permissions: string[]) {
    renderAs(
      <Routes>
        <Route element={<AppShell />}>
          <Route index element={<p>Home</p>} />
        </Route>
      </Routes>,
      permissions,
    );
  }

  it('shows every area to a user who holds the permissions', () => {
    shell([PERMISSIONS.Organization.View, PERMISSIONS.Employee.View, PERMISSIONS.Role.View]);
    const nav = screen.getByRole('navigation', { name: 'Primary' });
    expect(nav).toHaveTextContent('Organizations');
    expect(nav).toHaveTextContent('Employees');
    expect(nav).toHaveTextContent('Roles and permissions');
  });

  it('shows only the areas a limited user can use', () => {
    shell([PERMISSIONS.Employee.View]);
    const nav = screen.getByRole('navigation', { name: 'Primary' });
    expect(nav).toHaveTextContent('Employees');
    expect(nav).not.toHaveTextContent('Organizations');
    expect(nav).not.toHaveTextContent('Roles and permissions');
    expect(nav).toHaveTextContent('My account');
  });
});
