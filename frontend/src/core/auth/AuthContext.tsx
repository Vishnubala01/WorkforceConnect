import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { setUnauthorizedHandler } from '@/core/http/apiClient';
import { authService } from '@/core/services/services';
import type { CurrentUser } from '@/core/models/types';
import { clearSession, readSession, writeSession } from './authStorage';

export type AuthStatus = 'loading' | 'authenticated' | 'anonymous';

export interface AuthContextValue {
  status: AuthStatus;
  user: CurrentUser | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  /** True when the signed-in user holds the permission. This only shapes the UI; the API enforces it independently. */
  can: (permission: string) => boolean;
}

export const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<AuthStatus>(() => (readSession() ? 'loading' : 'anonymous'));
  const [user, setUser] = useState<CurrentUser | null>(null);

  const logout = useCallback(() => {
    clearSession();
    setUser(null);
    setStatus('anonymous');
  }, []);

  useEffect(() => {
    setUnauthorizedHandler(logout);
    return () => setUnauthorizedHandler(null);
  }, [logout]);

  // Restore the session on page load: the token is kept, the user and permissions are re-read from the API.
  useEffect(() => {
    if (status !== 'loading') return;
    const controller = new AbortController();
    authService
      .me(controller.signal)
      .then((me) => {
        setUser(me);
        setStatus('authenticated');
      })
      .catch(() => {
        if (!controller.signal.aborted) logout();
      });
    return () => controller.abort();
  }, [status, logout]);

  const login = useCallback(async (email: string, password: string) => {
    const response = await authService.login(email, password);
    writeSession({ token: response.accessToken, expiresAt: response.expiresAt });
    setUser(response.user);
    setStatus('authenticated');
  }, []);

  const value = useMemo<AuthContextValue>(() => {
    const granted = new Set(user?.permissions ?? []);
    return { status, user, login, logout, can: (permission) => granted.has(permission) };
  }, [status, user, login, logout]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used inside <AuthProvider>.');
  return context;
}
