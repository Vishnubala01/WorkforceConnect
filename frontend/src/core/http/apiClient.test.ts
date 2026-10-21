import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { writeSession } from '@/core/auth/authStorage';
import { ApiError, api, setUnauthorizedHandler } from './apiClient';

function respond(status: number, body?: unknown) {
  return new Response(body === undefined ? null : JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

describe('apiClient', () => {
  const fetchMock = vi.fn();

  beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock);
    fetchMock.mockReset();
  });

  afterEach(() => {
    setUnauthorizedHandler(null);
    vi.unstubAllGlobals();
  });

  it('sends the bearer token when a session exists', async () => {
    writeSession({ token: 'abc123', expiresAt: new Date(Date.now() + 60_000).toISOString() });
    fetchMock.mockResolvedValue(respond(200, []));
    await api.get('/api/organizations');
    const headers = (fetchMock.mock.calls[0]?.[1] as RequestInit).headers as Record<string, string>;
    expect(headers.Authorization).toBe('Bearer abc123');
  });

  it('sends no Authorization header without a session', async () => {
    fetchMock.mockResolvedValue(respond(200, {}));
    await api.get('/api/organizations');
    const headers = (fetchMock.mock.calls[0]?.[1] as RequestInit).headers as Record<string, string>;
    expect(headers.Authorization).toBeUndefined();
  });

  it('turns a problem response into an ApiError with the server detail', async () => {
    fetchMock.mockResolvedValue(respond(409, { title: 'Conflict', detail: 'An organization with that name already exists.' }));
    await expect(api.post('/api/organizations', { name: 'NNX' })).rejects.toMatchObject({
      status: 409,
      message: 'An organization with that name already exists.',
    });
  });

  it('joins model-validation errors into one message', async () => {
    fetchMock.mockResolvedValue(respond(400, { errors: { Name: ['Name is required.'], Email: ['Email is invalid.'] } }));
    const error = await api.post('/api/employees', {}).catch((e: unknown) => e);
    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).message).toBe('Name is required. Email is invalid.');
  });

  it('signs the user out when an authenticated request returns 401', async () => {
    writeSession({ token: 'expired', expiresAt: new Date(Date.now() + 60_000).toISOString() });
    const handler = vi.fn();
    setUnauthorizedHandler(handler);
    fetchMock.mockResolvedValue(respond(401, { detail: 'Authentication is required.' }));
    await expect(api.get('/api/auth/me')).rejects.toBeInstanceOf(ApiError);
    expect(handler).toHaveBeenCalledOnce();
  });

  it('does not sign out on a 401 from the login call (no session yet)', async () => {
    const handler = vi.fn();
    setUnauthorizedHandler(handler);
    fetchMock.mockResolvedValue(respond(401, { detail: 'Invalid email or password.' }));
    await expect(api.post('/api/auth/login', {})).rejects.toMatchObject({ message: 'Invalid email or password.' });
    expect(handler).not.toHaveBeenCalled();
  });

  it('reports an unreachable server in plain words', async () => {
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'));
    await expect(api.get('/api/organizations')).rejects.toMatchObject({ status: 0 });
  });

  it('returns undefined for 204 responses', async () => {
    fetchMock.mockResolvedValue(new Response(null, { status: 204 }));
    await expect(api.delete('/api/teams/3')).resolves.toBeUndefined();
  });
});
