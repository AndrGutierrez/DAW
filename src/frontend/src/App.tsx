import { lazy, Suspense } from 'react';
import { RouteLoading } from './components/RouteLoading';
import { Button } from './components/ui/Controls';
import { Link, Navigate, Outlet, Route, Routes, useLocation } from 'react-router-dom';
import { useAuth } from './auth/AuthContext';
import { homePath } from './auth/navigation';
import { AppLayout } from './components/AppLayout';
import { Icon } from './components/ui/Icon';
const LoginPage = lazy(() => import('./pages/LoginPage').then(module => ({ default: module.LoginPage })));
const AnimalEditorPage = lazy(() => import('./pages/AnimalEditorPage').then(module => ({ default: module.AnimalEditorPage })));
const WeighingPage = lazy(() => import('./pages/WeighingPage').then(module => ({ default: module.WeighingPage })));
const PaddocksPage = lazy(() => import('./pages/PaddocksPage').then(module => ({ default: module.PaddocksPage })));
const InventoryPage = lazy(() => import('./pages/InventoryPage').then(module => ({ default: module.InventoryPage })));
const DashboardPage = lazy(() => import('./pages/DashboardPage').then(module => ({ default: module.DashboardPage })));
const ReportsPage = lazy(() => import('./pages/ReportsPage').then(module => ({ default: module.ReportsPage })));
const UsersPage = lazy(() => import('./pages/UsersPage').then(module => ({ default: module.UsersPage })));
const MonitoringPage = lazy(() => import('./pages/MonitoringPage').then(module => ({ default: module.MonitoringPage })));
const AnimalsPage = lazy(() => import('./pages/AnimalsPage').then(module => ({ default: module.AnimalsPage })));
const AnimalPage = lazy(() => import('./pages/AnimalsPage').then(module => ({ default: module.AnimalPage })));
const AuditLogsPage = lazy(() => import('./pages/AuditLogsPage').then(module => ({ default: module.AuditLogsPage })));
const PerformancePage = lazy(() => import('./pages/PerformancePage').then(module => ({ default: module.PerformancePage })));

function RequireSession() {
  const auth = useAuth();
  const location = useLocation();
  if (auth.status !== 'authenticated')
    return <Navigate to="/login" state={{ from: location.pathname + location.search + location.hash }} replace />;
  return <Outlet />;
}

function AccountPage() {
  const { session, isAdmin } = useAuth();
  const user = session!.user;
  return <section><span className="eyebrow">TU ACCESO</span><h1>Mi cuenta</h1><p className="muted">Identidad y permisos asignados por el administrador.</p><div className="panel account-panel"><dl className="detail-grid"><div><dt>Nombre</dt><dd>{user.fullName}</dd></div><div><dt>Usuario</dt><dd>{user.username}</dd></div><div><dt>Correo</dt><dd>{user.email}</dd></div><div><dt>Roles</dt><dd>{user.roles.join(', ') || 'Sin roles asignados'}</dd></div></dl>{isAdmin && <p className="account-diagnostics"><Link className="button secondary" to="/diagnostics"><Icon name="growth" />Ver rendimiento de la aplicación</Link></p>}<h2>Permisos</h2>{user.permissions.length ? <div className="permission-tags">{user.permissions.map(permission => <span className="tag" key={permission}>{permission}</span>)}</div> : <p className="muted">No tienes permisos asignados. Solicita acceso al administrador.</p>}</div></section>;
}

export function App() {
  const auth = useAuth();
  if (auth.status === 'checking') return <main className="connection-state"><div className="brand-mark"><Icon name="leaf" size={25} /></div><h1>Preparando tu finca</h1><p className="muted" role="status">Comprobando la sesión…</p><div className="skeleton connection-skeleton" /></main>;
  if (auth.status === 'unavailable') return <main className="connection-state"><h1>No pudimos conectar</h1><p role="alert">{auth.error}</p><Button className="button primary" onClick={() => void auth.retry()}>Reintentar</Button></main>;
  return <Suspense fallback={<RouteLoading />}><Routes>
    <Route path="/login" element={<LoginPage />} />
    <Route element={<RequireSession />}><Route element={<AppLayout />}><Route path="/" element={<Navigate to={homePath(auth.isAdmin, auth.can)} replace />} /><Route path="/animals" element={<AnimalsPage />} /><Route path="/animals/new" element={<AnimalEditorPage />} /><Route path="/animals/:id/edit" element={<AnimalEditorPage />} /><Route path="/weighing" element={<WeighingPage />} /><Route path="/paddocks" element={<PaddocksPage />} /><Route path="/animals/:id" element={<AnimalPage />} /><Route path="/account" element={<AccountPage />} /><Route path="/diagnostics" element={<PerformancePage />} /><Route path="/inventory" element={<InventoryPage />} /><Route path="/dashboard" element={<DashboardPage />} /><Route path="/reports" element={<ReportsPage />} /><Route path="/users" element={<UsersPage />} /><Route path="/auditlogs" element={<AuditLogsPage />} /><Route path="/monitoring" element={<MonitoringPage />} /></Route></Route>
    <Route path="*" element={<Navigate to="/" replace />} />
  </Routes></Suspense>;
}
