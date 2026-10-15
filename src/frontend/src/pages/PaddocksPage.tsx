import { TableScroll } from '../components/TableScroll';
import { Button, Input, Select } from '../components/ui/Controls';
import { useEffect, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { useResource } from '../api/useResource';
import type { CatalogItem } from '../api/livestock';
import type { CarePage } from '../api/care';
import { date, number } from '../api/livestock';
import { useOperationalRefresh } from '../api/useOperationalRefresh';
import { PaddockPlan } from '../components/PaddockPlan';
import { PaddockEditor } from '../components/PaddockEditor';
import { PaddockCondition } from '../components/PaddockCondition';
import { occupancyState, stayDays, permanenceState } from '../api/paddocks';
import type { PaddockGeometry, PaddockSnapshot, Resident } from '../api/paddocks';
import { errorMessage } from '../api/errors';
import { CarePagination } from '../components/CareForm';
import { MovementForm } from '../components/AnimalLocation';
import type { SavedMovement } from '../components/AnimalLocation';
import type { ConfirmedMovement } from '../components/paddock-scene/model';
import { useUnsavedChanges } from '../components/NavigationProtection';
import { SidePanel } from '../components/SidePanel';
import { Icon } from '../components/ui/Icon';
import { useFeedback } from '../components/Feedback';

function PaddockResidents({ item, onMoved, onClose }: { item: PaddockSnapshot; onMoved: (movement?: SavedMovement) => void; onClose: () => void }) {
  const { can } = useAuth();
  const { confirm } = useFeedback();
  const [dirty, setDirty] = useState(false);
  const clearUnsaved = useUnsavedChanges(dirty);
  const content = useRef<HTMLDivElement>(null);
  async function closeDetail() {
    if (content.current?.querySelector('[aria-busy=true]')) return;
    if (dirty && !await confirm('¿Descartar los cambios del traslado y cerrar el detalle?', { destructive: true, confirmLabel: 'Descartar y cerrar' })) return;
    clearUnsaved(); onClose();
  }
  const [page, setPage] = useState(1), [moving, setMoving] = useState<Resident | null>(null), [lot, setLot] = useState('');
  const residents = useResource<CarePage<Resident>>('/api/paddocks/' + item.id + '/residents?page=' + page + '&pageSize=10' + (lot === 'unassigned' ? '&ungrouped=true' : lot ? '&lotId=' + lot : ''), can('paddocks.get') && can('animals.list'));
  if (!can('paddocks.get') || !can('animals.list')) return <SidePanel title={item.data.name} onRequestClose={onClose}><p className="muted">Sin permiso para consultar la ocupación individual.</p></SidePanel>;
  return <SidePanel title={item.data.name} onRequestClose={() => void closeDetail()}><div ref={content}><PaddockCondition item={item} />{can('animals.update') && can('animals.get') && can('lots.list') && <Link className="button primary" to={'/transfers?farm=' + item.data.farmId + '&source=' + item.id}>Trasladar desde este potrero</Link>}<section className="paddock-residents" aria-labelledby="residents-title"><div className="section-heading"><div><h3 id="residents-title">Animales presentes</h3><p className="muted">La permanencia se cuenta desde la última entrada registrada al potrero.</p></div></div>
    {!moving && item.lots.length > 0 && <div className="button-row lot-map" aria-label="Lotes presentes"><Button className={"button " + (!lot ? "primary" : "secondary")} aria-pressed={!lot} onClick={() => { setLot(""); setPage(1); }}>Todos los lotes</Button>{item.lots.map(l => <Button className={"button " + (lot === (l.id || "unassigned") ? "primary" : "secondary")} key={l.id || "unassigned"} aria-pressed={lot === (l.id || "unassigned")} onClick={() => { setLot(l.id || "unassigned"); setPage(1); }}>{l.name || "Sin lote"} · {l.count}</Button>)}</div>}
    {moving && <div onChangeCapture={() => setDirty(true)}><MovementForm animal={{ id: moving.id, internalTag: moving.internalTag, farmId: item.data.farmId, speciesId: moving.speciesId, paddockId: item.id, lotId: moving.lotId }} onSaved={movement => { clearUnsaved(); setDirty(false); setMoving(null); residents.reload(); onMoved(movement); }} /></div>}
    {residents.loading ? <p role="status">Cargando ocupación…</p> : residents.error ? <div className="error-banner"><p role="alert">{residents.error}</p><Button className="button secondary" onClick={residents.reload}>Reintentar</Button></div> : residents.data?.items.length ? <><TableScroll className="table-scroll"><table aria-label="Animales presentes en el potrero"><thead><tr><th>Animal</th><th>Lote</th><th>Entrada registrada</th><th>Permanencia</th><th>Acciones</th></tr></thead><tbody>{residents.data.items.map(a => <tr key={a.id}><td data-label="Animal">{a.name || a.internalTag}<small className="muted"> {a.internalTag}</small></td><td data-label="Lote">{a.lot || 'Sin lote'}</td><td data-label="Entrada registrada">{date(a.arrivalDate)}</td><td data-label="Permanencia">{stayDays(a.arrivalDate) === null ? 'Sin registro' : stayDays(a.arrivalDate) + (stayDays(a.arrivalDate) === 1 ? ' día' : ' días')}</td><td data-label="Acciones"><div className="button-row">{can('animals.get') && <Link className="button secondary" to={'/animals/' + a.id}>Ver ficha</Link>}{!moving && can('animals.update') && can('animals.get') && can('lots.list') && <Link className="button primary" to={'/transfers?farm=' + item.data.farmId + '&source=' + item.id + '&animal=' + a.id} aria-label={'Trasladar ' + a.internalTag + ' en el plano'}>Mover en el plano</Link>}{!moving && can('animals.update') && can('animals.get') && can('lots.list') && <Button className="button secondary" onClick={() => setMoving(a)}>Trasladar {a.internalTag}</Button>}</div></td></tr>)}</tbody></table></TableScroll><CarePagination {...residents.data} onPage={setPage} /></> : <p className="muted">Este potrero no tiene animales activos asignados.</p>}
  </section></div></SidePanel>;
}
export function PaddocksPage() {
  const { can, isAdmin, request } = useAuth();
  const { confirm, notify } = useFeedback();
  const [archiveBusy, setArchiveBusy] = useState('');
  const [archiveError, setArchiveError] = useState('');
  const [movement, setMovement] = useState<ConfirmedMovement>();
  const [params, setParams] = useSearchParams();
  const [selected, setSelected] = useState(''), [editor, setEditor] = useState<PaddockSnapshot | 'new' | null>(null);
  const farms = useResource<CatalogItem[]>('/api/farms', can('farms.list'));
  const farm = params.get('farm') || '', search = params.get('search') || '', active = params.get('active') || 'true', page = params.get('page') || '1';
  useEffect(() => {
    const activeFarms = farms.data?.filter(item => item.data.isActive);
    if (!farm && activeFarms?.length === 1) {
      const next = new URLSearchParams(params); next.set('farm', activeFarms[0].id);
      setParams(next, { replace: true });
    }
  }, [farm, farms.data, params, setParams]);
  const query = new URLSearchParams({ page, pageSize: '12', isActive: active, ...(farm ? { farmId: farm } : {}), ...(search ? { search } : {}) });
  const paddocks = useResource<CarePage<PaddockSnapshot>>('/api/paddocks/page?' + query, can('paddocks.list'), true);
  const plan = useResource<PaddockSnapshot[]>('/api/paddocks/destinations?farmId=' + farm, !!farm && can('paddocks.list'), true);
  const geometry = useResource<PaddockGeometry[]>('/api/paddocks', can('paddocks.list') && (!!farm || !!editor), true);
  const chosen = paddocks.data?.items.find(p => p.id === selected) || plan.data?.find(p => p.id === selected);
  function reloadPaddocks() { paddocks.reload(); plan.reload(); geometry.reload(); }
  function moved(saved?: SavedMovement) {
    if (saved) {
      setMovement({ id: crypto.randomUUID(), toId: saved.toPaddockId, groups: saved.fromPaddockId !== saved.toPaddockId ? [{ fromId: saved.fromPaddockId, count: 1 }] : [] });
      setSelected('');
    }
    reloadPaddocks();
  }
  useOperationalRefresh(reloadPaddocks, !!farm && !editor && !archiveBusy && !selected);
  async function archivePaddock(item: PaddockSnapshot) {
    if (archiveBusy || !await confirm('¿Archivar «' + item.data.name + '»? Sus datos se conservarán en Papelera. Resuelve primero los animales y lotes vinculados.', { destructive: true, confirmLabel: 'Archivar potrero' })) return;
    setArchiveBusy(item.id); setArchiveError('');
    try { await request('/api/paddocks/' + item.id, { method: 'DELETE' }); if (selected === item.id) setSelected(''); notify('Potrero archivado.'); reloadPaddocks(); }
    catch (failure) { setArchiveError(errorMessage(failure)); } finally { setArchiveBusy(''); }
  }
  function filter(event: FormEvent<HTMLFormElement>) { event.preventDefault(); const form = new FormData(event.currentTarget); setSelected(''); setMovement(undefined); setParams({ page: '1', farm: String(form.get('farm') || ''), search: String(form.get('search') || '').trim(), active: String(form.get('active') || 'true') }); }
  if (!can('paddocks.list')) return <div className="panel empty-state"><h1>Sin acceso a potreros</h1><p>Solicita el permiso de consulta al administrador.</p></div>;
  return <section><div className="page-heading"><div><span className="eyebrow">ESPACIO PARA CADA ANIMAL</span><h1>Potreros</h1><p className="muted">Explora la ocupación, los lotes presentes y la permanencia registrada.</p></div>{!editor && can('paddocks.create') && <Button className="button primary" disabled={!farms.data || !!farms.error} onClick={() => setEditor('new')}><Icon name="plus" size={18} />Registrar potrero</Button>}</div>
    {editor && <PaddockEditor key={editor === 'new' ? 'new' : editor.id} item={editor === 'new' ? undefined : editor} geometryResource={geometry} farms={farms.data || []} initialFarmId={farm} onSaved={() => { setEditor(null); setSelected(''); reloadPaddocks(); }} onCancel={() => setEditor(null)} />}
    <form className="panel search-bar paddock-filters" onSubmit={filter} key={farm + search + active}><div className="search-field"><label htmlFor="paddock-search">Buscar potrero</label><Input id="paddock-search" name="search" maxLength={100} defaultValue={search} placeholder="Nombre o código" /></div><div><label htmlFor="paddock-farm">Finca</label><Select id="paddock-farm" name="farm" key={farms.data?.map(item => item.id).join(',') || 'loading'} defaultValue={farm}><option value="">Todas las fincas asignadas</option>{farms.data?.map(f => <option value={f.id} key={f.id}>{f.data.name}</option>)}</Select></div><div><label htmlFor="paddock-active">Estado del potrero</label><Select id="paddock-active" name="active" defaultValue={active}><option value="true">Activos</option><option value="false">Inactivos</option></Select></div><Button className="button primary">Buscar</Button></form>
    {farms.error && <p className="error-banner" role="alert">{farms.error}</p>}
    {archiveError && <p className="error-banner" role="alert">{archiveError}</p>}
    {farm ? (plan.loading && !plan.data || geometry.loading && !geometry.data) ? <p role="status">Cargando plano…</p> : plan.error || geometry.error ? <p role="alert">{plan.error || geometry.error}</p> : plan.data && geometry.data && <PaddockPlan key={farm} items={plan.data.filter(item => item.data.farmId === farm)} geometryItems={geometry.data.filter(item => item.data.farmId === farm)} selected={selected} onSelect={id => setSelected(selected === id ? '' : id)} movement={movement}/> : <p className="muted">Selecciona una finca y aplica el filtro para ver su plano espacial.</p>}
    <p className="muted map-legend"><span className="map-dot positive" /> Disponible <span className="map-dot warning" /> Desde 80 % <span className="map-dot critical" /> Completo o excedido <span className="map-dot neutral" /> Sin límite definido</p>
    {paddocks.loading && !paddocks.data ? <p role="status">Cargando potreros…</p> : paddocks.error ? <div className="panel empty-state"><p role="alert">{paddocks.error}</p><Button className="button secondary" onClick={paddocks.reload}>Reintentar</Button></div> : paddocks.data?.items.length ? <><div className="paddock-map" aria-label="Mapa de ocupación de potreros">{paddocks.data.items.map(p => { const state = occupancyState(p.occupancy, p.data.capacity), days = stayDays(p.oldestKnownArrival), permanence = permanenceState(p.oldestKnownArrival, p.data.maxStayDays); return <article className={'panel paddock-card ' + state.tone + (selected === p.id ? ' selected' : '')} key={p.id}><Button className="paddock-select" aria-haspopup="dialog" aria-expanded={selected === p.id} onClick={() => setSelected(selected === p.id ? '' : p.id)}><span className="paddock-card-body"><span className="paddock-card-title"><strong>{p.data.name}</strong><span className={'map-dot ' + state.tone} /></span><span className="muted">{p.farm}{p.data.code ? ' · ' + p.data.code : ''}</span><span className="paddock-occupancy"><strong>{number(p.occupancy, 0)}</strong><span> / {p.data.capacity ?? '—'} animales</span></span><span className={'status-pill ' + state.tone}>{state.label}</span>{p.data.capacity && <span className="occupancy-track"><span style={{ width: Math.min(100, p.occupancy / p.data.capacity * 100) + '%' }} /></span>}<span className="paddock-facts"><span>{p.data.areaHectares ? number(p.data.areaHectares, 4) + ' ha · ' + number(p.occupancy / p.data.areaHectares) + ' animales/ha' : 'Superficie sin registro'}</span><span>{days === null ? 'Permanencia sin registro' : 'Mayor permanencia registrada: ' + days + (days === 1 ? ' día' : ' días')}</span>{p.data.maxStayDays && <span className={'status-pill ' + permanence.tone}>{permanence.label}</span>}{p.unknownArrivals > 0 && <span>{p.unknownArrivals} entradas sin fecha registrada</span>}</span><span className="paddock-open">{selected === p.id ? 'Detalle abierto' : 'Explorar ocupación'} <Icon name="arrow" size={17} /></span></span></Button><PaddockCondition item={p} compact /><div className="paddock-card-actions">{!editor && can('paddocks.update') && <Button className="button secondary paddock-edit" aria-label={"Editar " + p.data.name} onClick={() => setEditor(p)}><Icon name="edit" size={17} />Editar potrero</Button>}{!editor && isAdmin && can("paddocks.delete") && <Button variant="quiet" disabled={!!archiveBusy} aria-label={"Archivar " + p.data.name} onClick={() => void archivePaddock(p)}><Icon name="archive" size={17} />{archiveBusy === p.id ? "Archivando…" : "Archivar"}</Button>}</div></article>; })}</div><CarePagination {...paddocks.data} onPage={p => { setSelected(''); setParams({ farm, search, active, page: String(p) }); }} /></> : <div className="panel empty-state"><h2>No hay potreros en esta consulta</h2><p className="muted">Prueba otro nombre, finca o estado.</p></div>}
    {chosen && <PaddockResidents key={chosen.id} item={chosen} onMoved={moved} onClose={() => setSelected('')} />}
    <p className="muted map-note">Ocupación registrada, sin coordenadas geográficas. La capacidad cuenta animales activos por ubicación física; los animales/ha son una densidad descriptiva.</p>
  </section>;
}
