import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AuthProvider, useAuth } from '@/core/auth/AuthContext';
import { PERMISSIONS } from '@/core/auth/permissions';
import { RequireAuth, RequirePermission } from '@/core/guards/guards';
import { AccountPage } from '@/features/account/AccountPage';
import { RolesPage } from '@/features/administration/RolesPage';
import { LoginPage } from '@/features/authentication/LoginPage';
import { EmployeesPage } from '@/features/employees/EmployeesPage';
import { OrganizationPage } from '@/features/organizations/OrganizationPage';
import { OrganizationsPage } from '@/features/organizations/OrganizationsPage';
import { AppShell } from '@/layout/AppShell';
import { ToastProvider } from '@/shared/components/Toast';
import { Link } from 'react-router-dom';

/** Sends each user to the first area their permissions allow. */
function HomeRedirect() {
  const { can } = useAuth();
  if (can(PERMISSIONS.Organization.View)) return <Navigate to="/organizations" replace />;
  if (can(PERMISSIONS.Employee.View)) return <Navigate to="/employees" replace />;
  return <Navigate to="/account" replace />;
}

function NotFound() {
  return (
    <div className="state state--page state--empty">
      <h2 className="state__title">Page not found</h2>
      <p className="state__text">That address doesn't match anything in WorkforceConnect.</p>
      <Link className="btn btn--secondary" to="/">
        Go home
      </Link>
    </div>
  );
}

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<RequireAuth />}>
        <Route element={<AppShell />}>
          <Route index element={<HomeRedirect />} />
          <Route
            path="organizations"
            element={
              <RequirePermission permission={PERMISSIONS.Organization.View}>
                <OrganizationsPage />
              </RequirePermission>
            }
          />
          <Route
            path="organizations/:orgId"
            element={
              <RequirePermission permission={PERMISSIONS.Organization.View}>
                <OrganizationPage />
              </RequirePermission>
            }
          />
          <Route
            path="employees"
            element={
              <RequirePermission permission={PERMISSIONS.Employee.View}>
                <EmployeesPage />
              </RequirePermission>
            }
          />
          <Route
            path="roles"
            element={
              <RequirePermission permission={PERMISSIONS.Role.View}>
                <RolesPage />
              </RequirePermission>
            }
          />
          <Route path="account" element={<AccountPage />} />
          <Route path="*" element={<NotFound />} />
        </Route>
      </Route>
    </Routes>
  );
}

export function App() {
  return (
    <BrowserRouter>
      <ToastProvider>
        <AuthProvider>
          <AppRoutes />
        </AuthProvider>
      </ToastProvider>
    </BrowserRouter>
  );
}
