import { useState } from 'react';
import type { FormEvent } from 'react';
import { useAuth } from '../auth/AuthContext';
import { useResource } from '../api/useResource';
import { params } from '../api/operations';
import type { CarePage } from '../api/care';
import { auditActions, auditActor, auditChanges, auditEntity, auditField, auditTime, auditValue } from '../api/audit';
import type { AuditEntry, AuditDetail, AuditOptions } from '../api/audit';
import { Button, Input, Select } from '../components/ui/Controls';
import { Field } from '../components/Field';
import { Icon } from '../components/ui/Icon';
import { TableScroll } from '../components/TableScroll';
import { SidePanel } from '../components/SidePanel';
import { CarePagination } from '../components/CareForm';

function AuditEventPanel({ id, onClose }: { id: string; onClose: () => void }) {
  const detail = useResource<AuditDetail>('/api/admin/auditlogs/' + id);
  const [allFields, setAllFields] = useState(false);
  const changes = detail.data ? auditChanges(detail.data) : [];
  const visible = changes.filter(c => allFields || c.changed);
  return <SidePanel title="Detalle del evento" eyebrow="AUDITORÍA" closeLabel="Cerrar detalle de auditoría" onRequestClose={onClose}>
    {detail.loading ? <p role="status">Cargando evento…</p> : detail.error ? <div className="error-banner"><p role="alert">{detail.error}</p><Button onClick={detail.reload}>Reintentar</Button></div> : detail.data && <>
      <div className="audit-event-heading"><span className={'status-pill ' + (detail.data.entry.action === 'LoginRejected' || detail.data.entry.action === 'Deleted' ? 'warning' : 'neutral')}>{auditActions[detail.data.entry.action] || detail.data.entry.action}</span><h3>{auditEntity(detail.data.entry.entityName)}</h3><p className="muted">{auditTime(detail.data.entry.occurredAt)}</p></div>
      <dl className="audit-metadata"><div><dt>Usuario</dt><dd>{auditActor(detail.data.entry)}{detail.data.entry.userId && <small className="muted">{detail.data.entry.userId}</small>}</dd></div><div><dt>Finca</dt><dd>{detail.data.entry.farmName || (detail.data.entry.farmId ? 'Finca no disponible' : 'Sin finca asociada')}</dd></div><div><dt>Registro afectado</dt><dd className="audit-id">{detail.data.entry.entityId || 'Sin registro asociado'}</dd></div><div><dt>IP de origen</dt><dd>{detail.data.ipAddress || 'No registrada en este evento'}</dd></div><div><dt>Evento</dt><dd className="audit-id">{detail.data.entry.id}</dd></div></dl>
      <p className="muted audit-origin-note">La IP identifica la conexión recibida por el servidor; puede corresponder a un proxy.</p>
      <div className="audit-changes-heading"><h3>Valores registrados</h3>{changes.some(c => !c.changed) && <label className="checkbox-label"><Input type="checkbox" checked={allFields} onChange={e => setAllFields(e.target.checked)}/>Mostrar campos sin cambios</label>}</div>
      {visible.length ? <TableScroll><table className="audit-changes" aria-label="Cambios del evento"><thead><tr><th>Campo</th><th>Antes</th><th>Después</th></tr></thead><tbody>{visible.map(c => <tr key={c.field} data-changed={c.changed}><th scope="row">{auditField(c.field)}</th><td data-label="Antes">{c.hadBefore ? auditValue(c.before, c.field, detail.data!.entry.entityName) : 'No existía'}</td><td data-label="Después">{c.hasAfter ? auditValue(c.after, c.field, detail.data!.entry.entityName) : 'Sin valor posterior'}</td></tr>)}</tbody></table></TableScroll> : <p className="muted">Este evento no tiene una comparación de valores. Los accesos registran la acción y su momento, sin guardar credenciales.</p>}
    </>}
  </SidePanel>;
}
const initial = { search: '', actor: '', action: '', entityName: '', farmId: '', from: '', to: '', page: 1, pageSize: 25 };
export function AuditLogsPage() {
  const { isAdmin, can } = useAuth();
  const allowed = isAdmin && can('auditlogs.list');
  const [filter, setFilter] = useState(initial), [selected, setSelected] = useState<string | null>(null), [filterError, setFilterError] = useState(''), [formKey, setFormKey] = useState(0);
  const logs = useResource<CarePage<AuditEntry>>('/api/admin/auditlogs?' + params(filter), allowed);
  const options = useResource<AuditOptions>('/api/admin/auditlogs/options', allowed);
  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); const form = new FormData(event.currentTarget);
    const value = (key: string) => String(form.get(key) || '').trim();
    const from = value('from'), to = value('to');
    if (from && to && from > to) { setFilterError('La fecha inicial debe ser anterior o igual a la final.'); return; }
    setFilterError(''); setFilter({ ...initial, search: value('search'), actor: value('actor'), action: value('action'), entityName: value('entityName'), farmId: value('farmId'), from, to });
  }
  function clear() { setFilter(initial); setFilterError(''); setFormKey(k => k + 1); }
  if (!allowed) return <p role="alert">La auditoría requiere acceso de administrador y permiso de consulta.</p>;
  return <section className="operations-page audit-page"><div className="page-heading"><div><span className="eyebrow">CONTROL Y TRAZABILIDAD</span><h1>Auditoría</h1><p className="muted">Consulta quién realizó cada cambio y revisa los valores registrados.</p></div><Button variant="secondary" disabled={logs.loading} onClick={() => { logs.reload(); options.reload(); }}><Icon name="refresh"/>Actualizar</Button></div>
    <form key={formKey} className="filter-bar audit-filters" onSubmit={submit}>
      <Field label="Buscar registro"><Input name="search" type="search" maxLength={150} placeholder="Tipo de registro o identificador" defaultValue={filter.search}/></Field>
      <Field label="Usuario"><Input name="actor" type="search" maxLength={100} placeholder="Nombre o usuario" defaultValue={filter.actor}/></Field>
      <Field label="Acción"><Select name="action" defaultValue={filter.action}><option value="">Todas las acciones</option>{options.data?.actions.map(a => <option key={a} value={a}>{auditActions[a] || a}</option>)}</Select></Field>
      <Field label="Tipo de registro"><Select name="entityName" defaultValue={filter.entityName}><option value="">Todos los tipos</option>{options.data?.entities.map(e => <option key={e} value={e}>{auditEntity(e)}</option>)}</Select></Field>
      <Field label="Finca"><Select name="farmId" defaultValue={filter.farmId}><option value="">Todas las fincas y accesos</option>{options.data?.farms.map(f => <option key={f.id} value={f.id}>{f.name}</option>)}</Select></Field>
      <Field label="Desde (UTC)"><Input name="from" type="date" defaultValue={filter.from}/></Field><Field label="Hasta (UTC)"><Input name="to" type="date" defaultValue={filter.to}/></Field>
      <div className="button-row audit-filter-actions"><Button type="submit"><Icon name="search"/>Filtrar</Button><Button type="button" variant="secondary" onClick={clear}>Limpiar</Button></div>
    </form>
    {filterError && <p className="error-banner" role="alert">{filterError}</p>}{options.error && <div className="error-banner"><p role="alert">No pudimos cargar las opciones: {options.error}</p><Button onClick={options.reload}>Reintentar opciones</Button></div>}
    {logs.loading ? <div className="panel skeleton" role="status" aria-label="Cargando auditoría" style={{ minHeight: 180 }}/> : logs.error ? <div className="error-banner"><p role="alert">{logs.error}</p><Button onClick={logs.reload}>Reintentar</Button></div> : logs.data && <article className="panel audit-list"><div className="audit-list-heading"><h2>Actividad registrada</h2><span className="muted">Más reciente primero · horas locales</span></div>
      {logs.data.items.length ? <TableScroll><table aria-label="Eventos de auditoría"><thead><tr><th>Fecha y hora</th><th>Usuario</th><th>Acción</th><th>Registro</th><th>Finca</th><th>Detalle</th></tr></thead><tbody>{logs.data.items.map(entry => <tr key={entry.id}><td className="audit-date">{auditTime(entry.occurredAt)}</td><td>{auditActor(entry)}</td><td><span className={'status-pill ' + (entry.action === 'LoginRejected' || entry.action === 'Deleted' ? 'warning' : 'neutral')}>{auditActions[entry.action] || entry.action}</span></td><td>{auditEntity(entry.entityName)}{entry.entityId && <small className="audit-id muted">{entry.entityId}</small>}</td><td>{entry.farmName || (entry.farmId ? 'Finca no disponible' : 'Sin finca asociada')}</td><td>{can('auditlogs.get') && <Button variant="secondary" aria-label={'Ver evento de ' + auditEntity(entry.entityName) + ' del ' + auditTime(entry.occurredAt)} onClick={() => setSelected(entry.id)}><Icon name="eye"/>Ver detalle</Button>}</td></tr>)}</tbody></table></TableScroll> : <div className="audit-empty"><Icon name="audit" size={32}/><h3>No hay eventos con estos filtros</h3><p className="muted">Amplía las fechas o limpia los filtros para consultar otros registros.</p></div>}
      <CarePagination {...logs.data} onPage={page => setFilter(f => ({ ...f, page }))}/>
    </article>}
    {selected && <AuditEventPanel key={selected} id={selected} onClose={() => setSelected(null)}/>}
  </section>;
}
