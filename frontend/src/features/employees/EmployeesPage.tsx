import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useAuth } from '@/core/auth/AuthContext';
import { PERMISSIONS } from '@/core/auth/permissions';
import type { Employee } from '@/core/models/types';
import { employeeService, organizationService, teamService } from '@/core/services/services';
import { Can } from '@/shared/components/Can';
import { SelectField } from '@/shared/components/Fields';
import { ConfirmDialog } from '@/shared/components/Modal';
import { PageHeader } from '@/shared/components/PageHeader';
import { EmptyState, ErrorState, LoadingState } from '@/shared/components/States';
import { useToast } from '@/shared/components/Toast';
import { useAsync, useDebounced } from '@/shared/hooks/hooks';
import { errorMessage } from '@/shared/utils/format';
import { EmployeeFormModal } from './EmployeeFormModal';

const PAGE_SIZE = 25;

export function EmployeesPage() {
  const { can } = useAuth();
  const toast = useToast();
  const [params, setParams] = useSearchParams();

  // Filters live in the URL so a filtered list can be bookmarked or linked from a team.
  const organizationId = Number(params.get('organizationId')) || undefined;
  const teamId = Number(params.get('teamId')) || undefined;
  const page = Math.max(1, Number(params.get('page')) || 1);
  const [searchText, setSearchText] = useState(params.get('search') ?? '');
  const search = useDebounced(searchText.trim());

  const canViewOrganizations = can(PERMISSIONS.Organization.View);
  const organizations = useAsync((signal) => (canViewOrganizations ? organizationService.list(signal) : Promise.resolve([])), [canViewOrganizations]);
  const canViewTeams = can(PERMISSIONS.Team.View);
  const teams = useAsync(
    (signal) => (organizationId && canViewTeams ? teamService.listByOrganization(organizationId, signal) : Promise.resolve([])),
    [organizationId, canViewTeams],
  );
  const employees = useAsync(
    (signal) => employeeService.list({ organizationId, teamId, search: search || undefined, page, pageSize: PAGE_SIZE }, signal),
    [organizationId, teamId, search, page],
  );

  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<Employee | null>(null);
  const [deleting, setDeleting] = useState<Employee | null>(null);
  const [deleteBusy, setDeleteBusy] = useState(false);

  function updateParams(changes: Record<string, string | undefined>) {
    const next = new URLSearchParams(params);
    for (const [key, value] of Object.entries(changes)) {
      if (value) next.set(key, value);
      else next.delete(key);
    }
    setParams(next, { replace: true });
  }

  // Keep the search term in the URL once the debounce settles; restart at page 1 when it changes.
  const urlSearch = params.get('search') ?? '';
  useEffect(() => {
    if (search !== urlSearch) updateParams({ search: search || undefined, page: undefined });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [search]);

  const total = employees.data?.total ?? 0;
  const first = total === 0 ? 0 : (page - 1) * PAGE_SIZE + 1;
  const last = Math.min(page * PAGE_SIZE, total);
  const filtered = Boolean(organizationId || teamId || search);

  async function confirmDelete() {
    if (!deleting) return;
    setDeleteBusy(true);
    try {
      await employeeService.remove(deleting.id);
      toast.success(`${deleting.fullName} deleted`);
      setDeleting(null);
      employees.reload();
    } catch (e) {
      toast.error(errorMessage(e));
    } finally {
      setDeleteBusy(false);
    }
  }

  return (
    <>
      <PageHeader
        title="Employees"
        subtitle="People across the organizations you can access."
        crumbs={[{ label: 'Application' }, { label: 'Employees' }]}
        actions={
          <Can permission={PERMISSIONS.Employee.Create}>
            <button type="button" className="btn btn--primary" onClick={() => setCreating(true)}>
              New employee
            </button>
          </Can>
        }
      />

      <div className="toolbar" role="search">
        <input
          className="input toolbar__search"
          type="search"
          placeholder="Search by name, email or code"
          aria-label="Search employees"
          value={searchText}
          onChange={(e) => setSearchText(e.target.value)}
        />
        {canViewOrganizations && (
          <div className="toolbar__select">
            <SelectField
              label="Organization"
              value={organizationId ?? ''}
              onChange={(e) => updateParams({ organizationId: e.target.value || undefined, teamId: undefined, page: undefined })}
            >
              <option value="">All organizations</option>
              {(organizations.data ?? []).map((o) => (
                <option key={o.id} value={o.id}>
                  {o.name}
                </option>
              ))}
            </SelectField>
          </div>
        )}
        {canViewTeams && (
          <div className="toolbar__select">
            <SelectField
              label="Team"
              value={teamId ?? ''}
              disabled={!organizationId}
              onChange={(e) => updateParams({ teamId: e.target.value || undefined, page: undefined })}
            >
              <option value="">{organizationId ? 'All teams' : 'Choose organization first'}</option>
              {(teams.data ?? []).map((t) => (
                <option key={t.id} value={t.id}>
                  {t.name}
                </option>
              ))}
            </SelectField>
          </div>
        )}
        {teamId && canViewTeams && (
          <div className="toolbar__select" style={{ display: 'flex', flexDirection: 'column' }}>
            <span className="field__label">Project Lead</span>
            <div style={{ padding: '8px 12px', background: 'var(--bg)', border: '1px solid var(--line)', borderRadius: 'var(--radius)', height: '40px', display: 'flex', alignItems: 'center' }}>
              {teams.data?.find(t => t.id === teamId)?.projectLeadName ?? <span className="muted">—</span>}
            </div>
          </div>
        )}
        {filtered && (
          <button
            type="button"
            className="link-btn"
            onClick={() => {
              setSearchText('');
              setParams({}, { replace: true });
            }}
          >
            Clear filters
          </button>
        )}
      </div>

      {employees.loading && !employees.data ? (
        <LoadingState label="Loading employees" />
      ) : employees.error && !employees.data ? (
        <ErrorState error={employees.error} onRetry={employees.reload} />
      ) : (employees.data?.items.length ?? 0) === 0 ? (
        <EmptyState title={filtered ? 'No employees match these filters' : 'No employees yet'}>
          {filtered ? 'Try removing a filter or searching for something else.' : 'Employees you add will appear here.'}
        </EmptyState>
      ) : (
        <>
          <div className="table-wrap" aria-busy={employees.loading}>
            <table className="table">
              <thead>
                <tr>
                  <th scope="col">Employee</th>
                  <th scope="col">Code</th>
                  <th scope="col">Role</th>
                  <th scope="col">Organization</th>
                  <th scope="col">Team</th>
                  <th scope="col">Status</th>
                  <th scope="col">
                    <span className="visually-hidden">Actions</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {(employees.data?.items ?? []).map((e) => (
                  <tr key={e.id}>
                    <td>
                      <div className="cell-primary">{e.firstName} {e.lastName}</div>
                      <div className="cell-secondary">{e.jobTitle ? `${e.jobTitle} • ${e.email}` : e.email}</div>
                    </td>
                    <td className="cell-code">{e.employeeCode}</td>
                    <td>{e.roleName ?? <span className="muted">—</span>}</td>
                    <td>{e.organizationName}</td>
                    <td>{e.teamName ?? <span className="muted">No team</span>}</td>
                    <td>
                      <span className={e.isActive ? 'status status--active' : 'status status--inactive'}>{e.isActive ? 'Active' : 'Inactive'}</span>
                    </td>
                    <td className="cell-actions">
                      <Can permission={PERMISSIONS.Employee.Edit}>
                        <button type="button" className="link-btn" onClick={() => setEditing(e)} aria-label={`Edit ${e.firstName} ${e.lastName}`}>
                          Edit
                        </button>
                      </Can>
                      <Can permission={PERMISSIONS.Employee.Delete}>
                        <button type="button" className="link-btn link-btn--danger" onClick={() => setDeleting(e)} aria-label={`Delete ${e.firstName} ${e.lastName}`}>
                          Delete
                        </button>
                      </Can>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <nav className="pager" aria-label="Pagination">
            <span className="muted">
              Showing {first}–{last} of {total}
            </span>
            <div className="pager__buttons">
              <button type="button" className="btn btn--secondary" disabled={page <= 1} onClick={() => updateParams({ page: String(page - 1) })}>
                Previous
              </button>
              <button type="button" className="btn btn--secondary" disabled={last >= total} onClick={() => updateParams({ page: String(page + 1) })}>
                Next
              </button>
            </div>
          </nav>
        </>
      )}

      <EmployeeFormModal
        open={creating || editing !== null}
        employee={editing}
        organizations={organizations.data ?? []}
        defaultOrganizationId={organizationId}
        defaultTeamId={teamId}
        onClose={() => {
          setCreating(false);
          setEditing(null);
        }}
        onSaved={(saved) => {
          toast.success(editing ? `${saved.fullName} updated` : `${saved.fullName} created`);
          setCreating(false);
          setEditing(null);
          employees.reload();
        }}
      />

      <ConfirmDialog
        open={deleting !== null}
        title="Delete employee"
        message={
          <>
            Delete <strong>{deleting?.fullName}</strong>? They will no longer appear in employee lists.
          </>
        }
        confirmLabel="Delete employee"
        busy={deleteBusy}
        onConfirm={confirmDelete}
        onCancel={() => setDeleting(null)}
      />
    </>
  );
}
