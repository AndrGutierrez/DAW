import { useEffect, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { useResource } from '../api/useResource';
import type { Animal, AnimalPageResult, CatalogItem } from '../api/livestock';
import type { PaddockGeometry, PaddockSnapshot } from '../api/paddocks';
import { date, today } from '../api/livestock';
import { Button, Input, Select, Textarea } from '../components/ui/Controls';
import { Icon } from '../components/ui/Icon';
import { Field } from '../components/Field';
import { useSearchSelection } from '../components/useSearchSelection';
import { useCareSubmission } from '../components/CareForm';
import { useUnsavedChanges } from '../components/NavigationProtection';
import { PaddockPlan } from '../components/PaddockPlan';
import { movementGroups } from '../components/paddock-scene/model';
import type { ConfirmedMovement } from '../components/paddock-scene/model';
import { useOperationalRefresh } from '../api/useOperationalRefresh';

type Step = 1 | 2 | 3 | 4;
const steps = ['Origen', 'Animales', 'Destino', 'Revisión'];
const animalCount = (count: number) => count + (count === 1 ? ' animal' : ' animales');

export function TransfersPage() {
  const { can } = useAuth();
  const allowed = ['animals.list', 'animals.get', 'animals.update', 'farms.list', 'paddocks.list', 'lots.list'].every(can);
  const [params] = useSearchParams();
  const [farmId, setFarmId] = useState(params.get('farm') || '');
  const [sourceId, setSourceId] = useState(params.get('source') || '');
  const [originChosen, setOriginChosen] = useState(!!params.get('source'));
  const [step, setStep] = useState<Step>(params.get('source') ? 2 : 1);
  const appliedAnimal = useRef(false);
  const panel = useRef<HTMLElement>(null), workspace = useRef<HTMLDivElement>(null);
  const previousStep = useRef(step);
  const [lotId, setLotId] = useState('');
  const [search, setSearch] = useState(''), [query, setQuery] = useState('');
  const [page, setPage] = useState(1);
  const [selected, setSelected] = useState<Animal[]>([]);
  const [destination, setDestination] = useState(''), [destinationLot, setDestinationLot] = useState('keep');
  const [reason, setReason] = useState('');
  const [completed, setCompleted] = useState<Animal[]>([]);
  const pendingAnimals = useRef<Animal[]>([]), pendingDestination = useRef(''), pendingTargetName = useRef('');
  const [movement, setMovement] = useState<ConfirmedMovement>();
  const clearUnsaved = useUnsavedChanges(allowed && (selected.length > 0 || !!reason));
  const farms = useResource<CatalogItem[]>('/api/farms', allowed);
  const linkedAnimal = useResource<Animal>('/api/animals/' + params.get('animal'), allowed && !!params.get('animal'));
  const lots = useResource<CatalogItem[]>('/api/lots', allowed);
  const paddocks = useResource<PaddockSnapshot[]>('/api/paddocks/destinations?farmId=' + farmId, allowed && !!farmId, true);
  const geometry = useResource<PaddockGeometry[]>('/api/paddocks', allowed && !!farmId, true);
  const filters = new URLSearchParams({ farmId, status: 'Active', ...(sourceId ? { paddockId: sourceId } : {}), ...(lotId ? { lotId } : {}), ...(query ? { search: query } : {}) }).toString();
  const animals = useResource<AnimalPageResult>('/api/animals/page?' + filters + '&page=' + page + '&pageSize=12', allowed && !!farmId);
  const selection = useSearchSelection(filters, setSelected);
  const state = useCareSubmission('/api/animal-movements/batch', () => {
    setMovement({ id: crypto.randomUUID(), toId: pendingDestination.current, groups: movementGroups(pendingAnimals.current, pendingDestination.current) });
    setCompleted(pendingAnimals.current); setSelected([]); setReason(''); clearUnsaved(); animals.reload(); paddocks.reload();
  });
  useEffect(() => {
    const active = farms.data?.filter(item => item.data.isActive);
    if (!farmId && active?.length === 1) setFarmId(active[0].id);
  }, [farmId, farms.data]);
  useEffect(() => {
    const animal = linkedAnimal.data;
    if (animal && !appliedAnimal.current) {
      appliedAnimal.current = true;
      if (animal.farmId === farmId && animal.status === 'Active') { setSelected([animal]); setOriginChosen(true); setStep(3); }
    }
  }, [linkedAnimal.data, farmId]);
  useEffect(() => {
    if (previousStep.current === step) return;
    previousStep.current = step;
    panel.current?.focus({ preventScroll: true });
    workspace.current?.scrollIntoView?.({ behavior: window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth', block: 'start' });
  }, [step]);
  const farmPaddocks = paddocks.data?.filter(item => item.data.farmId === farmId);
  const source = farmPaddocks?.find(item => item.id === sourceId), target = farmPaddocks?.find(item => item.id === destination);
  const eligible = selected.filter(animal => animal.paddockId !== destination || destinationLot !== 'keep' && animal.lotId !== (destinationLot === 'none' ? null : destinationLot));
  const incoming = eligible.filter(animal => animal.paddockId !== destination).length;
  const projected = target ? target.occupancy + incoming : null;
  const insufficient = target?.data.capacity != null && projected !== null && projected > target.data.capacity;
  const catalogError = farms.error || lots.error || paddocks.error || geometry.error || animals.error;
  const locked = state.busy || state.uncertain;
  const finished = completed.length > 0;
  const readyOrigin = !!farmId && originChosen && (!sourceId || !!source) && !catalogError && !paddocks.loading && !geometry.loading;
  const readyAnimals = readyOrigin && selected.length > 0 && selected.length <= 500 && !selection.busy;
  const readyReview = readyAnimals && !!target && !!eligible.length && !insufficient && !!reason.trim() && !lots.loading;
  const currentFarm = farms.data?.find(item => item.id === farmId)?.data.name || 'Selecciona una finca';
  const sources = [...new Set(selected.map(animal => animal.paddockId))].map(id => farmPaddocks?.find(item => item.id === id)?.data.name || 'Sin potrero');
  useOperationalRefresh(() => { paddocks.reload(); geometry.reload(); }, !!farmId && !locked);
  useEffect(() => {
    if (!locked && destinationLot !== 'keep' && destinationLot !== 'none' && lots.data &&
      !lots.data.some(item => item.id === destinationLot && item.data.farmId === farmId && selected.every(animal => animal.speciesId === item.data.speciesId))) setDestinationLot('keep');
  }, [selected, destinationLot, farmId, lots.data, locked]);
  function go(next: Step) {
    if (locked || selection.busy || finished || next === 2 && !readyOrigin || next === 3 && !readyAnimals || next === 4 && !readyReview) return;
    setStep(next);
  }
  function chooseSource(id: string) {
    if (locked || selection.busy) return;
    if (id !== sourceId) { setSelected([]); setDestination(''); setDestinationLot('keep'); setLotId(''); setSearch(''); setQuery(''); }
    setSourceId(id); setOriginChosen(true); setPage(1);
  }
  function choose(animal: Animal) {
    setSelected(items => items.some(item => item.id === animal.id) ? items.filter(item => item.id !== animal.id) : [...items, animal]);
  }
  function restart() {
    setStep(1); setCompleted([]); setSelected([]); setSourceId(''); setOriginChosen(false); setDestination(''); setDestinationLot('keep');
    setReason(''); setLotId(''); setSearch(''); setQuery(''); setPage(1); setMovement(undefined);
  }
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (state.busy) return;
    if (state.uncertain) { await state.submit(event, {}); return; }
    if (!readyReview || step !== 4 || !target) return;
    pendingAnimals.current = [...eligible]; pendingDestination.current = destination; pendingTargetName.current = target.data.name;
    await state.submit(event, { farmId, toPaddockId: destination, changeLot: destinationLot !== 'keep',
      toLotId: destinationLot === 'keep' || destinationLot === 'none' ? null : destinationLot, reason: reason.trim(),
      animals: eligible.map(animal => ({ animalId: animal.id, submissionId: crypto.randomUUID(), expectedFromPaddockId: animal.paddockId ?? null, expectedFromLotId: animal.lotId ?? null })) });
  }
  if (!allowed) return <div className="panel empty-state"><h1>Sin acceso a los traslados</h1><p>Necesitas permisos para consultar animales, fincas, lotes y potreros, y actualizar animales.</p><Link to="/animals">Volver a los animales</Link></div>;
  return <section className="transfers-workspace"><div className="page-heading"><div><span className="eyebrow">ROTACIÓN DEL GANADO</span><h1>Traslados de potrero</h1><p className="muted">Un paso a la vez: prepara el grupo y revisa su destino antes de moverlo.</p></div><Link className="button secondary" to="/paddocks"><Icon name="paddock" size={18} />Ver potreros</Link></div>
    <div className="transfer-wizard" ref={workspace} data-step={step} data-completed={finished}>
      <nav className="transfer-progress" aria-label="Pasos del traslado">{steps.map((name, index) => {
        const number = index + 1 as Step, available = number === 1 || number === 2 && readyOrigin || number === 3 && readyAnimals || number === 4 && readyReview;
        return <Button type="button" key={name} className={'transfer-progress-step' + (step === number ? ' current' : '')} aria-current={step === number ? 'step' : undefined} disabled={locked || selection.busy || finished || !available} onClick={() => go(number)}><span aria-hidden="true">{step > number || finished ? '✓' : number}</span><strong>{name}</strong></Button>;
      })}</nav>
      {catalogError && <div className="error-banner" role="alert"><p>{catalogError}</p><Button type="button" variant="secondary" onClick={() => { farms.reload(); lots.reload(); paddocks.reload(); geometry.reload(); animals.reload(); }}>Reintentar</Button></div>}
      {linkedAnimal.error && <p className="error-banner" role="alert">{linkedAnimal.error}</p>}
      <div className="transfer-wizard-layout">
        <div className="transfer-wizard-map">
          {farmId && paddocks.data && geometry.data ? <PaddockPlan key={farmId} items={farmPaddocks || []} geometryItems={geometry.data.filter(item => item.data.farmId === farmId)} compact selected={step >= 3 ? destination : sourceId}
            heading={finished ? 'Traslado confirmado' : step >= 3 ? 'Destino del traslado' : 'Origen del traslado'} onSelect={id => { if (step === 1) chooseSource(id); else if (step === 3 && !locked) setDestination(id); }}
            destinationMode={step >= 3} disabled={locked || selection.busy || finished || step !== 1 && step !== 3} selectedAnimals={selected} movement={movement} />
            : <div className="panel transfer-map-empty"><Icon name="paddock" size={36} /><h2>Tu finca en el plano</h2><p className="muted">{farmId ? 'Preparando los potreros…' : 'Elige una finca para comenzar.'}</p></div>}
        </div>
        <section className="panel transfer-step-panel" aria-labelledby="transfer-step-title" ref={panel} tabIndex={-1}>
          <header className="transfer-panel-heading"><span className="eyebrow">{finished ? 'OPERACIÓN COMPLETADA' : 'PASO ' + step + ' DE 4'}</span><h2 id="transfer-step-title">{finished ? animalCount(completed.length) + (completed.length === 1 ? ' trasladado' : ' trasladados') : ['Elige el origen', 'Reúne los animales', 'Elige el destino', 'Trasladar ' + animalCount(eligible.length)][step - 1]}</h2><p className="muted">{finished ? 'La ubicación y el historial quedaron guardados.' : ['Marca un potrero en el plano o elígelo aquí.', 'Selecciona el grupo. Nada se moverá todavía.', 'Marca el destino y comprueba el espacio disponible.', 'Revisa los datos. Al confirmar se guardará todo el grupo.'][step - 1]}</p></header>
          {finished ? <div className="transfer-success" role="status"><span className="transfer-success-icon"><Icon name="check" size={28} /></span><strong>{pendingTargetName.current}</strong><p className="muted">{date(today())} · {currentFarm}</p><div className="transfer-record-links">{completed.map(animal => <Link key={animal.id} to={'/animals/' + animal.id + '?tab=location'}>{animal.internalTag}</Link>)}</div><Button type="button" variant="primary" onClick={restart}>Nuevo traslado</Button></div> : <>
            {step === 1 && <form onSubmit={event => { event.preventDefault(); go(2); }} className="transfer-step-content"><fieldset disabled={locked || selection.busy}><Field label="Finca"><Select required value={farmId} onChange={event => { setFarmId(event.target.value); setSourceId(''); setOriginChosen(false); setSelected([]); setDestination(''); setDestinationLot('keep'); setReason(''); setLotId(''); setSearch(''); setQuery(''); setPage(1); setMovement(undefined); }}><option value="">Selecciona una finca</option>{farms.data?.filter(item => item.data.isActive).map(item => <option key={item.id} value={item.id}>{item.data.name}</option>)}</Select></Field><Field label="Potrero de origen"><Select value={originChosen ? sourceId || 'all' : ''} disabled={!farmId || paddocks.loading} onChange={event => { if (event.target.value) chooseSource(event.target.value === 'all' ? '' : event.target.value); }}><option value="">Selecciona el origen</option><option value="all">Toda la finca · varios potreros</option>{farmPaddocks?.map(item => <option key={item.id} value={item.id}>{item.data.name} · {item.occupancy} animales</option>)}</Select></Field></fieldset>
              {originChosen && <div className="transfer-origin-summary"><Icon name="paddock" /><div><strong>{source?.data.name || 'Toda la finca'}</strong><p>{source ? animalCount(source.occupancy) + ' presentes' : 'Podrás reunir animales de varios potreros y lotes.'}</p></div></div>}
              {selected.length > 0 && <p className="muted">Cambiar el origen limpia la selección de animales.</p>}
              <div className="transfer-step-actions"><Button type="submit" variant="primary" disabled={!readyOrigin || selection.busy}>Elegir animales <span aria-hidden="true">→</span></Button></div>
            </form>}
            {step === 2 && <div className="transfer-step-content"><div className="transfer-context"><span>{source?.data.name || 'Toda la finca'}</span><Button type="button" variant="quiet" onClick={() => go(1)}>Cambiar origen</Button></div>
              <Field label="Lote de origen"><Select disabled={selection.busy} value={lotId} onChange={event => { setLotId(event.target.value); setPage(1); }}><option value="">Todos los lotes</option>{lots.data?.filter(item => item.data.farmId === farmId && item.data.isActive).map(item => <option key={item.id} value={item.id}>{item.data.name}</option>)}</Select></Field>
              <form className="compact-search" onSubmit={event => { event.preventDefault(); setQuery(search.trim()); setPage(1); }}><Input aria-label="Buscar animales para trasladar" value={search} maxLength={100} placeholder="Arete o nombre" onChange={event => setSearch(event.target.value)} /><Button type="submit" variant="secondary" disabled={selection.busy}>Buscar</Button></form>
              <div className="transfer-selection-tools"><strong>{animalCount(selected.length)} {selected.length === 1 ? 'seleccionado' : 'seleccionados'}</strong><Button type="button" variant="quiet" disabled={selection.busy || animals.loading || !animals.data?.total} aria-label={'Seleccionar todos los resultados (' + (animals.data?.total || 0) + ')'} onClick={() => void selection.selectAll()}>{selection.busy ? 'Seleccionando…' : 'Seleccionar resultados (' + (animals.data?.total || 0) + ')'}</Button></div>
              {animals.loading && <p role="status">Cargando animales activos…</p>}
              <div className="transfer-animal-list" aria-label="Animales disponibles">{animals.data?.items.map(animal => <label className={'transfer-animal-row' + (selected.some(item => item.id === animal.id) ? ' selected' : '')} key={animal.id}><Input type="checkbox" aria-label={'Seleccionar ' + animal.internalTag} checked={selected.some(item => item.id === animal.id)} disabled={selection.busy} onChange={() => choose(animal)} /><span><strong>{animal.internalTag}</strong><small>{animal.name || animal.species} · {animal.lot || 'Sin lote'}</small></span></label>)}{!animals.loading && !animals.data?.items.length && <p className="muted">No hay animales activos en esta búsqueda.</p>}</div>
              {animals.data && <div className="transfer-list-pagination"><span>{animals.data.total} resultados · página {page}</span><Button type="button" variant="quiet" aria-label="Página anterior de animales" disabled={page <= 1 || selection.busy} onClick={() => setPage(page - 1)}>←</Button><Button type="button" variant="quiet" aria-label="Página siguiente de animales" disabled={page * 12 >= animals.data.total || selection.busy} onClick={() => setPage(page + 1)}>→</Button></div>}
              {selected.length > 0 && <details className="transfer-selected-list"><summary>Ver selección ({selected.length})</summary><div>{selected.map(animal => <Button type="button" key={animal.id} disabled={selection.busy} aria-label={'Quitar ' + animal.internalTag} onClick={() => choose(animal)}>{animal.internalTag} ×</Button>)}</div><Button type="button" variant="quiet" disabled={selection.busy} onClick={() => setSelected([])}>Limpiar selección</Button></details>}
              {selected.length > 500 && <p className="warning-banner" role="alert">El máximo es 500 animales por traslado. Reduce la selección.</p>}{selection.error && <p className="error-banner" role="alert">{selection.error}</p>}
              <div className="transfer-step-actions"><Button type="button" variant="secondary" disabled={selection.busy} onClick={() => go(1)}>Atrás</Button><Button type="button" variant="primary" disabled={!readyAnimals} onClick={() => go(3)}>Elegir destino <span aria-hidden="true">→</span></Button></div>
            </div>}
            {step === 3 && <form className="transfer-step-content" onSubmit={event => { event.preventDefault(); go(4); }}><div className="transfer-context"><span>{animalCount(selected.length)} {selected.length === 1 ? 'seleccionado' : 'seleccionados'}</span><Button type="button" variant="quiet" onClick={() => go(2)}>Cambiar grupo</Button></div><fieldset disabled={locked || selection.busy || paddocks.loading || lots.loading || !!catalogError}>
              <Field label="Potrero de destino *" error={state.errors.topaddockid}><Select required value={destination} onChange={event => setDestination(event.target.value)}><option value="">Selecciona un potrero</option>{farmPaddocks?.map(item => <option value={item.id} key={item.id}>{item.data.name}</option>)}</Select></Field>
              {target && <div className={'transfer-space-preview' + (insufficient ? ' full' : '')}><span>{target.data.name}</span><strong>{target.occupancy} <span aria-hidden="true">→</span> {projected} / {target.data.capacity ?? '—'}</strong><small>Ocupación antes y después · {incoming} entradas</small></div>}
              {insufficient && <p className="warning-banner" role="alert">El potrero no tiene capacidad para todo el grupo. Elige otro destino o reduce la selección.</p>}
              <Field label="Lote de destino" error={state.errors.tolotid}><Select value={destinationLot} onChange={event => setDestinationLot(event.target.value)}><option value="keep">Conservar lote actual</option><option value="none">Sin lote asignado</option>{lots.data?.filter(item => item.data.isActive && item.data.farmId === farmId && selected.every(animal => animal.speciesId === item.data.speciesId)).map(item => <option value={item.id} key={item.id}>{item.data.name}</option>)}</Select></Field>
              <Field label="Motivo del traslado *" error={state.errors.reason}><Textarea required maxLength={300} rows={2} value={reason} onChange={event => setReason(event.target.value)} placeholder="¿Por qué se mueve el grupo?" /></Field><div className="transfer-reasons" aria-label="Motivos frecuentes">{['Rotación de pastoreo', 'Descanso del potrero', 'Reagrupación'].map(text => <Button type="button" variant="quiet" key={text} onClick={() => setReason(text)}>{text}</Button>)}</div></fieldset>
              {destination && selected.length !== eligible.length && <p className="muted">{selected.length - eligible.length} animales ya tienen este destino y lote; se excluyen.</p>}
              <div className="transfer-step-actions"><Button type="button" variant="secondary" onClick={() => go(2)}>Atrás</Button><Button type="submit" variant="primary" disabled={!readyReview}>Revisar traslado <span aria-hidden="true">→</span></Button></div>
            </form>}
            {step === 4 && <form className="transfer-step-content transfer-review" aria-busy={state.busy} onSubmit={event => void submit(event)}><p className="transfer-review-route" aria-label="Ruta del traslado"><span>{sources.join(' + ')}</span><span aria-hidden="true">↓</span><strong>{target?.data.name}</strong></p><dl className="transfer-review-facts"><div><dt>Animales que se trasladan</dt><dd>{eligible.length}</dd></div><div><dt>Finca</dt><dd>{currentFarm}</dd></div><div><dt>Fecha</dt><dd>{date(today())}</dd></div><div><dt>Lote</dt><dd>{destinationLot === 'keep' ? 'Se conserva el lote de cada animal' : destinationLot === 'none' ? 'Sin lote' : lots.data?.find(item => item.id === destinationLot)?.data.name}</dd></div><div><dt>Motivo</dt><dd>{reason}</dd></div></dl>
              <p className="transfer-review-occupancy">{target?.occupancy} presentes + {incoming} entradas = {projected} animales{target?.data.capacity != null ? ' / ' + target.data.capacity + ' de capacidad' : ' · sin límite definido'}</p>
              <details className="transfer-selected-list"><summary>Animales incluidos ({eligible.length})</summary><div>{eligible.map(animal => <span className="tag" key={animal.id}>{animal.internalTag}</span>)}</div></details><p className="muted">El grupo se guarda completo y cada animal conserva su historial.</p>
              <div className="transfer-review-result">{state.error && <p className="error-banner" role="alert">{state.error}</p>}{state.uncertain && <p className="warning-banner">Conservamos este envío. Reintentar confirma el mismo registro y evita duplicarlo.</p>}{insufficient && !state.uncertain && <p className="warning-banner" role="alert">La ocupación cambió y el destino ya no tiene espacio. Vuelve al paso anterior para elegir otro potrero.</p>}</div><div className="transfer-step-actions"><Button type="button" variant="secondary" disabled={locked} onClick={() => go(3)}>Atrás</Button><Button type="submit" variant="primary" disabled={state.busy || !state.uncertain && !readyReview}>{state.busy ? 'Guardando…' : state.uncertain ? 'Reintentar y confirmar registro' : 'Confirmar traslado'}</Button></div>
              {state.error && !locked && <Button type="button" variant="secondary" onClick={() => { setSelected([]); setStep(2); animals.reload(); paddocks.reload(); }}>Actualizar y volver a seleccionar</Button>}
            </form>}
          </>}
        </section>
      </div>
    </div>
  </section>;
}
