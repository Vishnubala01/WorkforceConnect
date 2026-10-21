import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthProvider } from '@/core/auth/AuthContext';
import { AppRoutes } from '@/app/App';
import { ToastProvider } from '@/shared/components/Toast';
import { fakeUser } from '@/test/helpers';

function respond(status: number, body: unknown) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
}

function renderApp(route = '/login') {
  return render(
    <MemoryRouter initialEntries={[route]}>
      <ToastProvider>
        <AuthProvider>
          <AppRoutes />
        </AuthProvider>
      </ToastProvider>
    </MemoryRouter>,
  );
}

describe('sign-in flow', () => {
  const fetchMock = vi.fn();
  beforeEach(() => {
    fetchMock.mockReset();
    vi.stubGlobal('fetch', fetchMock);
  });
  afterEach(() => vi.unstubAllGlobals());

  it('shows the server message when credentials are wrong', async () => {
    fetchMock.mockResolvedValue(respond(401, { detail: 'Invalid email or password.' }));
    renderApp();
    await userEvent.type(screen.getByLabelText('Email'), 'a@b.co');
    await userEvent.type(screen.getByLabelText('Password'), 'nope');
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Invalid email or password.');
  });

  it('signs in, stores the session and lands on the first area the user may use', async () => {
    const user = fakeUser(['Employee.View']);
    fetchMock.mockImplementation((url: string) => {
      if (url.endsWith('/api/auth/login')) {
        return Promise.resolve(respond(200, { accessToken: 't', tokenType: 'Bearer', expiresAt: new Date(Date.now() + 3600_000).toISOString(), user }));
      }
      if (url.includes('/api/employees')) return Promise.resolve(respond(200, { items: [], total: 0, page: 1, pageSize: 25 }));
      return Promise.resolve(respond(200, []));
    });
    renderApp();
    await userEvent.type(screen.getByLabelText('Email'), 'a@b.co');
    await userEvent.type(screen.getByLabelText('Password'), 'secret');
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));
    await waitFor(() => expect(screen.getByRole('heading', { name: 'Employees' })).toBeInTheDocument());
    expect(sessionStorage.getItem('wfc.session')).toContain('"token":"t"');
  });

  it('sends anonymous visitors to the sign-in page', () => {
    renderApp('/organizations');
    expect(screen.getByRole('heading', { name: 'Sign in' })).toBeInTheDocument();
  });
});
