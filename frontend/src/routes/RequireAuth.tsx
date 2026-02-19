import { Navigate, Outlet, useLocation } from "react-router-dom";
import { isAuthenticated } from "../auth/auth";
import { AUTH_ENFORCED } from "../config";

export function RequireAuth(): JSX.Element {
  if (!AUTH_ENFORCED) {
    return <Outlet />;
  }

  const location = useLocation();
  if (!isAuthenticated()) {
    return <Navigate to="/login" replace state={{ from: `${location.pathname}${location.search}` }} />;
  }

  return <Outlet />;
}
