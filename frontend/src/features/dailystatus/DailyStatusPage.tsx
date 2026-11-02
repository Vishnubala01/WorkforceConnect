import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useAuth } from '@/core/auth/AuthContext';
import { PERMISSIONS } from '@/core/auth/permissions';
import type { DailyStatus, SaveDailyStatus } from '@/core/models/types';
import { dailyStatusService, organizationService, teamService, employeeService } from '@/core/services/services';
import { SelectField } from '@/shared/components/Fields';
import { ConfirmDialog } from '@/shared/components/Modal';
import { PageHeader } from '@/shared/components/PageHeader';
import { EmptyState, ErrorState, LoadingState } from '@/shared/components/States';
import { useToast } from '@/shared/components/Toast';
import { useAsync, useDebounced } from '@/shared/hooks/hooks';
import { errorMessage } from '@/shared/utils/format';
import { DailyStatusFormModal } from './DailyStatusFormModal';

const PAGE_SIZE = 50;

const STATUS_BADGE_CLASS: Record<string, string> = {
  'Completed': 'badge--success',
  'In Progress': 'badge--info',
  'Under Review': 'badge--warning',
  'Blocked': 'badge--danger',
  'On Hold': 'badge--neutral',
};

export function DailyStatusPage() {
  const { user, can } = useAuth();
  const toast = useToast();
  const [params, setParams] = useSearchParams();

  const organizationId = Number(params.get('organizationId')) || undefined;
  const teamId = Number(params.get('teamId')) || undefined;
  const employeeId = Number(params.get('employeeId')) || undefined;
  const statusFilter = params.get('status') || undefined;
  const fromDate = params.get('fromDate') || '';
  const toDate = params.get('toDate') || '';
  const page = Math.max(1, Number(params.get('page')) || 1);
  const [searchText, setSearchText] = useState(params.get('search') ?? '');
  const search = useDebounced(searchText.trim());

  const canViewOrganizations = can(PERMISSIONS.Organization.View);
  const organizations = useAsync(
    (signal) => (canViewOrganizations ? organizationService.list(signal) : Promise.resolve([])),
    [canViewOrganizations]
  );

  // Auto-select user's organization if none is chosen in URL
  useEffect(() => {
    if (!organizationId && user?.organization?.id) {
      updateParams({ organizationId: String(user.organization.id) });
    } else if (!organizationId && organizations.data && organizations.data.length === 1 && organizations.data[0]) {
      updateParams({ organizationId: String(organizations.data[0].id) });
    }
  }, [organizationId, user?.organization?.id, organizations.data]);

  const canViewTeams = can(PERMISSIONS.Team.View);
  const effectiveOrgId = organizationId ?? user?.organization?.id;
  const teams = useAsync(
    (signal) => (effectiveOrgId && canViewTeams ? teamService.listByOrganization(effectiveOrgId, signal) : Promise.resolve([])),
    [effectiveOrgId, canViewTeams]
  );

  const canViewEmployees = can(PERMISSIONS.Employee.View);
  const employeeList = useAsync(
    (signal) => (canViewEmployees ? employeeService.list({ organizationId: effectiveOrgId, teamId, pageSize: 100 }, signal) : Promise.resolve({ items: [], total: 0, page: 1, pageSize: 100 })),
    [effectiveOrgId, teamId, canViewEmployees]
  );

  const dailyStatuses = useAsync(
    (signal) =>
      dailyStatusService.list(
        {
          organizationId,
          teamId,
          employeeId,
          status: statusFilter,
          fromDate: fromDate || undefined,
          toDate: toDate || undefined,
          search: search || undefined,
          page,
          pageSize: PAGE_SIZE,
        },
        signal
      ),
    [organizationId, teamId, employeeId, statusFilter, fromDate, toDate, search, page]
  );

  const [activeTab, setActiveTab] = useState<'all' | 'pending'>('all');
  const [modalOpen, setModalOpen] = useState(false);
  const [editingItem, setEditingItem] = useState<DailyStatus | null>(null);
  const [prefilledEmployee, setPrefilledEmployee] = useState<{ id: number; name: string } | null>(null);
  const [deletingItem, setDeletingItem] = useState<DailyStatus | null>(null);
  const [deleteBusy, setDeleteBusy] = useState(false);
  const [exportBusy, setExportBusy] = useState<'excel' | 'csv' | null>(null);

  // Compute pending status: employees in the team/organization who have NOT logged status for the target date
  const targetDateStr = fromDate || new Date().toISOString().split('T')[0];
  const submittedEmployeeIds = new Set(
    (dailyStatuses.data?.items ?? [])
      .filter((s) => s.date.split('T')[0] === targetDateStr)
      .map((s) => s.employeeId)
  );

  const pendingEmployees = (employeeList.data?.items ?? []).filter(
    (emp) => !submittedEmployeeIds.has(emp.id)
  );

  function updateParams(changes: Record<string, string | undefined>) {
    const next = new URLSearchParams(params);
    for (const [key, value] of Object.entries(changes)) {
      if (value) next.set(key, value);
      else next.delete(key);
    }
    setParams(next, { replace: true });
  }

  const urlSearch = params.get('search') ?? '';
  useEffect(() => {
    if (search !== urlSearch) updateParams({ search: search || undefined, page: undefined });
  }, [search]);

  async function handleSave(data: SaveDailyStatus, id?: number) {
    if (id) {
      await dailyStatusService.update(id, data);
      toast.success('Daily status entry updated');
    } else {
      await dailyStatusService.create(data);
      toast.success('Daily status logged successfully');
    }
    dailyStatuses.reload();
  }

  async function handleDelete() {
    if (!deletingItem) return;
    setDeleteBusy(true);
    try {
      await dailyStatusService.remove(deletingItem.id);
      toast.success('Daily status entry deleted');
      setDeletingItem(null);
      dailyStatuses.reload();
    } catch (err) {
      toast.error(errorMessage(err));
    } finally {
      setDeleteBusy(false);
    }
  }

  async function handleExportExcel() {
    setExportBusy('excel');
    try {
      await dailyStatusService.exportExcel({
        organizationId,
        teamId,
        employeeId,
        status: statusFilter,
        fromDate: fromDate || undefined,
        toDate: toDate || undefined,
        search: search || undefined,
      });
      toast.success('Excel download started');
    } catch (err) {
      toast.error(errorMessage(err));
    } finally {
      setExportBusy(null);
    }
  }

  async function handleExportCsv() {
    setExportBusy('csv');
    try {
      await dailyStatusService.exportCsv({
        organizationId,
        teamId,
        employeeId,
        status: statusFilter,
        fromDate: fromDate || undefined,
        toDate: toDate || undefined,
        search: search || undefined,
      });
      toast.success('CSV download started');
    } catch (err) {
      toast.error(errorMessage(err));
    } finally {
      setExportBusy(null);
    }
  }

  const filtered = Boolean(
    organizationId || teamId || employeeId || statusFilter || fromDate || toDate || searchText.trim()
  );

  const canExport = can(PERMISSIONS.DailyStatus.Export);
  const canCreate = can(PERMISSIONS.DailyStatus.Create);
  const canEdit = can(PERMISSIONS.DailyStatus.Edit);
  const canDelete = can(PERMISSIONS.DailyStatus.Delete);

  return (
    <div className="page page--wide">
      <PageHeader
        title="Daily Status Tracker"
        subtitle="Track daily activities, planned work, progress, hours, and blockers across teams."
        actions={
          <div className="header-actions" style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
            {canExport && (
              <>
                <button
                  type="button"
                  className="btn btn--secondary"
                  disabled={exportBusy !== null}
                  onClick={handleExportExcel}
                  title="Export to Excel Spreadsheet (.xls)"
                >
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{ marginRight: '6px' }}>
                    <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
                    <polyline points="7 10 12 15 17 10" />
                    <line x1="12" y1="15" x2="12" y2="3" />
                  </svg>
                  {exportBusy === 'excel' ? 'Exporting...' : 'Export Excel'}
                </button>
                <button
                  type="button"
                  className="btn btn--secondary"
                  disabled={exportBusy !== null}
                  onClick={handleExportCsv}
                  title="Export to CSV (.csv)"
                >
                  {exportBusy === 'csv' ? 'Exporting...' : 'Export CSV'}
                </button>
              </>
            )}

            {canCreate && (
              <button
                type="button"
                className="btn btn--primary"
                onClick={() => {
                  setEditingItem(null);
                  setModalOpen(true);
                }}
              >
                + Log Daily Status
              </button>
            )}
          </div>
        }
      />

      {/* Filter Toolbar */}
      <div className="toolbar" role="search" style={{ flexWrap: 'wrap', gap: '12px' }}>
        <input
          className="input toolbar__search"
          type="search"
          placeholder="Search task, activity, ID, member..."
          aria-label="Search status entries"
          value={searchText}
          onChange={(e) => setSearchText(e.target.value)}
          style={{ minWidth: '220px' }}
        />

        <div className="toolbar__select" style={{ minWidth: '130px' }}>
          <label className="field__label" style={{ fontSize: '12px', marginBottom: '4px' }}>Date From</label>
          <input
            type="date"
            className="input"
            value={fromDate}
            onChange={(e) => updateParams({ fromDate: e.target.value || undefined, page: undefined })}
          />
        </div>

        <div className="toolbar__select" style={{ minWidth: '130px' }}>
          <label className="field__label" style={{ fontSize: '12px', marginBottom: '4px' }}>Date To</label>
          <input
            type="date"
            className="input"
            value={toDate}
            onChange={(e) => updateParams({ toDate: e.target.value || undefined, page: undefined })}
          />
        </div>

        <div className="toolbar__select" style={{ minWidth: '140px' }}>
          <SelectField
            label="Status"
            value={statusFilter ?? ''}
            onChange={(e) => updateParams({ status: e.target.value || undefined, page: undefined })}
          >
            <option value="">All statuses</option>
            <option value="In Progress">In Progress</option>
            <option value="Completed">Completed</option>
            <option value="Under Review">Under Review</option>
            <option value="Blocked">Blocked</option>
            <option value="On Hold">On Hold</option>
          </SelectField>
        </div>

        {canViewOrganizations && (
          <div className="toolbar__select">
            <SelectField
              label="Organization"
              value={organizationId ?? effectiveOrgId ?? ''}
              onChange={(e) => updateParams({ organizationId: e.target.value || undefined, teamId: undefined, employeeId: undefined, page: undefined })}
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
              disabled={!effectiveOrgId && teams.data?.length === 0}
              onChange={(e) => updateParams({ teamId: e.target.value || undefined, employeeId: undefined, page: undefined })}
            >
              <option value="">{effectiveOrgId ? 'All teams' : 'Choose organization'}</option>
              {(teams.data ?? []).map((t) => (
                <option key={t.id} value={t.id}>
                  {t.name}
                </option>
              ))}
            </SelectField>
          </div>
        )}

        {canViewEmployees && (employeeList.data?.items.length ?? 0) > 0 && (
          <div className="toolbar__select">
            <SelectField
              label="Team Member"
              value={employeeId ?? ''}
              onChange={(e) => updateParams({ employeeId: e.target.value || undefined, page: undefined })}
            >
              <option value="">All members</option>
              {employeeList.data?.items.map((emp) => (
                <option key={emp.id} value={emp.id}>
                  {emp.fullName}
                </option>
              ))}
            </SelectField>
          </div>
        )}

        {filtered && (
          <button
            type="button"
            className="link-btn"
            style={{ alignSelf: 'center', marginTop: '16px' }}
            onClick={() => {
              setSearchText('');
              setParams({}, { replace: true });
            }}
          >
            Clear filters
          </button>
        )}
      </div>

      {/* Navigation Tabs */}
      <div className="status-tabs" role="tablist">
        <button
          type="button"
          role="tab"
          aria-selected={activeTab === 'all'}
          className={`status-tab-btn ${activeTab === 'all' ? 'active' : ''}`}
          onClick={() => setActiveTab('all')}
        >
          All Status Entries ({dailyStatuses.data?.total ?? 0})
        </button>
        <button
          type="button"
          role="tab"
          aria-selected={activeTab === 'pending'}
          className={`status-tab-btn ${activeTab === 'pending' ? 'active' : ''}`}
          onClick={() => setActiveTab('pending')}
        >
          Pending Submission ({pendingEmployees.length})
          {pendingEmployees.length > 0 && (
            <span className="status-tab-badge">
              {pendingEmployees.length} missing
            </span>
          )}
        </button>
      </div>

      {activeTab === 'pending' ? (
        <div className="pending-section">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '14px', flexWrap: 'wrap', gap: '8px' }}>
            <p style={{ margin: 0, fontSize: '13.5px', color: '#475569' }}>
              Showing team members who have <strong>not logged a daily status</strong> for{' '}
              <strong style={{ color: '#0f172a' }}>{targetDateStr}</strong>:
            </p>
          </div>

          {pendingEmployees.length === 0 ? (
            <div className="card" style={{ padding: '36px', textAlign: 'center', background: '#f0fdf4', borderColor: '#bbf7d0' }}>
              <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="#16a34a" strokeWidth="2" style={{ margin: '0 auto 8px' }}>
                <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14" />
                <polyline points="22 4 12 14.01 9 11.01" />
              </svg>
              <h3 style={{ margin: '0 0 4px', fontSize: '16px', color: '#166534', fontWeight: 600 }}>All Caught Up!</h3>
              <p style={{ margin: 0, fontSize: '13px', color: '#15803d' }}>
                Every member in this scope has submitted their daily status for {targetDateStr}.
              </p>
            </div>
          ) : (
            <div className="table-wrap tracker-table-wrap">
              <table className="table tracker-table">
                <thead className="tracker-thead">
                  <tr>
                    <th scope="col" style={{ minWidth: '160px' }}>Team Member</th>
                    <th scope="col" style={{ minWidth: '120px' }}>Employee Code</th>
                    <th scope="col" style={{ minWidth: '160px' }}>Email</th>
                    <th scope="col" style={{ minWidth: '140px' }}>Organization</th>
                    <th scope="col" style={{ minWidth: '130px' }}>Team</th>
                    <th scope="col" style={{ width: '120px', textAlign: 'center' }}>Daily Status</th>
                    {canCreate && (
                      <th scope="col" style={{ width: '130px', textAlign: 'center' }}>Action</th>
                    )}
                  </tr>
                </thead>
                <tbody>
                  {pendingEmployees.map((emp) => (
                    <tr key={emp.id} className="tracker-row">
                      <td style={{ fontWeight: 600, color: '#0f172a' }}>
                        {emp.fullName}
                      </td>
                      <td style={{ color: '#64748b', fontSize: '13px' }}>
                        {emp.employeeCode}
                      </td>
                      <td style={{ color: '#475569', fontSize: '13px' }}>
                        {emp.email}
                      </td>
                      <td style={{ fontSize: '13px' }}>
                        {emp.organizationName}
                      </td>
                      <td style={{ fontSize: '13px' }}>
                        {emp.teamName || '—'}
                      </td>
                      <td style={{ textAlign: 'center' }}>
                        <span className="badge badge--danger" style={{ fontWeight: 600 }}>
                          Pending
                        </span>
                      </td>
                      {canCreate && (
                        <td style={{ textAlign: 'center' }}>
                          <button
                            type="button"
                            className="btn btn--secondary btn--sm"
                            onClick={() => {
                              setPrefilledEmployee({ id: emp.id, name: emp.fullName });
                              setEditingItem(null);
                              setModalOpen(true);
                            }}
                          >
                            + Log for Member
                          </button>
                        </td>
                      )}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      ) : dailyStatuses.loading && !dailyStatuses.data ? (
        <LoadingState label="Loading daily status logs" />
      ) : dailyStatuses.error && !dailyStatuses.data ? (
        <ErrorState error={dailyStatuses.error} onRetry={dailyStatuses.reload} />
      ) : (dailyStatuses.data?.items.length ?? 0) === 0 ? (
        <EmptyState title={filtered ? 'No status entries match these filters' : 'No daily status logged yet'}>
          {filtered
            ? 'Try broadening your dates or clearing filters.'
            : 'Click "+ Log Daily Status" above to record today\'s work and progress.'}
        </EmptyState>
      ) : (
        <>
          {/* Tracker Table replicating template design */}
          <div className="table-wrap tracker-table-wrap" aria-busy={dailyStatuses.loading}>
            <table className="table tracker-table">
              <thead className="tracker-thead">
                <tr>
                  <th scope="col" style={{ width: '105px' }}>Date</th>
                  <th scope="col" style={{ minWidth: '140px' }}>Team Member</th>
                  <th scope="col" style={{ minWidth: '120px' }}>Project / Team</th>
                  <th scope="col" style={{ minWidth: '100px' }}>Work Item ID</th>
                  <th scope="col" style={{ minWidth: '160px' }}>Task / Activity</th>
                  <th scope="col" style={{ minWidth: '220px' }}>Planned Work for Today</th>
                  <th scope="col" style={{ minWidth: '220px' }}>Work Completed / Progress</th>
                  <th scope="col" style={{ width: '110px' }}>Status</th>
                  <th scope="col" style={{ width: '90px', textAlign: 'center' }}>Hours Spent</th>
                  <th scope="col" style={{ minWidth: '150px' }}>Blocker / Dependency</th>
                  <th scope="col" style={{ minWidth: '140px' }}>Support Required</th>
                  <th scope="col" style={{ minWidth: '130px' }}>Reviewer / Lead</th>
                  <th scope="col" style={{ minWidth: '130px' }}>Remarks</th>
                  {(canEdit || canDelete) && (
                    <th scope="col" style={{ width: '80px', textAlign: 'center' }}>
                      <span className="visually-hidden">Actions</span>
                    </th>
                  )}
                </tr>
              </thead>
              <tbody>
                {dailyStatuses.data?.items.map((row) => {
                  const dateFormatted = new Date(row.date).toLocaleDateString('en-GB', {
                    day: '2-digit',
                    month: 'short',
                    year: 'numeric',
                  });

                  const badgeClass = STATUS_BADGE_CLASS[row.status] || 'badge--neutral';

                  return (
                    <tr key={row.id} className="tracker-row">
                      <td className="cell-date" style={{ fontWeight: 500, whiteSpace: 'nowrap' }}>
                        {dateFormatted}
                      </td>
                      <td>
                        <div className="cell-primary" style={{ fontWeight: 600 }}>{row.employeeName}</div>
                        <div className="cell-secondary" style={{ fontSize: '11px', color: 'var(--muted)' }}>
                          {row.employeeCode}
                        </div>
                      </td>
                      <td>
                        <span className="tag">{row.projectOrTeamName || row.teamName || '—'}</span>
                      </td>
                      <td className="cell-code" style={{ fontFamily: 'monospace', fontWeight: 600 }}>
                        {row.workItemId || <span className="muted">—</span>}
                      </td>
                      <td style={{ fontWeight: 500 }}>{row.taskActivity}</td>
                      <td className="cell-wrap-text">{row.plannedWorkForToday}</td>
                      <td className="cell-wrap-text">{row.workCompletedProgress || <span className="muted">—</span>}</td>
                      <td>
                        <span className={`badge ${badgeClass}`}>{row.status}</span>
                      </td>
                      <td style={{ textAlign: 'center', fontWeight: 600 }}>
                        {row.hoursSpent > 0 ? `${row.hoursSpent}h` : '—'}
                      </td>
                      <td className="cell-wrap-text">
                        {row.blockerDependency ? (
                          <span className="blocker-text">{row.blockerDependency}</span>
                        ) : (
                          <span className="muted">—</span>
                        )}
                      </td>
                      <td className="cell-wrap-text">
                        {row.supportRequired || <span className="muted">—</span>}
                      </td>
                      <td>
                        {row.reviewerLead || <span className="muted">—</span>}
                      </td>
                      <td className="cell-wrap-text">
                        {row.remarks || <span className="muted">—</span>}
                      </td>
                      {(canEdit || canDelete) && (
                        <td style={{ textAlign: 'center', whiteSpace: 'nowrap' }}>
                          {canEdit && (
                            <button
                              type="button"
                              className="btn btn--ghost btn--sm"
                              onClick={() => {
                                setEditingItem(row);
                                setModalOpen(true);
                              }}
                              title="Edit entry"
                            >
                              Edit
                            </button>
                          )}
                          {canDelete && (
                            <button
                              type="button"
                              className="btn btn--ghost btn--sm btn--danger"
                              onClick={() => setDeletingItem(row)}
                              title="Delete entry"
                            >
                              ✕
                            </button>
                          )}
                        </td>
                      )}
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          <div className="pagination" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '16px' }}>
            <span className="muted" style={{ fontSize: '13px' }}>
              Showing {dailyStatuses.data?.items.length ?? 0} of {dailyStatuses.data?.total ?? 0} entries
            </span>
            <div style={{ display: 'flex', gap: '8px' }}>
              <button
                type="button"
                className="btn btn--secondary btn--sm"
                disabled={page <= 1}
                onClick={() => updateParams({ page: String(page - 1) })}
              >
                Previous
              </button>
              <button
                type="button"
                className="btn btn--secondary btn--sm"
                disabled={(page * PAGE_SIZE) >= (dailyStatuses.data?.total ?? 0)}
                onClick={() => updateParams({ page: String(page + 1) })}
              >
                Next
              </button>
            </div>
          </div>
        </>
      )}

      {/* Create / Edit Modal */}
      {modalOpen && (
        <DailyStatusFormModal
          open={modalOpen}
          statusItem={editingItem}
          prefilledEmployee={prefilledEmployee}
          defaultProject={user?.team?.name}
          onClose={() => {
            setModalOpen(false);
            setEditingItem(null);
            setPrefilledEmployee(null);
          }}
          onSave={handleSave}
        />
      )}

      {/* Delete Confirmation */}
      {deletingItem && (
        <ConfirmDialog
          open={Boolean(deletingItem)}
          title="Delete status entry"
          message={
            <p>
              Are you sure you want to delete this status entry for{' '}
              <strong>{deletingItem.taskActivity}</strong>?
            </p>
          }
          confirmLabel={deleteBusy ? 'Deleting...' : 'Delete'}
          busy={deleteBusy}
          onConfirm={handleDelete}
          onCancel={() => setDeletingItem(null)}
        />
      )}
    </div>
  );
}
