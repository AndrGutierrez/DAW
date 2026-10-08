import { Icon } from '../components/ui/Icon';
import { Button, Input } from '../components/ui/Controls';
import { useEffect, useRef, useState } from 'react';
import { PasswordInput } from '../components/ui/PasswordInput';
import { StatusNotice } from '../components/ui/StatusNotice';
import type { FormEvent } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { ApiError } from '../auth/session';
import { ThemeButton } from '../components/ThemeButton';

export function LoginPage() {
  const auth = useAuth();
  const location = useLocation();
  const [notice, setNotice] = useState<{ title: string; message: string; action: string } | null>(null);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const usernameInput = useRef<HTMLInputElement>(null);
  const passwordInput = useRef<HTMLInputElement>(null);
  const noticeElement = useRef<HTMLDivElement>(null);
  const submitting = useRef(false);
  useEffect(() => { if (notice) noticeElement.current?.focus(); }, [notice]);
  const [busy, setBusy] = useState(false);
  if (auth.status === 'authenticated') {
    const destination = (location.state as { from?: string } | null)?.from;
    return <Navigate to={destination?.startsWith('/') && !destination.startsWith('//') && destination !== '/login' ? destination : '/animals'} replace />;
  }
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (submitting.current) return;
    const data = new FormData(event.currentTarget);
    const username = String(data.get('username') || '').trim();
    const password = String(data.get('password') || '');
    const validation: Record<string, string> = {};
    if (!username) validation.username = 'Escribe tu usuario o correo.';
    if (!password) validation.password = 'Escribe tu contraseña.';
    setErrors(validation); setNotice(null);
    if (Object.keys(validation).length) {
      requestAnimationFrame(() => (validation.username ? usernameInput : passwordInput).current?.focus());
      return;
    }
    submitting.current = true; setBusy(true);
    try { await auth.login(username, password); }
    catch (failure) {
      if (failure instanceof ApiError && failure.status === 401)
        setNotice({ title: 'Revisa tus credenciales', message: 'Usuario o contraseña incorrectos, o cuenta inactiva.', action: 'Comprueba lo que escribiste. Si el problema continúa, contacta al administrador de tu finca.' });
      else if (failure instanceof ApiError && failure.status === 429)
        setNotice({ title: 'Espera antes de reintentar', message: 'El servicio recibió demasiados intentos de acceso.', action: 'Espera un momento y vuelve a intentarlo.' });
      else if (failure instanceof ApiError && failure.status < 500)
        setNotice({ title: 'No se pudo completar el acceso', message: 'La solicitud de acceso no pudo validarse.', action: 'Revisa los datos e inténtalo de nuevo. Si persiste, contacta al administrador.' });
      else
        setNotice({ title: 'No pudimos conectar', message: 'No se pudo comunicar con el servicio de la finca.', action: 'Revisa tu conexión y vuelve a pulsar Entrar a mi finca. Conservamos los datos del formulario.' });
    }
    finally { submitting.current = false; setBusy(false); }
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
      {auth.reason === 'expired' && !notice && <StatusNotice kind="info" title="Tu sesión terminó"><p>Vuelve a iniciar sesión para continuar en la pantalla que estabas consultando.</p></StatusNotice>}
      <form onSubmit={event => void submit(event)} noValidate aria-busy={busy}>
        <label htmlFor="username">Usuario o correo</label><Input ref={usernameInput} id="username" name="username" autoComplete="username" required maxLength={254} placeholder="Tu usuario" readOnly={busy} aria-invalid={!!errors.username} aria-describedby={errors.username ? 'username-error' : undefined} onInput={() => setErrors(current => ({ ...current, username: '' }))} />
        {errors.username && <small id="username-error" className="field-error">{errors.username}</small>}
        <label htmlFor="password">Contraseña</label><PasswordInput ref={passwordInput} id="password" name="password" autoComplete="current-password" required maxLength={256} placeholder="Tu contraseña" readOnly={busy} aria-invalid={!!errors.password} aria-describedby={errors.password ? 'password-error' : undefined} onInput={() => setErrors(current => ({ ...current, password: '' }))} />
        {errors.password && <small id="password-error" className="field-error">{errors.password}</small>}
        {notice && <StatusNotice ref={noticeElement} title={notice.title}><p>{notice.message}</p><p>{notice.action}</p></StatusNotice>}
        <Button className="button primary login-submit" type="submit" disabled={busy}>{busy ? 'Iniciando sesión…' : 'Entrar a mi finca'}<Icon name={busy ? 'spinner' : 'arrow'} className={busy ? 'loading-icon' : undefined} size={18} /></Button>
        {busy && <span className="sr-only" role="status">Comprobando tus credenciales. Espera un momento.</span>}
      </form>
      <p className="login-help">¿Necesitas acceso? Solicita una cuenta al administrador de tu finca.</p>
    </div><p className="login-footer">Universidad Nacional Experimental del Táchira</p></section>
  </main>;
}
