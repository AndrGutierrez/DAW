import { Icon } from '../components/ui/Icon';
import { Button, Input } from '../components/ui/Controls';
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { ApiError } from '../auth/session';
import { ThemeButton } from '../components/ThemeButton';

export function LoginPage() {
  const auth = useAuth();
  const location = useLocation();
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  if (auth.status === 'authenticated') {
    const destination = (location.state as { from?: string } | null)?.from;
    return <Navigate to={destination?.startsWith('/') && !destination.startsWith('//') && destination !== '/login' ? destination : '/animals'} replace />;
  }
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    setBusy(true); setError('');
    try { await auth.login(String(data.get('username')).trim(), String(data.get('password'))); }
    catch (error) {
      setError(error instanceof ApiError && error.status === 401
        ? 'Usuario o contraseña incorrectos, o cuenta inactiva.'
        : error instanceof Error ? error.message : 'No fue posible iniciar sesión.');
    }
    finally { setBusy(false); }
  }
  return <main className="login-page">
    <section className="login-story" aria-label="Gestión ganadera">
      <div className="brand"><span className="brand-mark"><Icon name="leaf" size={25} /></span><span>Gestión ganadera<small>Desarrollo de Aplicaciones Web · UNET</small></span></div>
      <div className="story-copy"><span className="eyebrow">CADA ANIMAL, UNA HISTORIA</span><h1>Conoce tu ganado.<br /><em>Cuida su futuro.</em></h1><p>Identificación, seguimiento y decisiones con los datos de tu finca en un mismo lugar.</p></div>
      <div className="pasture-art" aria-hidden="true"><div className="sun" /><div className="hill hill-back" /><div className="hill hill-front" /><span className="field-label">REGISTRAR · OBSERVAR · CUIDAR</span></div>
      <p className="story-foot">Proyecto de gestión ganadera · Grupo 3</p>
    </section>
    <section className="login-form-panel"><div className="login-theme"><ThemeButton /></div><div className="login-form-content">
      <span className="eyebrow">BIENVENIDO A TU FINCA</span><h2>Inicia sesión</h2><p className="muted">Accede con tu usuario o correo electrónico.</p>
      <form onSubmit={submit}>
        <label htmlFor="username">Usuario o correo</label><Input id="username" name="username" autoComplete="username" required maxLength={254} placeholder="Tu usuario" disabled={busy} />
        <label htmlFor="password">Contraseña</label><Input id="password" name="password" type="password" autoComplete="current-password" required maxLength={256} placeholder="Tu contraseña" disabled={busy} />
        {error && <p role="alert" className="error-message">{error}</p>}
        <Button className="button primary login-submit" disabled={busy}>{busy ? 'Iniciando sesión…' : 'Entrar a mi finca'}<Icon name="arrow" size={18} /></Button>
      </form>
      <p className="login-help">¿Necesitas acceso? Solicita una cuenta al administrador de tu finca.</p>
    </div><p className="login-footer">Universidad Nacional Experimental del Táchira</p></section>
  </main>;
}
