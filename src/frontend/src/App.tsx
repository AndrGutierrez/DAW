import { Navigate, Outlet, Route, Routes, useLocation } from 'react-router-dom';
import { useAuth } from './auth/AuthContext';
import { AppLayout } from './components/AppLayout';
import { LoginPage } from './pages/LoginPage';
import { AnimalsPage, AnimalPage } from './pages/AnimalsPage';

function RequireSession() {
  const auth = useAuth();
  const location = useLocation();
  if (auth.status !== 'authenticated')
    return <Navigate to="/login" state={{ from: location.pathname }} replace />;
  return <Outlet />;
}

function AccountPage() {
  const { session } = useAuth();
  const user = session!.user;
  return <section><span className="eyebrow">TU ACCESO</span><h1>Mi cuenta</h1><p className="muted">Identidad y permisos asignados por el administrador.</p><div className="panel account-panel"><dl className="detail-grid"><div><dt>Nombre</dt><dd>{user.fullName}</dd></div><div><dt>Usuario</dt><dd>{user.username}</dd></div><div><dt>Correo</dt><dd>{user.email}</dd></div><div><dt>Roles</dt><dd>{user.roles.join(', ') || 'Sin roles asignados'}</dd></div></dl><h2>Permisos</h2>{user.permissions.length ? <div className="permission-tags">{user.permissions.map(permission => <span className="tag" key={permission}>{permission}</span>)}</div> : <p className="muted">No tienes permisos asignados. Solicita acceso al administrador.</p>}</div></section>;
}

export function App() {
  const auth = useAuth();
  if (auth.status === 'checking') return <main className="connection-state"><div className="brand-mark">G</div><h1>Preparando tu finca</h1><p className="muted" role="status">Comprobando la sesión…</p><div className="skeleton connection-skeleton" /></main>;
  if (auth.status === 'unavailable') return <main className="connection-state"><h1>No pudimos conectar</h1><p role="alert">{auth.error}</p><button className="button primary" onClick={() => void auth.retry()}>Reintentar</button></main>;
  return <Routes>
    <Route path="/login" element={<LoginPage />} />
    <Route element={<RequireSession />}><Route element={<AppLayout />}><Route path="/animals" element={<AnimalsPage />} /><Route path="/animals/:id" element={<AnimalPage />} /><Route path="/account" element={<AccountPage />} /></Route></Route>
    <Route path="*" element={<Navigate to="/animals" replace />} />
  </Routes>;
}
