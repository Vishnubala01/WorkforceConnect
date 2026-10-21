// The token lives in sessionStorage: it is cleared when the tab closes and is not shared across tabs.
// The user's permissions are never stored; they are re-read from GET /api/auth/me on every page load.

const KEY = 'wfc.session';

interface StoredSession {
  token: string;
  expiresAt: string;
}

export function readSession(): StoredSession | null {
  try {
    const raw = sessionStorage.getItem(KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as StoredSession;
    if (!parsed.token || Date.parse(parsed.expiresAt) <= Date.now()) {
      sessionStorage.removeItem(KEY);
      return null;
    }
    return parsed;
  } catch {
    return null;
  }
}

export function writeSession(session: StoredSession): void {
  try {
    sessionStorage.setItem(KEY, JSON.stringify(session));
  } catch {
    // Storage can be unavailable (private mode). The user simply has to sign in again after a reload.
  }
}

export function clearSession(): void {
  try {
    sessionStorage.removeItem(KEY);
  } catch {
    // ignore
  }
}
