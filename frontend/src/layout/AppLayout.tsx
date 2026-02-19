import { Briefcase, LayoutDashboard, LogOut, Menu, Shield, UserCheck, Users, X, BarChart3 } from "lucide-react";
import { useEffect, useState } from "react";
import { NavLink, Outlet, useLocation, useNavigate } from "react-router-dom";
import { logout } from "../auth/auth";
import { canAccessAdminPanel, canApplyAndInterview, canManageCandidates, canManageJobs, canViewReports } from "../auth/capabilities";

export function AppLayout(): JSX.Element {
  const navigate = useNavigate();
  const location = useLocation();
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [menuOpen, setMenuOpen] = useState(false);
  const canSeeAdmin = canAccessAdminPanel();
  const canSeeJobs = canManageJobs() || canApplyAndInterview();
  const canSeeCandidates = canManageCandidates();
  const canSeeApplications = canApplyAndInterview();
  const canSeeReports = canViewReports();

  useEffect(() => {
    setSidebarOpen(false);
    setMenuOpen(false);
  }, [location.pathname]);

  const pageTitle = (() => {
    if (location.pathname.startsWith("/jobs/")) return "Job Detail";
    if (location.pathname.startsWith("/jobs")) return "Jobs";
    if (location.pathname.startsWith("/candidates/")) return "Candidate Detail";
    if (location.pathname.startsWith("/candidates")) return "Candidates";
    if (location.pathname.startsWith("/interviews")) return "Interview";
    if (location.pathname.startsWith("/applications")) return "Applications";
    if (location.pathname.startsWith("/reports")) return "Reports";
    if (location.pathname.startsWith("/admin")) return "Admin";
    return "Dashboard";
  })();

  const breadcrumb = (() => {
    if (location.pathname.startsWith("/jobs/")) return "Jobs / Job Detail";
    if (location.pathname.startsWith("/candidates/")) return "Candidates / Candidate Detail";
    if (location.pathname.startsWith("/interviews")) return "Jobs / Applications / Interview";
    if (location.pathname.startsWith("/applications")) return "Applications";
    if (location.pathname.startsWith("/reports")) return "Reports";
    if (location.pathname.startsWith("/jobs")) return "Jobs";
    if (location.pathname.startsWith("/candidates")) return "Candidates";
    if (location.pathname.startsWith("/admin")) return "Admin";
    return "Dashboard";
  })();

  return (
    <div className="app-shell">
      <aside className={`app-sidebar ${sidebarOpen ? "open" : ""}`}>
        <div className="sidebar-brand">
          <span className="brand-dot" />
          <div>
            <strong>IK Otomasyon</strong>
            <p className="muted">Enterprise HR SaaS</p>
          </div>
        </div>

        <nav className="sidebar-nav" aria-label="Primary navigation">
          <NavLink to="/dashboard" className={({ isActive }) => `sidebar-link ${isActive ? "active" : ""}`}>
            <LayoutDashboard size={18} />
            <span>Dashboard</span>
          </NavLink>
          {canSeeJobs ? (
            <NavLink to="/jobs" className={({ isActive }) => `sidebar-link ${isActive ? "active" : ""}`}>
              <Briefcase size={18} />
              <span>Jobs</span>
            </NavLink>
          ) : null}
          {canSeeCandidates ? (
            <NavLink to="/candidates" className={({ isActive }) => `sidebar-link ${isActive ? "active" : ""}`}>
              <Users size={18} />
              <span>Candidates</span>
            </NavLink>
          ) : null}
          {canSeeApplications ? (
            <NavLink to="/applications" className={({ isActive }) => `sidebar-link ${isActive ? "active" : ""}`}>
              <UserCheck size={18} />
              <span>Applications</span>
            </NavLink>
          ) : null}
          {canSeeReports ? (
            <NavLink to="/reports" className={({ isActive }) => `sidebar-link ${isActive ? "active" : ""}`}>
              <BarChart3 size={18} />
              <span>Reports</span>
            </NavLink>
          ) : null}
          {canSeeAdmin ? (
            <NavLink to="/admin" className={({ isActive }) => `sidebar-link ${isActive ? "active" : ""}`}>
              <Shield size={18} />
              <span>Admin</span>
            </NavLink>
          ) : null}
        </nav>
      </aside>

      <div className="app-main">
        <header className="top-nav modern-topbar">
          <div className="topbar-left">
            <button
              type="button"
              className="button ghost mobile-menu-btn"
              onClick={() => setSidebarOpen((open) => !open)}
              aria-label="Toggle navigation"
            >
              {sidebarOpen ? <X size={16} /> : <Menu size={16} />}
            </button>
            <div>
              <p className="muted topbar-breadcrumb">{breadcrumb}</p>
              <h1 className="topbar-title">{pageTitle}</h1>
            </div>
          </div>

          <div className="user-menu-wrap">
            <button type="button" className="button ghost" onClick={() => setMenuOpen((v) => !v)} aria-label="User menu">
              <span className="user-avatar" aria-hidden>
                HR
              </span>
              <span>User</span>
            </button>
            {menuOpen ? (
              <div className="user-menu">
                <button
                  type="button"
                  className="dropdown-item"
                  onClick={() => {
                    logout();
                    navigate("/login", { replace: true });
                  }}
                >
                  <LogOut size={16} />
                  <span>Logout</span>
                </button>
              </div>
            ) : null}
          </div>
        </header>

        {sidebarOpen ? (
          <button type="button" className="sidebar-backdrop" onClick={() => setSidebarOpen(false)} aria-label="Close navigation" />
        ) : null}

        <main className="page-container modern-container">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
