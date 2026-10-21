import type { ReactNode } from 'react';
import { useAuth } from '@/core/auth/AuthContext';

/** Renders children only when the signed-in user holds the permission. UI convenience only; never a security control. */
export function Can({ permission, children }: { permission: string; children: ReactNode }) {
  const { can } = useAuth();
  return can(permission) ? <>{children}</> : null;
}
