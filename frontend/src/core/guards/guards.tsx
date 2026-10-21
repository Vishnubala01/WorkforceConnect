import type { ReactNode } from 'react';
import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '@/core/auth/AuthContext';
import { LoadingState } from '@/shared/components/States';
import { AccessDenied } from '@/shared/components/AccessDenied';

/** Route guard: signed-in users only. Anyone else is sent to the sign-in page and returned afterwards. */
export function RequireAuth() {
  const { status } = useAuth();
  const location = useLocation();

  if (status === 'loading') return <LoadingState label="Restoring your session" fullPage />;
  if (status === 'anonymous') return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />;
  return <Outlet />;
}

/** Route guard: the user must hold the permission. Hiding a route is convenience; the API still checks it. */
export function RequirePermission({ permission, children }: { permission: string; children: ReactNode }) {
  const { can } = useAuth();
  return can(permission) ? <>{children}</> : <AccessDenied />;
}
