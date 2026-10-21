import { useState, useEffect, type FormEvent } from 'react';
import type { Employee, Organization, Team } from '@/core/models/types';
import { employeeService, roleService, teamService } from '@/core/services/services';
import { SelectField, TextField } from '@/shared/components/Fields';
import { Modal } from '@/shared/components/Modal';
import { useAsync } from '@/shared/hooks/hooks';
import { errorMessage } from '@/shared/utils/format';
import { useAuth } from '@/core/auth/AuthContext';
import { PERMISSIONS } from '@/core/auth/permissions';

interface Props {
  open: boolean;
  /** Pass an employee to edit; omit to create. */
  employee?: Employee | null;
  organizations: Organization[];
  defaultOrganizationId?: number;
  defaultTeamId?: number;
  onClose: () => void;
  onSaved: (employee: Employee) => void;
}

export function EmployeeFormModal({ open, employee, organizations, defaultOrganizationId, defaultTeamId, onClose, onSaved }: Props) {
  return (
    <Modal open={open} title={employee ? 'Edit employee' : 'New employee'} onClose={onClose}>
      <EmployeeForm
        employee={employee ?? null}
        organizations={organizations}
        defaultOrganizationId={defaultOrganizationId}
        defaultTeamId={defaultTeamId}
        onClose={onClose}
        onSaved={onSaved}
      />
    </Modal>
  );
}

type Errors = Partial<Record<'employeeCode' | 'firstName' | 'lastName' | 'email' | 'organizationId', string>>;

