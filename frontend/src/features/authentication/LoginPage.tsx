import { useState, type FormEvent } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '@/core/auth/AuthContext';
import { TextField } from '@/shared/components/Fields';
import { errorMessage } from '@/shared/utils/format';

export function LoginPage() {
  const { status, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: string } | null)?.from ?? '/';

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (status === 'authenticated') return <Navigate to={from} replace />;

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      await login(email.trim(), password);
      navigate(from, { replace: true });
    } catch (e) {
      setError(errorMessage(e));
      setBusy(false);
    }
  }

  return (
    <div className="login">
      <section className="login__intro" aria-hidden="true">
        <p className="login__product">WorkforceConnect</p>
        <p className="login__tagline">Every organization, team and person in one place.</p>
      </section>

      <section className="login__panel">
        <h1 className="login__title">Sign in</h1>
        <p className="login__subtitle">Use your WorkforceConnect work account.</p>

        <form onSubmit={onSubmit} noValidate className="form">
          {error && (
            <p className="form__alert" role="alert">
              {error}
            </p>
          )}
          <TextField
            label="Email"
            type="email"
            name="email"
            autoComplete="username"
            required
            autoFocus
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />
          <TextField
            label="Password"
            type="password"
            name="password"
            autoComplete="current-password"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
          <button type="submit" className="btn btn--primary btn--block" disabled={busy || !email || !password}>
            {busy ? 'Signing in…' : 'Sign in'}
          </button>
        </form>
      </section>
    </div>
  );
}
