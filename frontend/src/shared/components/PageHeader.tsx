import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';

export interface Crumb {
  label: string;
  to?: string;
}

/** The breadcrumb reads as the organization path: Application, then organization, then team. */
export function PageHeader({ title, subtitle, crumbs, actions }: { title: string; subtitle?: string; crumbs?: Crumb[]; actions?: ReactNode }) {
  return (
    <header className="page-header">
      {crumbs && crumbs.length > 0 && (
        <nav aria-label="Breadcrumb" className="crumbs">
          <ol>
            {crumbs.map((crumb, index) => (
              <li key={`${crumb.label}-${index}`} aria-current={index === crumbs.length - 1 ? 'page' : undefined}>
                {crumb.to ? <Link to={crumb.to}>{crumb.label}</Link> : crumb.label}
              </li>
            ))}
          </ol>
        </nav>
      )}
      <div className="page-header__row">
        <div>
          <h1 className="page-header__title">{title}</h1>
          {subtitle && <p className="page-header__subtitle">{subtitle}</p>}
        </div>
        {actions && <div className="page-header__actions">{actions}</div>}
      </div>
    </header>
  );
}
