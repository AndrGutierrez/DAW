import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useResource } from '../api/useResource';
import { useOperationalRefresh } from '../api/useOperationalRefresh';
import { Button } from './ui/Controls';
import { Icon } from './ui/Icon';
import { SidePanel } from './SidePanel';
export type NotificationSnapshot = { items: { id: string; farmId: string; kind: string; severity: 'Critical' | 'Warning' | 'Information'; title: string; message: string; href: string }[]; total: number; generatedAt: string };

export function NotificationCenter() {
  const [open, setOpen] = useState(false); const [category, setCategory] = useState('all');
  const feed = useResource<NotificationSnapshot>('/api/notifications', true, true);
  useOperationalRefresh(feed.reload);
  const categories: Record<string, string> = { all: 'Todos', stock: 'Inventario', health: 'Sanidad', withdrawal: 'Retiro', capacity: 'Potreros', growth: 'Crecimiento' };
  const items = feed.data?.items.filter(item => category === 'all' || item.kind === category) || [];
  return <><Button className="icon-button notification-trigger" aria-label={'Notificaciones: ' + (feed.data?.total ?? 0) + ' avisos activos'} onClick={() => { setOpen(true); feed.reload(); }}><Icon name="alert" />{Boolean(feed.data?.total) && <span className="notification-count" aria-hidden="true">{feed.data!.total > 99 ? '99+' : feed.data!.total}</span>}</Button>
    {open && <SidePanel title="Notificaciones" eyebrow="AVISOS DE TUS FINCAS" closeLabel="Cerrar notificaciones" onRequestClose={() => setOpen(false)}>
      <p className="muted">Condiciones activas según tus permisos y fincas asignadas. Al resolver la causa, el aviso desaparece.</p>
      <div className="notification-filters" aria-label="Filtrar notificaciones">{Object.entries(categories).filter(([key]) => key === 'all' || feed.data?.items.some(item => item.kind === key)).map(([key, label]) => <Button key={key} aria-pressed={category === key} variant={category === key ? 'primary' : 'secondary'} onClick={() => setCategory(key)}>{label}</Button>)}</div>
      {feed.loading && <p role="status">Actualizando avisos…</p>}{feed.error && <p role="alert" className="error-banner">{feed.error}</p>}
      <ul className="notification-list">{items.map(item => <li key={item.id} className={'notification-item ' + item.severity.toLowerCase()}><strong>{item.title}</strong><p>{item.message}</p><Link to={item.href} onClick={() => setOpen(false)}>Revisar {categories[item.kind]?.toLowerCase() || 'registro'}</Link></li>)}</ul>
      {!feed.loading && !feed.error && !items.length && <p className="panel empty-state">No hay avisos activos en esta categoría.</p>}
      {feed.data && <p className="muted">{feed.data.total} avisos activos · vista de hasta 25 por categoría · actualización {new Date(feed.data.generatedAt).toLocaleTimeString('es-VE')}</p>}
      <Button variant="secondary" disabled={feed.loading} onClick={feed.reload}>Actualizar avisos</Button>
    </SidePanel>}
  </>;
}
