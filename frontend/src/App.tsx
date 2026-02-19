import { Suspense, lazy, useEffect } from "react";
import { Navigate, Route, Routes, useNavigate } from "react-router-dom";
import { ErrorBanner } from "./components/ErrorBanner";
import { LoadingSkeleton } from "./components/LoadingSkeleton";
import { ToastHost } from "./components/Toast";
import { setUnauthorizedHandler } from "./auth/auth";
import { AUTH_ENFORCED } from "./config";
import { AppLayout } from "./layout/AppLayout";
import { LoginPage } from "./pages/LoginPage";
import { NotFoundPage } from "./pages/NotFound";
import { RequireAuth } from "./routes/RequireAuth";

const DashboardPage = lazy(() => import("./pages/DashboardPage").then((m) => ({ default: m.DashboardPage })));
const JobsPage = lazy(() => import("./pages/JobsPage").then((m) => ({ default: m.JobsPage })));
const JobDetailPage = lazy(() => import("./pages/JobDetailPage").then((m) => ({ default: m.JobDetailPage })));
const CandidatesPage = lazy(() => import("./pages/CandidatesPage").then((m) => ({ default: m.CandidatesPage })));
const CandidateDetailPage = lazy(() => import("./pages/CandidateDetailPage").then((m) => ({ default: m.CandidateDetailPage })));
const InterviewPage = lazy(() => import("./pages/InterviewPage").then((m) => ({ default: m.InterviewPage })));
const ApplicationsPage = lazy(() => import("./pages/ApplicationsPage").then((m) => ({ default: m.ApplicationsPage })));
const AdminPage = lazy(() => import("./pages/AdminPage").then((m) => ({ default: m.AdminPage })));
const ReportsPage = lazy(() => import("./pages/ReportsPage").then((m) => ({ default: m.ReportsPage })));

function UnauthorizedRedirectBinding(): null {
  const navigate = useNavigate();

  useEffect(() => {
    if (!AUTH_ENFORCED) {
      setUnauthorizedHandler(null);
      return () => setUnauthorizedHandler(null);
    }

    setUnauthorizedHandler(() => {
      navigate("/login", { replace: true });
    });

    return () => setUnauthorizedHandler(null);
  }, [navigate]);

  return null;
}

export default function App(): JSX.Element {
  return (
    <>
      <UnauthorizedRedirectBinding />
      <ErrorBanner />
      <ToastHost />

      <Routes>
        <Route path="/login" element={<LoginPage />} />

        <Route element={<RequireAuth />}>
          <Route element={<AppLayout />}>
            <Route path="/" element={<Navigate to="/dashboard" replace />} />
            <Route
              path="/dashboard"
              element={
                <Suspense fallback={<LoadingSkeleton lines={5} />}>
                  <DashboardPage />
                </Suspense>
              }
            />
            <Route
              path="/jobs"
              element={
                <Suspense fallback={<LoadingSkeleton lines={5} />}>
                  <JobsPage />
                </Suspense>
              }
            />
            <Route
              path="/jobs/:id"
              element={
                <Suspense fallback={<LoadingSkeleton lines={6} />}>
                  <JobDetailPage />
                </Suspense>
              }
            />
            <Route
              path="/candidates"
              element={
                <Suspense fallback={<LoadingSkeleton lines={5} />}>
                  <CandidatesPage />
                </Suspense>
              }
            />
            <Route
              path="/candidates/:id"
              element={
                <Suspense fallback={<LoadingSkeleton lines={6} />}>
                  <CandidateDetailPage />
                </Suspense>
              }
            />
            <Route
              path="/applications"
              element={
                <Suspense fallback={<LoadingSkeleton lines={6} />}>
                  <ApplicationsPage />
                </Suspense>
              }
            />
            <Route
              path="/interviews/:sessionId"
              element={
                <Suspense fallback={<LoadingSkeleton lines={6} />}>
                  <InterviewPage />
                </Suspense>
              }
            />
            <Route
              path="/admin"
              element={
                <Suspense fallback={<LoadingSkeleton lines={6} />}>
                  <AdminPage />
                </Suspense>
              }
            />
            <Route
              path="/reports"
              element={
                <Suspense fallback={<LoadingSkeleton lines={6} />}>
                  <ReportsPage />
                </Suspense>
              }
            />
          </Route>
        </Route>

        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </>
  );
}
