import { readSession } from '@/core/auth/authStorage';

export class ApiError extends Error {
  readonly status: number;

  constructor(status: number, message: string) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
  }
}

const BASE_URL = (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? '';

let onUnauthorized: (() => void) | null = null;

/** Called once by the auth provider so an expired or revoked token sends the user back to sign-in. */
export function setUnauthorizedHandler(handler: (() => void) | null): void {
  onUnauthorized = handler;
}

type Query = Record<string, string | number | boolean | undefined | null>;

interface RequestOptions {
  body?: unknown;
  query?: Query;
  signal?: AbortSignal;
}

function buildUrl(path: string, query?: Query): string {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(query ?? {})) {
    if (value !== undefined && value !== null && value !== '') params.set(key, String(value));
  }
  const qs = params.toString();
  return `${BASE_URL}${path}${qs ? `?${qs}` : ''}`;
}

async function toError(response: Response): Promise<ApiError> {
  let message = 'Something went wrong. Try again.';
  try {
    const problem = (await response.json()) as {
      detail?: string;
      title?: string;
      errors?: Record<string, string[]>;
    };
    if (problem.errors) {
      // ASP.NET model-validation failures: { errors: { Field: ["message"] } }
      message = Object.values(problem.errors).flat().join(' ');
    } else if (problem.detail) {
      message = problem.detail;
    } else if (problem.title) {
      message = problem.title;
    }
  } catch {
    // Non-JSON body: keep the generic message.
  }
  if (response.status === 403) message = message || 'You do not have permission to do that.';
  return new ApiError(response.status, message);
}

async function request<T>(method: string, path: string, options: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' };
  const session = readSession();
  if (session) headers.Authorization = `Bearer ${session.token}`;
  if (options.body !== undefined) headers['Content-Type'] = 'application/json';

  let response: Response;
  try {
    response = await fetch(buildUrl(path, options.query), {
      method,
      headers,
      body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
      signal: options.signal,
    });
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') throw error;
    throw new ApiError(0, "Can't reach the server. Check your connection and try again.");
  }

  if (!response.ok) {
    // A 401 on a request that carried a token means the session ended. The login call itself is excluded.
    if (response.status === 401 && session) onUnauthorized?.();
    throw await toError(response);
  }

  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

export const api = {
  get: <T>(path: string, options?: Omit<RequestOptions, 'body'>) => request<T>('GET', path, options),
  post: <T>(path: string, body?: unknown, options?: Omit<RequestOptions, 'body'>) =>
    request<T>('POST', path, { ...options, body }),
  put: <T>(path: string, body?: unknown, options?: Omit<RequestOptions, 'body'>) =>
    request<T>('PUT', path, { ...options, body }),
  delete: <T = void>(path: string, options?: Omit<RequestOptions, 'body'>) => request<T>('DELETE', path, options),
};
