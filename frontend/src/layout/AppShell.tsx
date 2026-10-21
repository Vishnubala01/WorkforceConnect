import { useState } from 'react';
import { NavLink, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '@/core/auth/AuthContext';
import { initials } from '@/shared/utils/format';
import { NAV_ITEMS } from './navigation';

export function AppShell() {
  const { user, can, logout } = useAuth();
  const [navOpen, setNavOpen] = useState(false);
  const location = useLocation();

  // The menu is generated from the user's permissions, so each role sees only the areas it can use.
  const visible = NAV_ITEMS.filter((item) => !item.permission || can(item.permission));
  const groups = [...new Set(visible.map((item) => item.group))];

  return (
    <div className="shell">
      <a href="#main" className="skip-link">
        Skip to content
      </a>

      <aside className={navOpen ? 'rail rail--open' : 'rail'}>
        <div className="rail__top">
          <div className="brand">
            <span className="brand__mark" aria-hidden="true">
              W
            </span>
            <span className="brand__name">WorkforceConnect</span>
          </div>
          <button
            type="button"
            className="rail__toggle"
            aria-expanded={navOpen}
            aria-controls="primary-nav"
            onClick={() => setNavOpen((open) => !open)}
          >
            {navOpen ? 'Close' : 'Menu'}
          </button>
        </div>

        <nav id="primary-nav" aria-label="Primary" className="rail__nav" key={location.pathname}>
          {groups.map((group) => (
            <div key={group} className="rail__group">
              <p className="rail__group-label">{group}</p>
              <ul>
                {visible
                  .filter((item) => item.group === group)
                  .map((item) => (
                    <li key={item.to}>
                      <NavLink to={item.to} className="rail__link" onClick={() => setNavOpen(false)}>
                        {item.label}
                      </NavLink>
                    </li>
                  ))}
              </ul>
            </div>
          ))}
        </nav>

        <div className="rail__user">
          <span className="avatar" aria-hidden="true">
            {initials(user?.name ?? '')}
          </span>
          <div className="rail__user-text">
            <p className="rail__user-name">{user?.name}</p>
            <p className="rail__user-role">{user?.role}</p>
          </div>
          <button type="button" className="rail__signout" onClick={logout}>
            Sign out
          </button>
        </div>
      </aside>

      <main id="main" className="content" tabIndex={-1}>
        <Outlet />
      </main>
    </div>
  );
}
