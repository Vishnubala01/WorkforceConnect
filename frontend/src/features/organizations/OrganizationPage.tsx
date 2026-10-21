import { useState } from 'react';
import { Link, useParams, useSearchParams } from 'react-router-dom';
import { useAuth } from '@/core/auth/AuthContext';
import { PERMISSIONS } from '@/core/auth/permissions';
import type { Team } from '@/core/models/types';
import { organizationService, teamService } from '@/core/services/services';
import { TeamFormModal } from '@/features/teams/TeamFormModal';
import { Can } from '@/shared/components/Can';
import { ConfirmDialog } from '@/shared/components/Modal';
import { PageHeader } from '@/shared/components/PageHeader';
import { EmptyState, ErrorState, LoadingState } from '@/shared/components/States';
import { useToast } from '@/shared/components/Toast';
import { useAsync } from '@/shared/hooks/hooks';
import { accentFor, errorMessage, plural } from '@/shared/utils/format';

export function OrganizationPage() {
  const { orgId } = useParams();
  const id = Number(orgId);
  const { can } = useAuth();
  const toast = useToast();
  const [params, setParams] = useSearchParams();
  const selectedId = Number(params.get('team')) || null;

  const org = useAsync((signal) => organizationService.get(id, signal), [id]);
  const canViewTeams = can(PERMISSIONS.Team.View);
  // Teams always come from the API, so a team added later appears here with no frontend change.
  const teams = useAsync((signal) => (canViewTeams ? teamService.listByOrganization(id, signal) : Promise.resolve([] as Team[])), [id, canViewTeams]);

  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<Team | null>(null);
  const [deleting, setDeleting] = useState<Team | null>(null);
  const [deleteBusy, setDeleteBusy] = useState(false);

  const selected = teams.data?.find((t) => t.id === selectedId) ?? null;

  function select(team: Team | null) {
    const next = new URLSearchParams(params);
    if (team) next.set('team', String(team.id));
    else next.delete('team');
    setParams(next, { replace: true });
  }

  async function confirmDelete() {
    if (!deleting) return;
    setDeleteBusy(true);
    try {
      await teamService.remove(deleting.id);
      toast.success(`${deleting.name} deleted`);
      if (selectedId === deleting.id) select(null);
      setDeleting(null);
      teams.reload();
      org.reload();
    } catch (e) {
      toast.error(errorMessage(e));
    } finally {
      setDeleteBusy(false);
    }
  }

  if (!Number.isInteger(id)) return <ErrorState error={new Error('That organization link is not valid.')} />;
  if (org.loading && !org.data) return <LoadingState label="Loading organization" />;
  if (org.error && !org.data) {
    return (
      <>
        <ErrorState error={org.error} onRetry={org.reload} />
        <p className="muted center">
          <Link to="/organizations">Back to organizations</Link>
        </p>
      </>
    );
  }
  const organization = org.data;
  if (!organization) return null;

  return (
    <>
      <PageHeader
        title={organization.name}
        subtitle={organization.description ?? undefined}
        crumbs={[
          { label: 'Application' },
          { label: 'Organizations', to: '/organizations' },
          { label: organization.name, to: selected ? `/organizations/${organization.id}` : undefined },
          ...(selected ? [{ label: selected.name }] : []),
        ]}
        actions={
          <Can permission={PERMISSIONS.Team.Create}>
            <button type="button" className="btn btn--primary" onClick={() => setCreating(true)}>
              New team
            </button>
          </Can>
        }
      />

      <div className={selected ? 'split split--open' : 'split'}>
        <section aria-labelledby="teams-title">
          <h2 id="teams-title" className="section-title">
            Teams
          </h2>
          {!canViewTeams ? (
            <EmptyState title="Teams aren't available to your role" />
          ) : teams.loading && !teams.data ? (
            <LoadingState label="Loading teams" />
          ) : teams.error && !teams.data ? (
            <ErrorState error={teams.error} onRetry={teams.reload} />
          ) : (teams.data ?? []).length === 0 ? (
            <EmptyState
              title={`${organization.name} has no teams yet`}
              action={
                <Can permission={PERMISSIONS.Team.Create}>
                  <button type="button" className="btn btn--primary" onClick={() => setCreating(true)}>
                    Create a team
                  </button>
                </Can>
              }
            >
              Teams you add to this organization will appear here.
            </EmptyState>
          ) : (
            <ul className="team-grid" style={{ ['--accent-org' as string]: accentFor(organization.id) }}>
              {(teams.data ?? []).map((team) => (
                <li key={team.id}>
                  <button
                    type="button"
                    className={team.id === selectedId ? 'team-tile team-tile--selected' : 'team-tile'}
                    aria-pressed={team.id === selectedId}
                    onClick={() => select(team.id === selectedId ? null : team)}
                  >
                    <span className="team-tile__name">{team.name}</span>
                    <span className="team-tile__meta">{plural(team.employeeCount, 'employee')}</span>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </section>

        {selected && (
          <aside className="detail" aria-label={`${selected.name} details`}>
            <div className="detail__head">
              <h2 className="detail__title">{selected.name}</h2>
              <button type="button" className="icon-btn" aria-label="Close team details" onClick={() => select(null)}>
                ×
              </button>
            </div>
            <p className="detail__desc">{selected.description || 'No description'}</p>
            <dl className="details">
              <div className="details__row">
                <dt>Organization</dt>
                <dd>{selected.organizationName}</dd>
              </div>
              <div className="details__row">
                <dt>Project Lead</dt>
                <dd>{selected.projectLeadName ?? <span className="muted">—</span>}</dd>
              </div>
              <div className="details__row">
                <dt>Employees</dt>
                <dd>{selected.employeeCount}</dd>
              </div>
            </dl>
            <div className="detail__actions">
              <Can permission={PERMISSIONS.Employee.View}>
                <Link className="btn btn--secondary" to={`/employees?organizationId=${selected.organizationId}&teamId=${selected.id}`}>
                  View employees
                </Link>
              </Can>
              <Can permission={PERMISSIONS.Team.Edit}>
                <button type="button" className="btn btn--secondary" onClick={() => setEditing(selected)}>
                  Edit team
                </button>
              </Can>
              <Can permission={PERMISSIONS.Team.Delete}>
                <button type="button" className="btn btn--ghost-danger" onClick={() => setDeleting(selected)}>
                  Delete team
                </button>
              </Can>
            </div>
          </aside>
        )}
      </div>

      <TeamFormModal
        open={creating || editing !== null}
        organizationId={organization.id}
        organizationName={organization.name}
        team={editing}
        onClose={() => {
          setCreating(false);
          setEditing(null);
        }}
        onSaved={(saved) => {
          toast.success(editing ? `${saved.name} updated` : `${saved.name} created`);
          setCreating(false);
          setEditing(null);
          teams.reload();
          org.reload();
        }}
      />

      <ConfirmDialog
        open={deleting !== null}
        title="Delete team"
        message={
          <>
            Delete <strong>{deleting?.name}</strong>? A team that still has active employees can't be deleted.
          </>
        }
        confirmLabel="Delete team"
        busy={deleteBusy}
        onConfirm={confirmDelete}
        onCancel={() => setDeleting(null)}
      />
    </>
  );
}
