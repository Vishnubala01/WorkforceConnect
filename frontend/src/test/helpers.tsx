import type { ReactElement } from 'react';
import { render } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { AuthContext, type AuthContextValue } from '@/core/auth/AuthContext';
import type { CurrentUser } from '@/core/models/types';
import { ToastProvider } from '@/shared/components/Toast';

export function fakeUser(permissions: string[], overrides: Partial<CurrentUser> = {}): CurrentUser {
  return {
    id: 1,
    name: 'Test User',
    email: 'test@example.com',
    role: 'TestRole',
    permissions,
    dataScope: { level: 'All', organizationIds: [], teamIds: [] },
    employeeId: null,
    organization: null,
    team: null,
    ...overrides,
  };
}

export function renderAs(ui: ReactElement, permissions: string[], route = '/') {
  const user = fakeUser(permissions);
  const granted = new Set(permissions);
  const value: AuthContextValue = {
    status: 'authenticated',
    user,
    login: async () => undefined,
    logout: () => undefined,
    can: (permission) => granted.has(permission),
  };
  return render(
    <MemoryRouter initialEntries={[route]}>
      <ToastProvider>
        <AuthContext.Provider value={value}>{ui}</AuthContext.Provider>
      </ToastProvider>
    </MemoryRouter>,
  );
}
