import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { useResource } from '../api/useResource';
import { errorMessage, fieldErrors } from '../api/errors';
import { formValues } from '../api/care';
import { text } from '../api/livestock';
import { Field } from '../components/Field';
import { SidePanel } from '../components/SidePanel';
import { Button, Input, Select, Textarea } from '../components/ui/Controls';
import { Icon } from '../components/ui/Icon';
import { CarePagination } from '../components/CareForm';
import { useFeedback } from '../components/Feedback';
import { useUnsavedChanges } from '../components/NavigationProtection';

type Kind = 'farms' | 'species' | 'breeds' | 'lots';
type CatalogData = { name: string; isActive: boolean; code?: string; address?: string | null; phone?: string | null; email?: string | null; purpose?: string; gestationDays?: number | null; speciesId?: string; origin?: string | null; farmId?: string; paddockId?: string | null };
type Row = { id: string; data: CatalogData };
const catalogs: Record<Kind, { label: string; singular: string; create: string; icon: 'paddock' | 'animal' | 'genealogy' | 'inventory'; description: string; dependencies: string[] }> = {
  farms: { label: 'Fincas', singular: 'finca', create: 'Nueva finca', icon: 'paddock', description: 'Identificación y contacto de tus unidades de producción.', dependencies: [] },
  species: { label: 'Especies', singular: 'especie', create: 'Nueva especie', icon: 'animal', description: 'Especies, propósito productivo y gestación de referencia.', dependencies: [] },
  breeds: { label: 'Razas', singular: 'raza', create: 'Nueva raza', icon: 'genealogy', description: 'Razas vinculadas a la especie de cada animal.', dependencies: ['species.list'] },
  lots: { label: 'Lotes', singular: 'lote', create: 'Nuevo lote', icon: 'inventory', description: 'Grupos productivos por finca y especie. La ubicación física se registra por animal.', dependencies: ['farms.list', 'species.list', 'paddocks.list'] },
};
const purposes = ['Meat', 'Milk', 'DualPurpose', 'Wool', 'Eggs', 'Work'];

