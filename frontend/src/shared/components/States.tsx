import type { ReactNode } from 'react';

export function LoadingState({ label = 'Loading', fullPage = false }: { label?: string; fullPage?: boolean }) {
  return (
    <div className={fullPage ? 'state state--page' : 'state'} role="status" aria-live="polite">
      <span className="spinner" aria-hidden="true" />
      <span>{label}</span>
    </div>
  );
}

export function EmptyState({ title, children, action }: { title: string; children?: ReactNode; action?: ReactNode }) {
  return (
    <div className="state state--empty">
      <h3 className="state__title">{title}</h3>
      {children && <p className="state__text">{children}</p>}
      {action}
    </div>
  );
}

export function ErrorState({ error, onRetry }: { error: Error; onRetry?: () => void }) {
  return (
    <div className="state state--error" role="alert">
      <h3 className="state__title">This couldn't be loaded</h3>
      <p className="state__text">{error.message}</p>
      {onRetry && (
        <button type="button" className="btn btn--secondary" onClick={onRetry}>
          Try again
        </button>
      )}
    </div>
  );
}

export function Skeleton({ lines = 3 }: { lines?: number }) {
  return (
    <div className="skeleton" aria-hidden="true">
      {Array.from({ length: lines }, (_, i) => (
        <span key={i} className="skeleton__line" />
      ))}
    </div>
  );
}