function EmployeeForm({ employee, organizations, defaultOrganizationId, defaultTeamId, onClose, onSaved }: Omit<Props, 'open' | 'employee'> & { employee: Employee | null }) {
  const [code, setCode] = useState(employee?.employeeCode ?? '');
  const [firstName, setFirstName] = useState(employee?.firstName ?? '');
  const [lastName, setLastName] = useState(employee?.lastName ?? '');
  const [email, setEmail] = useState(employee?.email ?? '');
  const [jobTitle, setJobTitle] = useState(employee?.jobTitle ?? '');
  const [organizationId, setOrganizationId] = useState<number | ''>(employee?.organizationId ?? defaultOrganizationId ?? '');
  const [teamId, setTeamId] = useState<number | ''>(employee?.teamId ?? defaultTeamId ?? '');
  const [roleId, setRoleId] = useState<number | ''>(''); // Employees don't necessarily have their RoleId fetched directly on the employee yet, so we'll just allow setting it
  const [errors, setErrors] = useState<Errors>({});
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const { can } = useAuth();
  const isSuperAdmin = can(PERMISSIONS.Organization.Create); // Super admin can manage orgs
  const isManager = can(PERMISSIONS.Team.Create); // Managers can manage teams

  const [secondaryOrganizationIds, setSecondaryOrganizationIds] = useState<number[]>(employee?.secondaryOrganizationIds ?? []);
  const [secondaryTeamIds, setSecondaryTeamIds] = useState<number[]>(employee?.secondaryTeamIds ?? []);

  // Teams per organization cache for secondary organization team pickers
  const [secondaryOrgTeams, setSecondaryOrgTeams] = useState<Record<number, Team[]>>({});

  // When secondary organizations change, fetch teams for any new ones
  useEffect(() => {
    let active = true;
    for (const orgId of secondaryOrganizationIds) {
      if (!secondaryOrgTeams[orgId]) {
        teamService.listByOrganization(orgId).then((data) => {
          if (active) {
            setSecondaryOrgTeams((prev) => ({ ...prev, [orgId]: data }));
          }
        }).catch(() => {});
      }
    }
    return () => {
      active = false;
    };
  }, [secondaryOrganizationIds, secondaryOrgTeams]);

  // The team list is read from the API for the chosen primary organization.
  const teams = useAsync(
    (signal) => (organizationId === '' ? Promise.resolve([]) : teamService.listByOrganization(organizationId, signal)),
    [organizationId],
  );

  const roles = useAsync((signal) => roleService.listRoles(signal), []);

  function validate(): Errors {
    const next: Errors = {};
    if (!code.trim()) next.employeeCode = 'Enter an employee code.';
    else if (!/^[A-Za-z0-9_-]+$/.test(code.trim())) next.employeeCode = 'Use letters, digits, "-" and "_" only.';
    if (!firstName.trim()) next.firstName = 'Enter a first name.';
    if (!lastName.trim()) next.lastName = 'Enter a last name.';
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) next.email = 'Enter a valid email address.';
    if (organizationId === '') next.organizationId = 'Choose an organization.';
    return next;
  }

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    const found = validate();
    setErrors(found);
    if (Object.keys(found).length > 0 || organizationId === '') return;

    setBusy(true);
    setError(null);
    try {
      const body = {
        employeeCode: code.trim(),
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        email: email.trim(),
        jobTitle: jobTitle.trim(),
        organizationId,
        teamId: teamId === '' ? null : teamId,
        roleId: roleId === '' ? null : roleId,
        secondaryOrganizationIds,
        secondaryTeamIds,
      };
      const saved = employee ? await employeeService.update(employee.id, body) : await employeeService.create(body);
      onSaved(saved);
    } catch (e) {
      setError(errorMessage(e));
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} noValidate className="form">
      {error && (
        <p className="form__alert" role="alert">
          {error}
        </p>
      )}
      <TextField label="Employee code" value={code} onChange={(e) => setCode(e.target.value)} error={errors.employeeCode} maxLength={30} autoFocus required />
      <div className="form__row">
        <TextField label="First name" value={firstName} onChange={(e) => setFirstName(e.target.value)} error={errors.firstName} maxLength={100} required />
        <TextField label="Last name" value={lastName} onChange={(e) => setLastName(e.target.value)} error={errors.lastName} maxLength={100} required />
      </div>
      <div className="form__row">
        <TextField label="Email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} error={errors.email} maxLength={256} required />
        <TextField label="Job Title" hint="Optional" value={jobTitle} onChange={(e) => setJobTitle(e.target.value)} maxLength={100} />
      </div>
      <div className="form__row">
        <SelectField label="Role" value={roleId} onChange={(e) => setRoleId(e.target.value === '' ? '' : Number(e.target.value))} hint={employee ? "Roles cannot currently be edited from here" : "Optional login access"}>
          <option value="">No login access</option>
          {(roles.data ?? []).map((r) => (
            <option key={r.id} value={r.id}>
              {r.name}
            </option>
          ))}
        </SelectField>
      </div>
      <div className="form__row">
        <SelectField
          label="Organization"
          value={organizationId}
          error={errors.organizationId}
          onChange={(e) => {
            const nextOrgId = e.target.value === '' ? '' : Number(e.target.value);
            setOrganizationId(nextOrgId);
            setTeamId('');
            if (nextOrgId !== '') {
              setSecondaryOrganizationIds((prev) => prev.filter((id) => id !== nextOrgId));
            }
          }}
          required
        >
          <option value="">Choose organization</option>
          {organizations.map((o) => (
            <option key={o.id} value={o.id}>
              {o.name}
            </option>
          ))}
        </SelectField>
        <SelectField
          label="Team"
          hint={organizationId === '' ? 'Choose an organization first.' : 'Optional.'}
          value={teamId}
          disabled={organizationId === ''}
          onChange={(e) => setTeamId(e.target.value === '' ? '' : Number(e.target.value))}
        >
          <option value="">No team</option>
          {(teams.data ?? []).map((t) => (
            <option key={t.id} value={t.id}>
              {t.name}
            </option>
          ))}
        </SelectField>
      </div>

      {(isSuperAdmin || isManager) && (
        <>
          <div className="form__row">
            {isSuperAdmin && (
              <SelectField
                label="Secondary Organizations (Optional)"
                multiple
                size={Math.min(4, Math.max(2, organizations.filter(o => o.id !== organizationId).length))}
                value={secondaryOrganizationIds.map(String)}
                hint="Hold Ctrl/Cmd to select multiple organizations"
                onChange={(e) => {
                  const values = Array.from(e.target.selectedOptions, option => Number(option.value));
                  setSecondaryOrganizationIds(values);
                  // Clean up any selected secondary teams from deselected organizations
                  const allowedSecondaryTeams = new Set<number>();
                  values.forEach((v) => {
                    const orgTeams = secondaryOrgTeams[v] ?? [];
                    orgTeams.forEach((t) => allowedSecondaryTeams.add(t.id));
                  });
                  // If teams for an org aren't loaded yet, keep them to avoid accidental loss
                  setSecondaryTeamIds((prev) => prev.filter((tid) => {
                    // find which org tid belongs to
                    for (const [oIdStr, tList] of Object.entries(secondaryOrgTeams)) {
                      if (tList.some((t) => t.id === tid)) {
                        return values.includes(Number(oIdStr));
                      }
                    }
                    return true;
                  }));
                }}
              >
                {organizations.filter(o => o.id !== organizationId).map((o) => (
                  <option key={o.id} value={o.id}>
                    {o.name}
                  </option>
                ))}
              </SelectField>
            )}

            {/* If primary org has other teams, also allow choosing secondary teams within primary org */}
            {((teams.data ?? []).filter(t => t.id !== teamId).length > 0) && (
              <SelectField
                label="Additional Teams in Primary Org (Optional)"
                multiple
                size={Math.min(4, Math.max(2, (teams.data ?? []).filter(t => t.id !== teamId).length))}
                value={secondaryTeamIds.map(String)}
                hint="Hold Ctrl/Cmd to select multiple"
                onChange={(e) => {
                  const selectedInPrimary = Array.from(e.target.selectedOptions, option => Number(option.value));
                  const primaryTeamIdsSet = new Set((teams.data ?? []).map(t => t.id));
                  setSecondaryTeamIds((prev) => [
                    ...prev.filter(id => !primaryTeamIdsSet.has(id)),
                    ...selectedInPrimary,
                  ]);
                }}
              >
                {(teams.data ?? []).filter(t => t.id !== teamId).map((t) => (
                  <option key={t.id} value={t.id}>
                    {t.name}
                  </option>
                ))}
              </SelectField>
            )}
          </div>

          {/* Teams for each selected Secondary Organization */}
          {secondaryOrganizationIds.map((secOrgId) => {
            const orgObj = organizations.find((o) => o.id === secOrgId);
            const orgTeamsList = secondaryOrgTeams[secOrgId] ?? [];
            const orgName = orgObj ? orgObj.name : `Org #${secOrgId}`;

            return (
              <div key={secOrgId} className="form__row" style={{ marginTop: '4px' }}>
                <SelectField
                  label={`Teams in ${orgName} (Optional)`}
                  multiple
                  size={Math.min(4, Math.max(2, orgTeamsList.length || 1))}
                  value={secondaryTeamIds.map(String)}
                  hint={`Select team(s) in ${orgName} (e.g. TME, S1). Hold Ctrl/Cmd for multiple.`}
                  onChange={(e) => {
                    const selectedThisOrg = Array.from(e.target.selectedOptions, option => Number(option.value));
                    const thisOrgTeamIdsSet = new Set(orgTeamsList.map(t => t.id));
                    setSecondaryTeamIds((prev) => [
                      ...prev.filter(id => !thisOrgTeamIdsSet.has(id)),
                      ...selectedThisOrg,
                    ]);
                  }}
                >
                  {orgTeamsList.length === 0 ? (
                    <option value="" disabled>No teams found in {orgName}</option>
                  ) : (
                    orgTeamsList.map((t) => (
                      <option key={t.id} value={t.id}>
                        {t.name}
                      </option>
                    ))
                  )}
                </SelectField>
              </div>
            );
          })}
        </>
      )}

      <div className="form__actions">
        <button type="button" className="btn btn--secondary" onClick={onClose} disabled={busy}>
          Cancel
        </button>
        <button type="submit" className="btn btn--primary" disabled={busy}>
          {busy ? 'Saving…' : employee ? 'Save changes' : 'Create employee'}
        </button>
      </div>
    </form>
  );
}
