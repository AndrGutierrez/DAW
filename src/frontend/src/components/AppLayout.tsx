import { Suspense, useEffect, useState } from 'react';
import * as Popover from '@radix-ui/react-popover';
import { Link, NavLink, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { dashboardPermissions } from '../api/operations';
import { homePath } from '../auth/navigation';
import { ThemeButton } from './ThemeButton';
import { RouteLoading } from './RouteLoading';
import { Button } from './ui/Controls';
import { Icon } from './ui/Icon';
import type { IconName } from './ui/Icon';
import { useNavigationProtection } from './NavigationProtection';

export function AppLayout() {
  const { session, logout, can, isAdmin } = useAuth();
  const { confirmExit } = useNavigationProtection();
  const location = useLocation();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [moreOpen, setMoreOpen] = useState(false);
  const section = location.pathname.startsWith('/dashboard') ? 'Indicadores de tu finca' : location.pathname.startsWith('/inventory') ? 'Insumos y existencias' : location.pathname.startsWith('/reports') ? 'Reportes de seguimiento' : location.pathname.startsWith('/paddocks') ? 'Espacios y ocupación' : location.pathname.startsWith('/transfers') ? 'Rotación del ganado' : location.pathname.startsWith('/weighing') ? 'Control de crecimiento' : location.pathname.startsWith('/monitoring') ? 'Seguimiento del ganado' : location.pathname.startsWith('/archive') ? 'Registros archivados' : location.pathname.startsWith('/auditlogs') ? 'Historial de actividad' : location.pathname.startsWith('/management') ? 'Fincas y catálogos' : location.pathname.startsWith('/users') ? 'Personas y accesos' : location.pathname.startsWith('/diagnostics') ? 'Diagnóstico de la aplicación' : location.pathname.startsWith('/account') ? 'Tu cuenta' : 'Tu ganado';
  const items: { to: string; label: string; icon: IconName; allowed: boolean }[] = ([
    { to: '/dashboard', label: 'Dashboard', icon: 'growth', allowed: isAdmin && dashboardPermissions.every(can) },
    { to: '/animals', label: 'Animales', icon: 'animal', allowed: true },
    { to: '/weighing', label: 'Pesaje', icon: 'scale', allowed: can('weights.create') && can('animals.list') && can('animals.get') },
    { to: '/paddocks', label: 'Potreros', icon: 'paddock', allowed: can('paddocks.list') },
    { to: '/transfers', label: 'Traslados', icon: 'location', allowed: ['animals.list', 'animals.get', 'animals.update', 'farms.list', 'paddocks.list', 'lots.list'].every(can) },
    { to: '/inventory', label: 'Inventario', icon: 'inventory', allowed: can('inventory.list') && can('products.list') },
    { to: '/reports', label: 'Reportes', icon: 'report', allowed: can('animals.list') && (can('clinical.list') || can('production.list')) },
    { to: '/monitoring', label: 'Seguimiento', icon: 'growth', allowed: can('animals.list') && can('weights.list') },
    { to: '/management', label: 'Fincas y catálogos', icon: 'paddock', allowed: isAdmin && ['farms', 'species', 'breeds', 'lots'].some(resource => can(resource + '.list')) },
    { to: '/users', label: 'Usuarios', icon: 'user', allowed: isAdmin && can('users.list') },
    { to: '/archive', label: 'Papelera', icon: 'archive', allowed: isAdmin && can('archive.list') },
    { to: '/auditlogs', label: 'Auditoría', icon: 'audit', allowed: isAdmin && can('auditlogs.list') },
  ] satisfies { to: string; label: string; icon: IconName; allowed: boolean }[]).filter(item => item.allowed);
  const remaining = items.slice(4);
  useEffect(() => {
    document.title = section + ' · Gestión ganadera';
    window.scrollTo({ top: 0, behavior: 'instant' });
    document.getElementById('main-content')?.focus({ preventScroll: true });
    setMoreOpen(false);
  }, [location.pathname, section]);
  async function signOut() {
    if (!await confirmExit()) return;
    setBusy(true); setError('');
    try { await logout(); }
    catch { setError('No se pudo cerrar la sesión en el servidor. Inténtalo de nuevo.'); }
    finally { setBusy(false); }
  }
  const user = session!.user;
  const links = (list: typeof items) => list.map(item => <NavLink key={item.to} to={item.to} onClick={() => setMoreOpen(false)}><Icon name={item.icon} /><span>{item.label}</span></NavLink>);
  return <div className="app-shell"><a className="skip-link" href="#main-content">Saltar al contenido</a>
    <aside className="sidebar"><Link className="brand" to={homePath(isAdmin, can)}><span className="brand-mark"><Icon name="leaf" size={25} /></span><span>Gestión ganadera<small>Control y trazabilidad</small></span></Link>
      <div className="sidebar-section">ESPACIO DE TRABAJO</div><nav className="desktop-nav" aria-label="Principal">{links(items)}</nav>
      <div className="sidebar-footer"><Icon name="paddock" size={16} /><span>Gestión de la finca</span></div>
    </aside>
    <nav className="mobile-nav" aria-label="Principal">{links(items.slice(0,4))}{remaining.length > 0 && <Popover.Root open={moreOpen} onOpenChange={setMoreOpen}><Popover.Trigger asChild><Button className="mobile-more" data-active={remaining.some(item => location.pathname.startsWith(item.to))} aria-label="Más secciones"><Icon name="menu" /><span>Más</span></Button></Popover.Trigger><Popover.Portal><Popover.Content className="mobile-menu" side="top" sideOffset={12} collisionPadding={12} aria-label="Más secciones"><div className="mobile-menu-heading"><strong>Más secciones</strong><Popover.Close asChild><Button className="icon-button" aria-label="Cerrar más secciones"><Icon name="close" /></Button></Popover.Close></div>{links(remaining)}</Popover.Content></Popover.Portal></Popover.Root>}</nav>
    <div className="main-shell"><header className="app-header"><div className="header-title">{section}</div><div className="header-actions"><ThemeButton /><Link className="avatar account-link" to="/account" aria-label="Mi cuenta" title="Mi cuenta"><span className="avatar-initial" aria-hidden="true">{(user.fullName || user.username).slice(0,1).toUpperCase()}</span><span className="avatar-hover" aria-hidden="true"><Icon name="user" size={21} /></span></Link><div className="header-user">{user.fullName || user.username}<small>{user.roles.join(' · ') || 'Sin rol asignado'}</small></div><Button variant="secondary" onClick={signOut} disabled={busy}><Icon name="logout" size={18} />{busy ? 'Cerrando…' : 'Salir'}</Button></div></header>
      {error && <p role="alert" className="error-message logout-error">{error}</p>}
      <main id="main-content" className="app-main" tabIndex={-1}><Suspense fallback={<RouteLoading />}><Outlet /></Suspense></main>
      <footer className="app-footer">Gestión ganadera · UNET</footer>
    </div>
  </div>;
}
