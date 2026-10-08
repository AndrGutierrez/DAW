import { Button, Input, Select } from '../components/ui/Controls';
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { useResource } from '../api/useResource';
import type { CatalogItem } from '../api/livestock';
import type { CarePage } from '../api/care';
import { date, number } from '../api/livestock';
import { occupancyState, stayDays } from '../api/paddocks';
import type { PaddockData, PaddockSnapshot, Resident } from '../api/paddocks';
import { errorMessage, fieldErrors } from '../api/errors';
import type { FieldErrors } from '../api/errors';
import { Field } from '../components/Field';
import { CarePagination } from '../components/CareForm';
import { MovementForm } from '../components/AnimalLocation';
import { useUnsavedChanges } from '../components/NavigationProtection';
import { PaddockVisual } from '../components/PaddockVisual';
import { Icon } from '../components/ui/Icon';
import { useFeedback } from '../components/Feedback';

function PaddockEditor({ item, farms, onSaved, onCancel }: { item?: PaddockSnapshot; farms: CatalogItem[]; onSaved: () => void; onCancel: () => void }) {
  const { request } = useAuth();
  const { notify, confirm } = useFeedback();
  const [busy, setBusy] = useState(false), [error, setError] = useState(''), [errors, setErrors] = useState<FieldErrors>({});
  const [dirty, setDirty] = useState(false);
  const clearUnsaved = useUnsavedChanges(dirty);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (busy) return;
    const form = new FormData(event.currentTarget);
    const value = (key: string) => String(form.get(key) || '').trim();
    const data: PaddockData = { farmId: item?.data.farmId || value('farm'), name: value('name'), code: value('code') || null,
      areaHectares: value('area') ? Number(value('area')) : null, capacity: value('capacity') ? Number(value('capacity')) : null, isActive: form.get('active') === 'on' };
    setBusy(true); setError(''); setErrors({});
    try { await request('/api/paddocks' + (item ? '/' + item.id : ''), { method: item ? 'PUT' : 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(data) }); clearUnsaved(); setDirty(false); notify('Potrero guardado.'); onSaved(); }
    catch (failure) { setError(errorMessage(failure)); setErrors(fieldErrors(failure)); notify(errorMessage(failure), 'error'); }
    finally { setBusy(false); }
  }
  return <section className="panel paddock-detail" aria-labelledby="paddock-editor"><h2 id="paddock-editor">{item ? 'Editar potrero' : 'Registrar potrero'}</h2><form className="care-form" onSubmit={submit} onChange={() => setDirty(true)}><fieldset disabled={busy}><div className="form-grid">
    <Field label="Finca del potrero *" error={errors.farmid}><Select name="farm" required disabled={!!item} defaultValue={item?.data.farmId || ''}><option value="">Selecciona una finca</option>{farms.filter(f => f.data.isActive).map(f => <option value={f.id} key={f.id}>{f.data.name}</option>)}</Select></Field>
    <Field label="Nombre del potrero *" error={errors.name}><Input name="name" required maxLength={150} defaultValue={item?.data.name} /></Field>
    <Field label="Código del potrero" error={errors.code}><Input name="code" maxLength={30} defaultValue={item?.data.code || ''} /></Field>
    <Field label="Superficie (ha)" error={errors.areahectares}><Input name="area" type="number" min="0.0001" max="9999999999.9999" step="0.0001" defaultValue={item?.data.areaHectares ?? ''} /></Field>
    <Field label="Capacidad máxima (animales)" error={errors.capacity} hint="Límite operativo definido para esta finca. Sin valor, el sistema no puede bloquear por capacidad."><Input name="capacity" type="number" min="1" step="1" max="2147483647" defaultValue={item?.data.capacity ?? ''} /></Field>
    <label className="check-field"><Input name="active" type="checkbox" defaultChecked={item?.data.isActive ?? true} /> Potrero activo</label>
  </div></fieldset>{error && <p className="error-banner" role="alert">{error}</p>}<div className="button-row"><Button className="button primary" disabled={busy}>{busy ? 'Guardando…' : 'Guardar potrero'}</Button><Button type="button" className="button secondary" disabled={busy} onClick={async () => { if (!dirty || await confirm('¿Descartar los cambios del potrero?', { destructive: true })) { clearUnsaved(); onCancel(); } }}>Cancelar</Button></div></form></section>;
}
function PaddockResidents({ item, onMoved }: { item: PaddockSnapshot; onMoved: () => void }) {
  const { can } = useAuth();
  const [page, setPage] = useState(1), [moving, setMoving] = useState<Resident | null>(null), [lot, setLot] = useState('');
  const residents = useResource<CarePage<Resident>>('/api/paddocks/' + item.id + '/residents?page=' + page + '&pageSize=10' + (lot === 'unassigned' ? '&ungrouped=true' : lot ? '&lotId=' + lot : ''), can('paddocks.get') && can('animals.list'));
  if (!can('paddocks.get') || !can('animals.list')) return <p className="muted">Sin permiso para consultar la ocupación individual.</p>;
  return <section className="panel paddock-detail" aria-labelledby="residents-title"><div className="section-heading"><div><h2 id="residents-title">{item.data.name}: animales presentes</h2><p className="muted">La permanencia se cuenta desde la última entrada registrada al potrero.</p></div></div>
    {!moving && item.lots.length > 0 && <div className="button-row lot-map" aria-label="Lotes presentes"><Button className={"button " + (!lot ? "primary" : "secondary")} aria-pressed={!lot} onClick={() => { setLot(""); setPage(1); }}>Todos los lotes</Button>{item.lots.map(l => <Button className={"button " + (lot === (l.id || "unassigned") ? "primary" : "secondary")} key={l.id || "unassigned"} aria-pressed={lot === (l.id || "unassigned")} onClick={() => { setLot(l.id || "unassigned"); setPage(1); }}>{l.name || "Sin lote"} · {l.count}</Button>)}</div>}
    {moving && <MovementForm animal={{ id: moving.id, internalTag: moving.internalTag, farmId: item.data.farmId, speciesId: moving.speciesId, paddockId: item.id, lotId: moving.lotId }} onSaved={() => { setMoving(null); residents.reload(); onMoved(); }} />}
    {residents.loading ? <p role="status">Cargando ocupación…</p> : residents.error ? <div className="error-banner"><p role="alert">{residents.error}</p><Button className="button secondary" onClick={residents.reload}>Reintentar</Button></div> : residents.data?.items.length ? <><div className="table-scroll"><table aria-label="Animales presentes en el potrero"><thead><tr><th>Animal</th><th>Lote</th><th>Entrada registrada</th><th>Permanencia</th><th>Acciones</th></tr></thead><tbody>{residents.data.items.map(a => <tr key={a.id}><td>{a.name || a.internalTag}<small className="muted"> {a.internalTag}</small></td><td>{a.lot || 'Sin lote'}</td><td>{date(a.arrivalDate)}</td><td>{stayDays(a.arrivalDate) === null ? 'Sin registro' : stayDays(a.arrivalDate) + (stayDays(a.arrivalDate) === 1 ? ' día' : ' días')}</td><td><div className="button-row">{can('animals.get') && <Link className="button secondary" to={'/animals/' + a.id}>Ver ficha</Link>}{!moving && can('animals.update') && can('animals.get') && can('lots.list') && <Button className="button secondary" onClick={() => setMoving(a)}>Trasladar {a.internalTag}</Button>}</div></td></tr>)}</tbody></table></div><CarePagination {...residents.data} onPage={setPage} /></> : <p className="muted">Este potrero no tiene animales activos asignados.</p>}
  </section>;
}
export function PaddocksPage() {
  const { can } = useAuth();
  const [params, setParams] = useSearchParams();
  const [selected, setSelected] = useState(''), [editor, setEditor] = useState<PaddockSnapshot | 'new' | null>(null);
  const farms = useResource<CatalogItem[]>('/api/farms', can('farms.list'));
  const farm = params.get('farm') || '', search = params.get('search') || '', active = params.get('active') || 'true', page = params.get('page') || '1';
  const query = new URLSearchParams({ page, pageSize: '12', isActive: active, ...(farm ? { farmId: farm } : {}), ...(search ? { search } : {}) });
  const paddocks = useResource<CarePage<PaddockSnapshot>>('/api/paddocks/page?' + query, can('paddocks.list'));
  const chosen = paddocks.data?.items.find(p => p.id === selected);
  function filter(event: FormEvent<HTMLFormElement>) { event.preventDefault(); const form = new FormData(event.currentTarget); setSelected(''); setParams({ page: '1', farm: String(form.get('farm') || ''), search: String(form.get('search') || '').trim(), active: String(form.get('active') || 'true') }); }
  if (!can('paddocks.list')) return <div className="panel empty-state"><h1>Sin acceso a potreros</h1><p>Solicita el permiso de consulta al administrador.</p></div>;
  return <section><div className="page-heading"><div><span className="eyebrow">ESPACIO PARA CADA ANIMAL</span><h1>Potreros</h1><p className="muted">Explora la ocupación, los lotes presentes y la permanencia registrada.</p></div>{!editor && can('paddocks.create') && <Button className="button primary" disabled={!farms.data || !!farms.error} onClick={() => setEditor('new')}><Icon name="plus" size={18} />Registrar potrero</Button>}</div>
    {editor && <PaddockEditor key={editor === 'new' ? 'new' : editor.id} item={editor === 'new' ? undefined : editor} farms={farms.data || []} onSaved={() => { setEditor(null); setSelected(''); paddocks.reload(); }} onCancel={() => setEditor(null)} />}
    <form className="panel search-bar paddock-filters" onSubmit={filter} key={farm + search + active}><div className="search-field"><label htmlFor="paddock-search">Buscar potrero</label><Input id="paddock-search" name="search" maxLength={100} defaultValue={search} placeholder="Nombre o código" /></div><div><label htmlFor="paddock-farm">Finca</label><Select id="paddock-farm" name="farm" defaultValue={farm}><option value="">Todas las fincas asignadas</option>{farms.data?.map(f => <option value={f.id} key={f.id}>{f.data.name}</option>)}</Select></div><div><label htmlFor="paddock-active">Estado del potrero</label><Select id="paddock-active" name="active" defaultValue={active}><option value="true">Activos</option><option value="false">Inactivos</option></Select></div><Button className="button primary">Buscar</Button></form>
    {farms.error && <p className="error-banner" role="alert">{farms.error}</p>}
    <p className="muted map-legend"><span className="map-dot positive" /> Disponible <span className="map-dot warning" /> Desde 80 % <span className="map-dot critical" /> Completo o excedido <span className="map-dot neutral" /> Sin límite definido</p>
    {paddocks.loading ? <p role="status">Cargando mapa de potreros…</p> : paddocks.error ? <div className="panel empty-state"><p role="alert">{paddocks.error}</p><Button className="button secondary" onClick={paddocks.reload}>Reintentar</Button></div> : paddocks.data?.items.length ? <><div className="paddock-map" aria-label="Mapa de ocupación de potreros">{paddocks.data.items.map(p => { const state = occupancyState(p.occupancy, p.data.capacity), days = stayDays(p.oldestKnownArrival); return <article className={'panel paddock-card ' + state.tone + (selected === p.id ? ' selected' : '')} key={p.id}><Button className="paddock-select" aria-pressed={selected === p.id} onClick={() => setSelected(selected === p.id ? '' : p.id)}><PaddockVisual /><span className="paddock-card-body"><span className="paddock-card-title"><strong>{p.data.name}</strong><span className={'map-dot ' + state.tone} /></span><span className="muted">{p.farm}{p.data.code ? ' · ' + p.data.code : ''}</span><span className="paddock-occupancy"><strong>{number(p.occupancy, 0)}</strong><span> / {p.data.capacity ?? '—'} animales</span></span><span className={'status-pill ' + state.tone}>{state.label}</span>{p.data.capacity && <span className="occupancy-track"><span style={{ width: Math.min(100, p.occupancy / p.data.capacity * 100) + '%' }} /></span>}<span className="paddock-facts"><span>{p.data.areaHectares ? number(p.data.areaHectares, 4) + ' ha · ' + number(p.occupancy / p.data.areaHectares) + ' animales/ha' : 'Superficie sin registro'}</span><span>{days === null ? 'Permanencia sin registro' : 'Mayor permanencia registrada: ' + days + (days === 1 ? ' día' : ' días')}</span>{p.unknownArrivals > 0 && <span>{p.unknownArrivals} entradas sin fecha registrada</span>}</span><span className="paddock-open">{selected === p.id ? 'Cerrar detalle' : 'Explorar ocupación'} <Icon name="arrow" size={17} /></span></span></Button>{!editor && can('paddocks.update') && <Button className="button secondary paddock-edit" aria-label={"Editar " + p.data.name} onClick={() => setEditor(p)}><Icon name="edit" size={17} />Editar potrero</Button>}</article>; })}</div><CarePagination {...paddocks.data} onPage={p => { setSelected(''); setParams({ farm, search, active, page: String(p) }); }} />{chosen && <PaddockResidents key={chosen.id} item={chosen} onMoved={paddocks.reload} />}</> : <div className="panel empty-state"><h2>No hay potreros en esta consulta</h2><p className="muted">Prueba otro nombre, finca o estado.</p></div>}
    <p className="muted map-note">Mapa esquemático, sin coordenadas geográficas. La capacidad cuenta animales activos por ubicación física; los animales/ha son una densidad descriptiva.</p>
  </section>;
}
