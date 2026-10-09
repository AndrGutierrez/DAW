import { useEffect, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { useResource } from '../api/useResource';
import type { Animal, AnimalPageResult, CatalogItem } from '../api/livestock';
import type { PaddockSnapshot } from '../api/paddocks';
import { date, today } from '../api/livestock';
import { Button, Input, Select, Textarea } from '../components/ui/Controls';
import { Icon } from '../components/ui/Icon';
import { Field } from '../components/Field';
import { useFeedback } from '../components/Feedback';
import { useSearchSelection } from '../components/useSearchSelection';
import { CareFormResult, useCareSubmission } from '../components/CareForm';
import { useUnsavedChanges } from '../components/NavigationProtection';

export function TransfersPage() {
  const { can } = useAuth();
  const { confirm } = useFeedback();
  const allowed = ['animals.list', 'animals.get', 'animals.update', 'farms.list', 'paddocks.list', 'lots.list'].every(can);
  const [farmId, setFarmId] = useState('');
  const [lotId, setLotId] = useState('');
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [page, setPage] = useState(1);
  const [selected, setSelected] = useState<Animal[]>([]);
  const [destination, setDestination] = useState('');
  const [destinationLot, setDestinationLot] = useState('keep');
  const [reason, setReason] = useState('');
  const [completed, setCompleted] = useState<Animal[]>([]);
  const pendingAnimals = useRef<Animal[]>([]);
  const clearUnsaved = useUnsavedChanges(allowed && (selected.length > 0 || !!reason));
  const farms = useResource<CatalogItem[]>('/api/farms', allowed);
  const lots = useResource<CatalogItem[]>('/api/lots', allowed);
  const paddocks = useResource<PaddockSnapshot[]>('/api/paddocks/destinations?farmId=' + farmId, allowed && !!farmId);
  const filters = new URLSearchParams({ farmId, status: 'Active', ...(lotId ? { lotId } : {}), ...(query ? { search: query } : {}) }).toString();
  const animals = useResource<AnimalPageResult>('/api/animals/page?' + filters + '&page=' + page + '&pageSize=12', allowed && !!farmId);
  const selection = useSearchSelection(filters, setSelected);
  const state = useCareSubmission('/api/animal-movements/batch', () => {
    setCompleted(pendingAnimals.current); setSelected([]); setReason(''); clearUnsaved(); animals.reload(); paddocks.reload();
  });
  useEffect(() => {
    const active = farms.data?.filter(item => item.data.isActive);
    if (!farmId && active?.length === 1) setFarmId(active[0].id);
  }, [farmId, farms.data]);
  const target = paddocks.data?.find(item => item.id === destination);
  const eligible = selected.filter(animal => animal.paddockId !== destination ||
    destinationLot !== 'keep' && animal.lotId !== (destinationLot === 'none' ? null : destinationLot));
  const incoming = eligible.filter(animal => animal.paddockId !== destination).length;
  const projected = target ? target.occupancy + incoming : null;
  const insufficient = target?.data.capacity !== null && target?.data.capacity !== undefined && projected !== null && projected > target.data.capacity;
  const catalogError = farms.error || lots.error || paddocks.error || animals.error;
  const locked = state.busy || state.uncertain;
  useEffect(() => {
    if (!locked && destinationLot !== 'keep' && destinationLot !== 'none' && lots.data &&
      !lots.data.some(item => item.id === destinationLot && item.data.farmId === farmId && selected.every(animal => animal.speciesId === item.data.speciesId))) setDestinationLot('keep');
  }, [selected, destinationLot, farmId, lots.data, locked]);
  function choose(animal: Animal) {
    setSelected(items => items.some(item => item.id === animal.id) ? items.filter(item => item.id !== animal.id) : [...items, animal]);
  }
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!target || !eligible.length || eligible.length > 500 || selection.busy || insufficient || state.busy) return;
    const payload = { farmId, toPaddockId: destination, changeLot: destinationLot !== 'keep',
      toLotId: destinationLot === 'keep' || destinationLot === 'none' ? null : destinationLot,
      reason: reason.trim(), animals: eligible.map(animal => ({ animalId: animal.id, submissionId: crypto.randomUUID(),
        expectedFromPaddockId: animal.paddockId ?? null, expectedFromLotId: animal.lotId ?? null })) };
    if (!state.uncertain && !await confirm('¿Trasladar ' + eligible.length + ' animales a ' + target.data.name + '? Se guardará un registro por animal.', { confirmLabel: 'Confirmar traslado' })) return;
    pendingAnimals.current = [...eligible];
    await state.submit(event, payload);
  }
  if (!allowed) return <div className="panel empty-state"><h1>Sin acceso a los traslados</h1><p>Necesitas permisos para consultar animales, fincas, lotes y potreros, y actualizar animales.</p><Link to="/animals">Volver a los animales</Link></div>;
  return <section className="transfers-workspace"><div className="page-heading"><div><span className="eyebrow">ROTACIÓN DEL GANADO</span><h1>Traslados de potrero</h1><p className="muted">Selecciona animales o un lote completo y define su destino.</p></div><Link className="button secondary" to="/paddocks"><Icon name="paddock" size={18} />Ver potreros</Link></div>
    {completed.length > 0 && <div className="panel transfer-completed" role="status"><Icon name="check" /><div><h2>{completed.length} animales trasladados</h2><p className="muted">Confirmado el {date(today())}. Consulta cada traslado en la ficha del animal.</p><div className="transfer-record-links">{completed.map(animal => <Link key={animal.id} to={'/animals/' + animal.id + '?tab=location'}>{animal.internalTag}</Link>)}</div></div></div>}
    <fieldset disabled={locked} className="transfer-selection"><div className="panel form-section"><div className="form-grid"><Field label="Finca"><Select value={farmId} onChange={event => { setFarmId(event.target.value); setLotId(''); setSelected([]); setDestination(''); setDestinationLot('keep'); setCompleted([]); setPage(1); }}><option value="">Selecciona una finca</option>{farms.data?.filter(item => item.data.isActive).map(item => <option key={item.id} value={item.id}>{item.data.name}</option>)}</Select></Field><Field label="Lote de origen" hint="Elige un lote y selecciona todos los resultados para mover el grupo completo."><Select value={lotId} disabled={!farmId} onChange={event => { setLotId(event.target.value); setPage(1); }}><option value="">Todos los lotes</option>{lots.data?.filter(item => item.data.farmId === farmId && item.data.isActive).map(item => <option key={item.id} value={item.id}>{item.data.name}</option>)}</Select></Field></div><form className="compact-search" onSubmit={event => { event.preventDefault(); setQuery(search.trim()); setPage(1); }}><Input aria-label="Buscar animales para trasladar" value={search} maxLength={100} placeholder="Arete, nombre o identificación" onChange={event => setSearch(event.target.value)} /><Button type="submit" variant="secondary" disabled={!farmId}>Buscar</Button></form></div>
      {catalogError && <div className="error-banner" role="alert"><p>{catalogError}</p><Button type="button" variant="secondary" onClick={() => { farms.reload(); lots.reload(); paddocks.reload(); animals.reload(); }}>Reintentar</Button></div>}
      {!farmId && <div className="panel empty-state"><h2>Comienza por la finca</h2><p className="muted">Los traslados se realizan entre potreros de la misma finca.</p></div>}
      {animals.loading && farmId && <p role="status">Cargando animales activos…</p>}
      {animals.data && <><div className="selection-toolbar"><strong>{selected.length} seleccionados</strong><div className="button-row"><Button type="button" variant="secondary" disabled={selection.busy || animals.loading || !animals.data.total} onClick={() => void selection.selectAll()}>{selection.busy ? 'Seleccionando…' : 'Seleccionar todos los resultados (' + animals.data.total + ')'}</Button><Button type="button" variant="quiet" disabled={selection.busy || !selected.length} onClick={() => setSelected([])}>Limpiar selección</Button></div></div><div className="selection-grid">{animals.data.items.map(animal => <label className={'panel animal-choice ' + (selected.some(item => item.id === animal.id) ? 'selected' : '')} key={animal.id}><Input type="checkbox" aria-label={'Seleccionar ' + animal.internalTag} checked={selected.some(item => item.id === animal.id)} disabled={selection.busy} onChange={() => choose(animal)} /><span><strong>{animal.internalTag}</strong><small>{animal.name || animal.species}</small><small>{paddocks.data?.find(item => item.id === animal.paddockId)?.data.name || 'Sin potrero'} · {animal.lot || 'Sin lote'}</small></span></label>)}</div>{!animals.data.items.length && <p className="panel empty-state">No hay animales activos en esta búsqueda.</p>}<div className="pagination"><p className="muted">{animals.data.total} animales · página {page}</p><div><Button type="button" variant="secondary" disabled={page <= 1 || selection.busy} onClick={() => setPage(page - 1)}>Anterior</Button><Button type="button" variant="secondary" disabled={page * 12 >= animals.data.total || selection.busy} onClick={() => setPage(page + 1)}>Siguiente</Button></div></div></>}
      {selection.busy && <p role="status">Seleccionando todas las páginas de esta búsqueda…</p>}{selection.error && <p className="error-banner" role="alert">{selection.error}</p>}
    </fieldset>
    {selected.length > 0 && <form className="panel transfer-destination" onSubmit={event => void submit(event)} aria-busy={state.busy}><div className="section-heading"><div><span className="eyebrow">DESTINO DEL GRUPO</span><h2>Trasladar {selected.length} animales</h2></div><span className="tag">Fecha: {date(today())}</span></div><fieldset disabled={locked || selection.busy || paddocks.loading || lots.loading || !!catalogError}><div className="form-grid"><Field label="Potrero de destino *" error={state.errors.topaddockid}><Select required value={destination} onChange={event => setDestination(event.target.value)}><option value="">Selecciona un potrero</option>{paddocks.data?.filter(item => item.data.isActive).map(item => <option value={item.id} key={item.id}>{item.data.name}</option>)}</Select></Field><Field label="Lote de destino" error={state.errors.tolotid} hint="Mover de potrero conserva el lote de cada animal por defecto."><Select value={destinationLot} onChange={event => setDestinationLot(event.target.value)}><option value="keep">Conservar lote actual</option><option value="none">Sin lote asignado</option>{lots.data?.filter(item => item.data.isActive && item.data.farmId === farmId && selected.every(animal => animal.speciesId === item.data.speciesId)).map(item => <option value={item.id} key={item.id}>{item.data.name}</option>)}</Select></Field></div><Field label="Motivo del traslado *" error={state.errors.reason}><Textarea required maxLength={300} rows={2} value={reason} onChange={event => setReason(event.target.value)} placeholder="Rotación de pastoreo, descanso del potrero…" /></Field></fieldset>
      {target && <div className={'transfer-capacity ' + (insufficient ? 'warning-banner' : '')}><strong>{target.data.name}</strong><span>{target.occupancy} presentes + {incoming} entradas = {projected} animales{target.data.capacity !== null ? ' / ' + target.data.capacity + ' de capacidad' : ' · sin límite definido'}</span></div>}
      {destination && selected.length !== eligible.length && <p className="muted">{selected.length - eligible.length} animales ya tienen este destino y lote; se excluyen del traslado.</p>}
      {insufficient && <p role="alert">El potrero no tiene capacidad para todo el grupo. Elige otro destino o reduce la selección.</p>}
      {eligible.length > 500 && <p className="warning-banner" role="alert">El máximo es 500 animales por traslado. Divide la selección antes de confirmar.</p>}
      <p className="muted">El grupo se guarda completo o no se realiza ningún traslado. Cada animal conserva su historial.</p><div className="transfer-submit"><CareFormResult state={state} disabled={selection.busy || !destination || !eligible.length || eligible.length > 500 || insufficient || paddocks.loading || lots.loading || !!catalogError} label={'Trasladar ' + eligible.length + ' animales'} /></div>
      {state.error && !locked && <Button type="button" variant="secondary" onClick={() => { setSelected([]); animals.reload(); paddocks.reload(); }}>Actualizar y volver a seleccionar</Button>}
    </form>}
  </section>;
}
