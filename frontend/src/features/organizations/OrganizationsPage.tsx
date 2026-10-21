import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { PERMISSIONS } from '@/core/auth/permissions';
import type { Organization } from '@/core/models/types';
import { organizationService } from '@/core/services/services';
import { Can } from '@/shared/components/Can';
import { ConfirmDialog } from '@/shared/components/Modal';
import { PageHeader } from '@/shared/components/PageHeader';
import { EmptyState, ErrorState, LoadingState } from '@/shared/components/States';
import { useToast } from '@/shared/components/Toast';
import { useAsync } from '@/shared/hooks/hooks';
import { accentFor, errorMessage, plural } from '@/shared/utils/format';
import { OrganizationFormModal } from './OrganizationFormModal';

export function OrganizationsPage() {
  const toast = useToast();
  const { data, error, loading, reload } = useAsync((signal) => organizationService.list(signal), []);
  const [query, setQuery] = useState('');
  const [editing, setEditing] = useState<Organization | null>(null);
  const [creating, setCreating] = useState(false);
  const [deleting, setDeleting] = useState<Organization | null>(null);
  const [deleteBusy, setDeleteBusy] = useState(false);

  const organizations = useMemo(() => {
    const term = query.trim().toLowerCase();
    return (data ?? []).filter((o) => !term || o.name.toLowerCase().includes(term));
  }, [data, query]);

  async function confirmDelete() {
    if (!deleting) return;
    setDeleteBusy(true);
    try {
      await organizationService.remove(deleting.id);
      toast.success(`${deleting.name} deleted`);
      setDeleting(null);
      reload();
    } catch (e) {
      toast.error(errorMessage(e));
    } finally {
      setDeleteBusy(false);
    }
  }

  return (
    <>
      <PageHeader
        title="Organizations"
        subtitle="Choose an organization to see its teams."
        crumbs={[{ label: 'Application' }, { label: 'Organizations' }]}
        actions={
          <Can permission={PERMISSIONS.Organization.Create}>
            <button type="button" className="btn btn--primary" onClick={() => setCreating(true)}>
              New organization
            </button>
          </Can>
        }
      />

      {(data?.length ?? 0) > 6 && (
        <div className="toolbar">
          <input
            className="input toolbar__search"
            type="search"
            placeholder="Search organizations"
            aria-label="Search organizations"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
          />
        </div>
      )}

      {loading && !data ? (
        <LoadingState label="Loading organizations" />
      ) : error && !data ? (
        <ErrorState error={error} onRetry={reload} />
      ) : organizations.length === 0 ? (
        <EmptyState
          title={query ? 'No organizations match your search' : 'No organizations yet'}
          action={
            !query && (
              <Can permission={PERMISSIONS.Organization.Create}>
                <button type="button" className="btn btn--primary" onClick={() => setCreating(true)}>
                  Create the first organization
                </button>
              </Can>
            )
          }
        >
          {query ? 'Try a different name.' : 'Organizations you can access will appear here.'}
        </EmptyState>
      ) : (
        <ul className="org-grid">
          {organizations.map((org) => (
            <li key={org.id} className="org-card" style={{ ['--accent-org' as string]: accentFor(org.id) }}>
              <div className="org-card__body">
                <h2 className="org-card__name">{org.name}</h2>
                <p className="org-card__desc">{org.description || 'No description'}</p>
                <dl className="org-card__figures">
                  <div>
                    <dt>{org.teamCount === 1 ? 'Team' : 'Teams'}</dt>
                    <dd>{org.teamCount}</dd>
                  </div>
                  <div>
                    <dt>{org.employeeCount === 1 ? 'Employee' : 'Employees'}</dt>
                    <dd>{org.employeeCount}</dd>
                  </div>
                </dl>
              </div>
              <div className="org-card__footer">
                <Link className="btn btn--secondary" to={`/organizations/${org.id}`} aria-label={`View organization ${org.name}`}>
                  View organization
                </Link>
                <div className="org-card__tools">
                  <Can permission={PERMISSIONS.Organization.Edit}>
                    <button type="button" className="link-btn" onClick={() => setEditing(org)} aria-label={`Edit ${org.name}`}>
                      Edit
                    </button>
                  </Can>
                  <Can permission={PERMISSIONS.Organization.Delete}>
                    <button type="button" className="link-btn link-btn--danger" onClick={() => setDeleting(org)} aria-label={`Delete ${org.name}`}>
                      Delete
                    </button>
                  </Can>
                </div>
              </div>
              <span className="visually-hidden">
                {plural(org.teamCount, 'team')}, {plural(org.employeeCount, 'employee')}
              </span>
            </li>
          ))}
        </ul>
      )}

      <OrganizationFormModal
        open={creating || editing !== null}
        organization={editing}
        onClose={() => {
          setCreating(false);
          setEditing(null);
        }}
        onSaved={(saved) => {
          toast.success(editing ? `${saved.name} updated` : `${saved.name} created`);
          setCreating(false);
          setEditing(null);
          reload();
        }}
      />

      <ConfirmDialog
        open={deleting !== null}
        title="Delete organization"
        message={
          <>
            Delete <strong>{deleting?.name}</strong>? An organization that still has active teams or employees can't be deleted.
          </>
        }
        confirmLabel="Delete organization"
        busy={deleteBusy}
        onConfirm={confirmDelete}
        onCancel={() => setDeleting(null)}
      />
    </>
  );
}
