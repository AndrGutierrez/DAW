import { useState } from 'react';
import type { FormEvent } from 'react';
import { useAuth } from '../auth/AuthContext';
import { useResource } from '../api/useResource';
import { params } from '../api/operations';
import { auditField, auditTime, auditValue } from '../api/audit';
import type { CarePage } from '../api/care';
import { Button, Input, Select } from '../components/ui/Controls';
import { Field } from '../components/Field';
import { Icon } from '../components/ui/Icon';
import { TableScroll } from '../components/TableScroll';
import { SidePanel } from '../components/SidePanel';
import { CarePagination } from '../components/CareForm';
import { useFeedback } from '../components/Feedback';
import { errorMessage } from '../api/errors';
export const archiveResources: Record<string, string> = { farms: 'Fincas', species: 'Especies', breeds: 'Razas', paddocks: 'Potreros', lots: 'Lotes', categories: 'Categorías', products: 'Productos', inventory: 'Existencias', animals: 'Animales', weights: 'Pesajes', production: 'Producción', photos: 'Fotografías' };
type Entry = { id: string; resource: string; label: string; farmId: string | null; deletedAt: string; deletedByUserId: string | null };
type Detail = { entry: Entry; values: string };
function ArchivePanel({ entry, onClose, onRestored }: { entry: Entry; onClose: () => void; onRestored: () => void }) {
  const { request, can } = useAuth(); const { confirm, notify } = useFeedback();
  const result = useResource<Detail>('/api/admin/archive/' + entry.resource + '/' + entry.id);
  const [busy, setBusy] = useState(false), [error, setError] = useState('');
  async function restore() {
    if (!await confirm('¿Restaurar ' + entry.label + '? Volverá a las consultas y cálculos que correspondan. Las referencias y reglas de negocio se validarán antes de recuperarlo.', { confirmLabel: 'Restaurar' })) return;
    setBusy(true); setError('');
    try { await request('/api/admin/archive/' + entry.resource + '/' + entry.id + '/restore', { method: 'POST' }); notify('Registro restaurado.'); onRestored(); }
    catch (failure) { setError(errorMessage(failure)); } finally { setBusy(false); }
  }
  return <SidePanel title="Registro archivado" eyebrow="PAPELERA" closeLabel="Cerrar registro archivado" onRequestClose={() => { if (!busy) onClose(); }}>
    <h3>{entry.label}</h3><p className="muted">{archiveResources[entry.resource]} · archivado el {auditTime(entry.deletedAt)}</p>
    {result.loading ? <p role="status">Cargando registro…</p> : result.error ? <div className="error-banner"><p role="alert">{result.error}</p><Button onClick={result.reload}>Reintentar</Button></div> : result.data && <TableScroll><table aria-label="Datos conservados"><thead><tr><th>Campo</th><th>Valor</th></tr></thead><tbody>{Object.entries(JSON.parse(result.data.values) as Record<string, unknown>).map(([field, value]) => <tr key={field}><th scope="row">{auditField(field)}</th><td>{auditValue(value, field, entry.resource)}</td></tr>)}</tbody></table></TableScroll>}
    {error && <p className="error-banner" role="alert">{error}</p>}{can('archive.restore') && <Button disabled={busy || result.loading || !!result.error} onClick={() => void restore()}><Icon name="refresh"/>{busy ? 'Restaurando…' : 'Restaurar registro'}</Button>}
  </SidePanel>;
}
export function ArchivePage() {
  const { isAdmin, can } = useAuth(); const allowed = isAdmin && can('archive.list');
  const [query, setQuery] = useState({ search: '', resource: '', page: 1, pageSize: 25 });
  const [selected, setSelected] = useState<Entry | null>(null);
  const records = useResource<CarePage<Entry>>('/api/admin/archive?' + params(query), allowed);
  function filter(event: FormEvent<HTMLFormElement>) { event.preventDefault(); const data = new FormData(event.currentTarget); setQuery({ ...query, page: 1, search: String(data.get('search') || '').trim(), resource: String(data.get('resource') || '') }); }
  if (!allowed) return <p role="alert">La papelera requiere acceso de administrador y permiso de consulta.</p>;
  return <section className="operations-page"><div className="page-heading"><div><span className="eyebrow">CONSERVACIÓN DE INFORMACIÓN</span><h1>Papelera</h1><p className="muted">Los registros archivados se conservan y quedan fuera de las consultas operativas.</p></div><Button variant="secondary" disabled={records.loading} onClick={records.reload}><Icon name="refresh"/>Actualizar</Button></div>
    <form className="filter-bar" onSubmit={filter}><Field label="Buscar registro archivado"><Input name="search" type="search" maxLength={100} placeholder="Nombre o identificación"/></Field><Field label="Tipo de registro archivado"><Select name="resource"><option value="">Todos los tipos</option>{Object.entries(archiveResources).map(([key, label]) => <option key={key} value={key}>{label}</option>)}</Select></Field><Button type="submit"><Icon name="search"/>Filtrar</Button></form>
    {records.loading ? <p role="status">Cargando papelera…</p> : records.error ? <div className="error-banner"><p role="alert">{records.error}</p><Button onClick={records.reload}>Reintentar</Button></div> : records.data && <article className="panel audit-list">
      {records.data.items.length ? <TableScroll><table aria-label="Registros archivados"><thead><tr><th>Registro</th><th>Tipo</th><th>Fecha de archivo</th><th>Detalle</th></tr></thead><tbody>{records.data.items.map(entry => <tr key={entry.resource + entry.id}><th scope="row">{entry.label}</th><td>{archiveResources[entry.resource]}</td><td>{auditTime(entry.deletedAt)}</td><td>{can('archive.get') && <Button variant="secondary" aria-label={'Revisar ' + entry.label} onClick={() => setSelected(entry)}><Icon name="eye"/>Revisar registro</Button>}</td></tr>)}</tbody></table></TableScroll> : <div className="audit-empty"><Icon name="archive" size={32}/><h2>No hay registros archivados con estos filtros</h2><p className="muted">Los usuarios desactivados se consultan y reactivan desde Usuarios.</p></div>}
      <CarePagination {...records.data} onPage={page => setQuery({ ...query, page })}/>
    </article>}{selected && <ArchivePanel key={selected.resource + selected.id} entry={selected} onClose={() => setSelected(null)} onRestored={() => { setSelected(null); records.reload(); }}/>}
  </section>;
}
