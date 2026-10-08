import { useEffect, useState } from 'react';
import { Link, NavLink, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { dashboardPermissions } from '../api/operations';
import { homePath } from '../auth/navigation';
import { ThemeButton } from './ThemeButton';
import { Button } from './ui/Controls';
import { Icon } from './ui/Icon';
import { useNavigationProtection } from './NavigationProtection';

export function AppLayout() {
  const { session, logout, can, isAdmin } = useAuth();
  const { confirmExit } = useNavigationProtection();
  const location = useLocation();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const section = location.pathname.startsWith('/dashboard') ? 'Indicadores de tu finca' : location.pathname.startsWith('/inventory') ? 'Insumos y existencias' : location.pathname.startsWith('/reports') ? 'Reportes de seguimiento' : location.pathname.startsWith('/paddocks') ? 'Espacios y ocupación' : location.pathname.startsWith('/weighing') ? 'Control de crecimiento' : location.pathname.startsWith('/account') ? 'Tu cuenta' : 'Tu ganado';
  useEffect(() => {
    document.title = section + ' · Gestión ganadera';
    window.scrollTo({ top: 0, behavior: 'instant' });
    document.getElementById('main-content')?.focus({ preventScroll: true });
  }, [location.pathname, section]);
  async function signOut() {
    if (!await confirmExit()) return;
    setBusy(true); setError('');
    try { await logout(); }
    catch { setError('No se pudo cerrar la sesión en el servidor. Inténtalo de nuevo.'); }
    finally { setBusy(false); }
  }
  const user = session!.user;
  return <div className="app-shell"><a className="skip-link" href="#main-content">Saltar al contenido</a>
    <aside className="sidebar">
      <Link className="brand" to={homePath(isAdmin, can)}><span className="brand-mark"><Icon name="leaf" size={25} /></span><span>Gestión ganadera<small>Tu finca, en perspectiva</small></span></Link>
      <div className="sidebar-section">TU FINCA</div>
      <nav aria-label="Principal">{isAdmin && dashboardPermissions.every(can) && <NavLink to="/dashboard"><Icon name="growth" /><span>Dashboard</span></NavLink>}<NavLink to="/animals"><Icon name="animal" /><span>Animales</span></NavLink>{can('weights.create') && can('animals.list') && can('animals.get') && <NavLink to="/weighing"><Icon name="scale" /><span>Pesaje</span></NavLink>}{can('paddocks.list') && <NavLink to="/paddocks"><Icon name="paddock" /><span>Potreros</span></NavLink>}{can("inventory.list") && can("products.list") && <NavLink to="/inventory"><Icon name="inventory" /><span>Inventario</span></NavLink>}{can("animals.list") && (can("clinical.list") || can("production.list")) && <NavLink to="/reports"><Icon name="report" /><span>Reportes</span></NavLink>}<NavLink to="/account"><Icon name="user" /><span>Mi cuenta</span></NavLink></nav>
      <div className="sidebar-note"><Icon name="leaf" size={24} /><p>Una finca conectada.<br />Cada animal cuenta.</p><small>Proyecto académico · Grupo 3</small></div>
    </aside>
    <div className="main-shell">
      <header className="app-header"><div className="header-title"><span className="eyebrow">GESTIÓN GANADERA</span>{section}</div><div className="header-actions"><ThemeButton /><span className="avatar" aria-hidden="true">{(user.fullName || user.username).slice(0, 1).toUpperCase()}</span><div className="header-user">{user.fullName || user.username}<small>{user.roles.join(' · ') || 'Sin rol asignado'}</small></div><Button variant="secondary" onClick={signOut} disabled={busy}><Icon name="logout" size={18} />{busy ? 'Cerrando…' : 'Salir'}</Button></div></header>
      {error && <p role="alert" className="error-message logout-error">{error}</p>}
      <main id="main-content" className="app-main" tabIndex={-1}><Outlet /></main>
      <footer className="app-footer">Gestión ganadera · Universidad Nacional Experimental del Táchira</footer>
    </div>
  </div>;
}
