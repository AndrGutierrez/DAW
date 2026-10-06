import { useState } from 'react';
import { Link, NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { ThemeButton } from './ThemeButton';

export function AppLayout() {
  const { session, logout } = useAuth();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  async function signOut() {
    setBusy(true); setError('');
    try { await logout(); }
    catch { setError('No se pudo cerrar la sesión en el servidor. Inténtalo de nuevo.'); }
    finally { setBusy(false); }
  }
  const user = session!.user;
  return <div className="app-shell">
    <aside className="sidebar">
      <Link className="brand" to="/animals"><span className="brand-mark">G</span><span>Gestión ganadera<small>Tu finca, en perspectiva</small></span></Link>
      <div className="sidebar-section">SEGUIMIENTO DEL GANADO</div>
      <nav aria-label="Principal"><NavLink to="/animals"><span aria-hidden="true">◈</span>Animales</NavLink><NavLink to="/account"><span aria-hidden="true">◎</span>Mi cuenta</NavLink></nav>
      <div className="sidebar-note"><span className="eyebrow">UNET · GRUPO 3</span><p>El animal es el centro de cada registro.</p></div>
    </aside>
    <div className="main-shell">
      <header className="app-header"><span className="header-title">Gestión y seguimiento</span><div className="header-actions"><ThemeButton /><span className="avatar" aria-hidden="true">{(user.fullName || user.username).slice(0, 1).toUpperCase()}</span><div className="header-user">{user.fullName || user.username}<small>{user.roles.join(' · ') || 'Sin rol asignado'}</small></div><button className="button secondary" onClick={signOut} disabled={busy}>{busy ? 'Cerrando…' : 'Salir'}</button></div></header>
      {error && <p role="alert" className="error-message logout-error">{error}</p>}
      <main className="app-main"><Outlet /></main>
      <footer className="app-footer">Gestión ganadera · Universidad Nacional Experimental del Táchira</footer>
    </div>
  </div>;
}
