import { Link } from 'react-router-dom';

export function AccessDenied() {
  return (
    <div className="state state--page state--empty" role="alert">
      <h2 className="state__title">You don't have access to this page</h2>
      <p className="state__text">Your role doesn't include this area. If you need it, ask a Super Admin to update your role.</p>
      <Link className="btn btn--secondary" to="/account">
        Go to my account
      </Link>
    </div>
  );
}
