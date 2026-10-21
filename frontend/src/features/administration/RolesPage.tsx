import { useMemo, useState } from 'react';
import type { Permission, Role } from '@/core/models/types';
import { roleService } from '@/core/services/services';
import { PageHeader } from '@/shared/components/PageHeader';
import { ErrorState, LoadingState } from '@/shared/components/States';
import { useAsync } from '@/shared/hooks/hooks';

const SCOPE_TEXT: Record<string, string> = {
  All: 'All organizations, teams and employees',
  Organization: 'Assigned organizations',
  Team: 'Assigned teams',
  Self: 'Own information only',
};

export function RolesPage() {
  const roles = useAsync((signal) => roleService.listRoles(signal), []);
  const permissions = useAsync((signal) => roleService.listPermissions(signal), []);
  const [selectedId, setSelectedId] = useState<number | null>(null);

  const selected = roles.data?.find((r) => r.id === selectedId) ?? roles.data?.[0] ?? null;

  if ((roles.loading && !roles.data) || (permissions.loading && !permissions.data)) return <LoadingState label="Loading roles" />;
  const failure = roles.error ?? permissions.error;
  if (failure && (!roles.data || !permissions.data)) {
    return <ErrorState error={failure} onRetry={() => { roles.reload(); permissions.reload(); }} />;
  }

  return (
    <>
      <PageHeader
        title="Roles and permissions"
        subtitle="What each role can do, and which data it can reach."
        crumbs={[{ label: 'Application' }, { label: 'Roles and permissions' }]}
      />
      <div className="roles">
        <ul className="role-list" aria-label="Roles">
          {(roles.data ?? []).map((role) => (
            <li key={role.id}>
              <button
                type="button"
                className={role.id === selected?.id ? 'role-item role-item--selected' : 'role-item'}
                aria-pressed={role.id === selected?.id}
                onClick={() => setSelectedId(role.id)}
              >
                <span className="role-item__name">{role.name}</span>
                <span className="role-item__meta">
                  {role.userCount} {role.userCount === 1 ? 'user' : 'users'}
                </span>
              </button>
            </li>
          ))}
        </ul>

        {selected && <RoleDetail role={selected} permissions={permissions.data ?? []} />}
      </div>
    </>
  );
}

function RoleDetail({ role, permissions }: { role: Role; permissions: Permission[] }) {
  // The matrix is built from whatever the API returns, so permissions added later show up automatically.
  const { modules, actions } = useMemo(() => {
    const byModule = new Map<string, Map<string, string>>();
    const actionSet: string[] = [];
    for (const p of permissions) {
      const action = p.name.split('.').slice(1).join('.') || p.name;
      if (!actionSet.includes(action)) actionSet.push(action);
      if (!byModule.has(p.module)) byModule.set(p.module, new Map());
      byModule.get(p.module)?.set(action, p.name);
    }
    return { modules: [...byModule.entries()], actions: actionSet };
  }, [permissions]);

  const held = new Set(role.permissions);

  return (
    <section className="panel role-detail" aria-labelledby="role-title">
      <h2 id="role-title" className="panel__title">
        {role.name}
      </h2>
      {role.description && <p className="muted">{role.description}</p>}
      <dl className="details">
        <div className="details__row">
          <dt>Data access</dt>
          <dd>{SCOPE_TEXT[role.dataScope] ?? role.dataScope}</dd>
        </div>
        <div className="details__row">
          <dt>Role type</dt>
          <dd>{role.isSystem ? 'Built in' : 'Custom'}</dd>
        </div>
      </dl>

      <div className="table-wrap">
        <table className="table table--matrix">
          <caption className="visually-hidden">Permissions held by {role.name}</caption>
          <thead>
            <tr>
              <th scope="col">Area</th>
              {actions.map((a) => (
                <th key={a} scope="col">
                  {a}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {modules.map(([module, entries]) => (
              <tr key={module}>
                <th scope="row">{module}</th>
                {actions.map((a) => {
                  const name = entries.get(a);
                  if (!name) return <td key={a} className="matrix__na" aria-label="Not applicable">–</td>;
                  const has = held.has(name);
                  return (
                    <td key={a} className={has ? 'matrix__yes' : 'matrix__no'}>
                      <span aria-label={has ? `${name}: granted` : `${name}: not granted`}>{has ? 'Yes' : 'No'}</span>
                    </td>
                  );
                })}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
