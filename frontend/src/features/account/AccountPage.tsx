import { useAuth } from '@/core/auth/AuthContext';
import { PageHeader } from '@/shared/components/PageHeader';

const SCOPE_TEXT: Record<string, string> = {
  All: 'All organizations, teams and employees',
  Organization: 'Your assigned organizations',
  Team: 'Your assigned teams',
  Self: 'Your own information only',
};

export function AccountPage() {
  const { user } = useAuth();
  if (!user) return null;

  const rows: [string, string][] = [
    ['Name', user.name],
    ['Email', user.email],
    ['Role', user.role],
    ['Data access', SCOPE_TEXT[user.dataScope.level] ?? user.dataScope.level],
    ['Organization', user.organization?.name ?? 'Not assigned'],
    ['Team', user.team?.name ?? 'Not assigned'],
  ];

  return (
    <>
      <PageHeader title="My account" subtitle="Your sign-in details and what your role lets you do." />
      <div className="panel">
        <dl className="details">
          {rows.map(([label, value]) => (
            <div key={label} className="details__row">
              <dt>{label}</dt>
              <dd>{value}</dd>
            </div>
          ))}
        </dl>
      </div>
    </>
  );
}