export function ManagementPage() {
  const { isAdmin, can, request } = useAuth(); const { confirm, notify } = useFeedback();
  const [query] = useSearchParams();
  const available = (Object.keys(catalogs) as Kind[]).filter(kind => can(kind + '.list'));
  const selected = query.get('tab');
  const kind: Kind = available.includes(selected as Kind) ? selected as Kind : available[0] || 'farms';
  const allowed = isAdmin && available.length > 0;
  const info = catalogs[kind];
  const rows = useResource<Row[]>('/api/' + kind, allowed && can(kind + '.list'));
  const farms = useResource<Row[]>('/api/farms', allowed && kind === 'lots' && can('farms.list'));
  const species = useResource<Row[]>('/api/species', allowed && (kind === 'breeds' || kind === 'lots') && can('species.list'));
  const paddocks = useResource<Row[]>('/api/paddocks', allowed && kind === 'lots' && can('paddocks.list'));
  const references = [farms, species, paddocks];
  const referenceBusy = references.some(resource => resource.loading);
  const referenceError = references.find(resource => resource.error);
  const [editor, setEditor] = useState<{ kind: Kind; row?: Row } | null>(null);
  const [search, setSearch] = useState(''); const [page, setPage] = useState(1);
  const [busyId, setBusyId] = useState(''); const [error, setError] = useState('');
  if (!allowed) return <p role="alert">La gestión de fincas y catálogos requiere acceso de administrador y permisos de consulta.</p>;
  const eligible = info.dependencies.every(can) && !referenceBusy && !referenceError;
  const filtered = (rows.data || []).filter(row => (row.data.name + ' ' + (row.data.code || '')).toLocaleLowerCase().includes(search.toLocaleLowerCase()));
  const currentPage = Math.min(page, Math.max(1, Math.ceil(filtered.length / 20)));
  const related = (items: Row[] | null, id?: string | null) => items?.find(item => item.id === id)?.data.name || 'Sin asignación';
  async function archive(row: Row) {
    if (busyId || !await confirm('¿Archivar «' + row.data.name + '»? Se conservará el registro y podrá restaurarse desde Papelera. Las dependencias activas pueden impedir esta operación.', { destructive: true, confirmLabel: 'Archivar' })) return;
    setBusyId(row.id); setError('');
    try { await request('/api/' + kind + '/' + row.id, { method: 'DELETE' }); notify('Registro archivado.'); rows.reload(); }
    catch (failure) { setError(errorMessage(failure)); } finally { setBusyId(''); }
  }
  return <section className="operations-page management-page">
    <div className="section-heading"><div><span className="eyebrow">ESTRUCTURA DE TU FINCA</span><h1>Fincas y catálogos</h1></div><Link className="button secondary" to="/paddocks"><Icon name="location" />Gestionar potreros</Link></div>
    <nav className="catalog-tabs" aria-label="Catálogos de la finca">{available.map(tab => <Link key={tab} className={'button ' + (kind === tab ? 'primary' : 'secondary')} aria-current={kind === tab ? 'page' : undefined} to={'/management?tab=' + tab} onClick={() => { setSearch(''); setPage(1); setError(''); }}><Icon name={catalogs[tab].icon} />{catalogs[tab].label}</Link>)}</nav>
    <div className="management-heading"><div><h2>{info.label}</h2><p className="muted">{info.description}</p></div>{can(kind + '.create') && <Button variant="primary" disabled={!eligible || !!busyId} onClick={() => setEditor({ kind })}><Icon name="plus" />{info.create}</Button>}</div>
    <Field label="Buscar por nombre o código"><Input type="search" maxLength={100} value={search} onChange={event => { setSearch(event.target.value); setPage(1); }} /></Field>
    {(rows.error || error) && <div className="error-banner" role="alert">{rows.error || error}{rows.error && <Button onClick={rows.reload}>Reintentar</Button>}</div>}
    {referenceError && <div className="error-banner" role="alert">{referenceError.error}<Button onClick={() => references.forEach(resource => resource.reload())}>Reintentar catálogos</Button></div>}
    {!info.dependencies.every(can) && <p className="muted">La edición requiere permisos para consultar los catálogos relacionados.</p>}
    {rows.loading && <p role="status">Cargando {info.label.toLowerCase()}…</p>}
    {rows.data && <><div className="management-grid">{filtered.slice((currentPage - 1) * 20, currentPage * 20).map(row => <article className="panel management-card" key={row.id} aria-label={row.data.name}>
      <div className="management-card-heading"><span className="dashboard-metric-icon"><Icon name={info.icon} /></span><span className={'tag ' + (row.data.isActive ? 'positive' : '')}>{row.data.isActive ? 'Activo' : 'Inactivo'}</span></div>
      <h3>{row.data.name}</h3>
      <dl className="management-details">
        {row.data.code && <div><dt>Código</dt><dd>{row.data.code}</dd></div>}
        {kind === 'farms' && <>{row.data.address && <div><dt>Dirección</dt><dd>{row.data.address}</dd></div>}{row.data.phone && <div><dt>Teléfono</dt><dd>{row.data.phone}</dd></div>}{row.data.email && <div><dt>Correo</dt><dd>{row.data.email}</dd></div>}</>}
        {kind !== 'farms' && <div><dt>Propósito</dt><dd>{text(row.data.purpose)}</dd></div>}
        {kind === 'species' && <div><dt>Gestación de referencia</dt><dd>{row.data.gestationDays ? row.data.gestationDays + ' días' : 'Sin definir'}</dd></div>}
        {(kind === 'breeds' || kind === 'lots') && <div><dt>Especie</dt><dd>{related(species.data, row.data.speciesId)}</dd></div>}
        {kind === 'breeds' && row.data.origin && <div><dt>Origen</dt><dd>{row.data.origin}</dd></div>}
        {kind === 'lots' && <><div><dt>Finca</dt><dd>{related(farms.data, row.data.farmId)}</dd></div><div><dt>Potrero de referencia</dt><dd>{related(paddocks.data, row.data.paddockId)}</dd></div></>}
      </dl>
      <div className="button-row">{can(kind + '.update') && <Button disabled={!eligible || !!busyId} onClick={() => setEditor({ kind, row })}><Icon name="edit" size={17} />Editar {info.singular}</Button>}{can(kind + '.delete') && <Button variant="quiet" disabled={!!busyId} onClick={() => void archive(row)}><Icon name="archive" size={17} />{busyId === row.id ? 'Archivando…' : 'Archivar'}</Button>}</div>
    </article>)}</div>{!filtered.length && <p className="panel muted">{search ? 'No hay coincidencias para esta búsqueda.' : 'Todavía no hay registros. Crea el primero para comenzar.'}</p>}<CarePagination page={currentPage} pageSize={20} total={filtered.length} onPage={setPage} /></>}
    {editor && <CatalogEditor key={editor.kind + (editor.row?.id || 'new')} kind={editor.kind} row={editor.row} farms={farms.data || []} species={species.data || []} paddocks={paddocks.data || []} onClose={() => setEditor(null)} onSaved={() => { setEditor(null); rows.reload(); }} />}
  </section>;
}
function CatalogEditor({ kind, row, farms, species, paddocks, onClose, onSaved }: { kind: Kind; row?: Row; farms: Row[]; species: Row[]; paddocks: Row[]; onClose: () => void; onSaved: () => void }) {
  const { request } = useAuth(); const { confirm, notify } = useFeedback();
  const [dirty, setDirty] = useState(false); const [busy, setBusy] = useState(false);
  const [error, setError] = useState(''); const [errors, setErrors] = useState<Record<string, string>>({});
  const [farmId, setFarmId] = useState(row?.data.farmId || '');
  const clearDirty = useUnsavedChanges(dirty || busy);
  const d = row?.data; const info = catalogs[kind];
  async function close() { if (!busy && (!dirty || await confirm('Hay cambios sin guardar. ¿Descartarlos?', { destructive: true, confirmLabel: 'Descartar' }))) { clearDirty(); onClose(); } }
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (busy) return;
    const v = formValues(event.currentTarget); const fields = new FormData(event.currentTarget);
    const common = { name: v.text('name'), isActive: fields.has('isActive') };
    const payload = kind === 'farms' ? { ...common, code: v.text('code'), address: v.text('address'), phone: v.text('phone'), email: v.text('email') }
      : kind === 'species' ? { ...common, code: v.text('code'), purpose: v.text('purpose'), gestationDays: v.number('gestationDays') }
      : kind === 'breeds' ? { ...common, speciesId: v.text('speciesId'), purpose: v.text('purpose'), origin: v.text('origin') }
      : { ...common, farmId: d?.farmId || farmId, speciesId: v.text('speciesId'), purpose: v.text('purpose'), paddockId: v.text('paddockId') };
    setBusy(true); setError(''); setErrors({});
    try { await request('/api/' + kind + (row ? '/' + row.id : ''), { method: row ? 'PUT' : 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) }); clearDirty(); notify('Registro guardado.'); onSaved(); }
    catch (failure) { setError(errorMessage(failure)); setErrors(fieldErrors(failure)); } finally { setBusy(false); }
  }
  const selectItems = (items: Row[], selected?: string | null) => items.filter(item => item.data.isActive || item.id === selected).map(item => <option value={item.id} key={item.id}>{item.data.name}{item.data.isActive ? '' : ' · inactivo'}</option>);
  return <SidePanel title={row ? 'Editar ' + info.singular : info.create} eyebrow="GESTIÓN DE LA FINCA" closeLabel="Cerrar editor" onRequestClose={() => void close()}>
    <form onSubmit={event => void save(event)} onChange={() => setDirty(true)}><fieldset disabled={busy} className="form-grid">
      <Field label="Nombre" error={errors.name}><Input required name="name" maxLength={kind === 'farms' || kind === 'lots' ? 150 : 100} defaultValue={d?.name} /></Field>
      {(kind === 'farms' || kind === 'species') && <Field label="Código" error={errors.code}><Input required name="code" maxLength={20} defaultValue={d?.code} /></Field>}
      {kind === 'farms' && <><Field label="Dirección" error={errors.address}><Textarea name="address" maxLength={300} defaultValue={d?.address || ''} /></Field><Field label="Teléfono" error={errors.phone}><Input type="tel" name="phone" maxLength={50} defaultValue={d?.phone || ''} /></Field><Field label="Correo" error={errors.email}><Input type="email" name="email" maxLength={200} defaultValue={d?.email || ''} /></Field></>}
      {kind === 'lots' && <Field label="Finca" error={errors.farmid}><Select required name="farmId" disabled={!!row} value={farmId} onChange={event => { setFarmId(event.target.value); const form = event.currentTarget.form; const paddock = form?.elements.namedItem('paddockId') as HTMLSelectElement | null; if (paddock) paddock.value = ''; }}><option value="">Selecciona una finca</option>{selectItems(farms, d?.farmId)}</Select></Field>}
      {(kind === 'breeds' || kind === 'lots') && <Field label="Especie" error={errors.speciesid}><Select required name="speciesId" defaultValue={d?.speciesId || ''}><option value="">Selecciona una especie</option>{selectItems(species, d?.speciesId)}</Select></Field>}
      {kind !== 'farms' && <Field label="Propósito" error={errors.purpose}><Select name="purpose" required defaultValue={d?.purpose || 'Meat'}>{purposes.map(purpose => <option value={purpose} key={purpose}>{text(purpose)}</option>)}</Select></Field>}
      {kind === 'species' && <Field label="Gestación de referencia (días)" error={errors.gestationdays}><Input type="number" name="gestationDays" min="1" max="2147483647" step="1" defaultValue={d?.gestationDays ?? ''} /></Field>}
      {kind === 'breeds' && <Field label="Origen" error={errors.origin}><Input name="origin" maxLength={100} defaultValue={d?.origin || ''} /></Field>}
      {kind === 'lots' && <Field label="Potrero de referencia" error={errors.paddockid} hint="La ubicación física de cada animal se actualiza mediante un traslado."><Select name="paddockId" key={farmId} disabled={!farmId} defaultValue={d?.paddockId || ''}><option value="">Sin asignación</option>{selectItems(paddocks.filter(item => item.data.farmId === farmId), d?.paddockId)}</Select></Field>}
      <label className="check-field"><Input type="checkbox" name="isActive" defaultChecked={d?.isActive ?? true} />Registro activo</label>
    </fieldset>{error && <p className="error-banner" role="alert">{error}</p>}<div className="button-row"><Button type="submit" variant="primary" disabled={busy}>{busy ? 'Guardando…' : 'Guardar cambios'}</Button><Button type="button" disabled={busy} onClick={() => void close()}>Cancelar</Button></div></form>
  </SidePanel>;
}
