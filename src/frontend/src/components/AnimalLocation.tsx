import { useState } from 'react';
import type { FormEvent } from 'react';
import { useAuth } from '../auth/AuthContext';
import { useResource } from '../api/useResource';
import type { AnimalDetail, CatalogItem } from '../api/livestock';
import type { CarePage } from '../api/care';
import type { MovementRecord } from '../api/paddocks';
import { date } from '../api/livestock';
import { Field } from './Field';
import { CareFormResult, CarePagination, useCareSubmission } from './CareForm';

export type MovableAnimal = Pick<AnimalDetail, 'id' | 'farmId' | 'speciesId' | 'internalTag' | 'paddockId' | 'lotId'>;
export function MovementForm({ animal, onSaved }: { animal: MovableAnimal; onSaved: () => void }) {
  const [paddock, setPaddock] = useState(animal.paddockId || '');
  const [lot, setLot] = useState(animal.lotId || '');
  const { can } = useAuth();
  const paddocks = useResource<CatalogItem[]>('/api/paddocks', can('paddocks.list'));
  const lots = useResource<CatalogItem[]>('/api/lots', can('lots.list'));
  const state = useCareSubmission('/api/animals/' + animal.id + '/movements', onSaved);
  function submit(event: FormEvent<HTMLFormElement>) {
    const values = new FormData(event.currentTarget);
    void state.submit(event, { toPaddockId: paddock || null, toLotId: lot || null,
      expectedFromPaddockId: animal.paddockId, expectedFromLotId: animal.lotId, reason: String(values.get('reason') || '').trim() });
  }
  const error = paddocks.error || lots.error;
  return <form className="care-form" onSubmit={submit}>
    <p className="muted">Traslado de {animal.internalTag} con fecha de hoy. El lote identifica el grupo; el potrero identifica la ubicación física.</p>
    {error && <div className="error-banner"><p role="alert">{error}</p><button type="button" className="button secondary" onClick={() => { paddocks.reload(); lots.reload(); }}>Reintentar catálogos</button></div>}
    <fieldset disabled={state.busy || state.uncertain || paddocks.loading || lots.loading || !!error}><div className="form-grid">
      <Field label="Potrero de destino" error={state.errors.topaddockid}><select value={paddock} onChange={e => setPaddock(e.target.value)}><option value="">Sin potrero asignado</option>{paddocks.data?.filter(p => p.data.farmId === animal.farmId && p.data.isActive).map(p => <option key={p.id} value={p.id}>{p.data.name}</option>)}</select></Field>
      <Field label="Lote de destino" error={state.errors.tolotid} hint="Selecciona el grupo explícitamente; no se cambia al elegir un potrero."><select value={lot} onChange={e => setLot(e.target.value)}><option value="">Sin lote asignado</option>{lots.data?.filter(l => l.data.farmId === animal.farmId && l.data.speciesId === animal.speciesId && l.data.isActive).map(l => <option key={l.id} value={l.id}>{l.data.name}</option>)}</select></Field>
    </div><Field label="Motivo del traslado *" error={state.errors.reason}><textarea name="reason" required maxLength={300} rows={2} /></Field></fieldset>
    <CareFormResult state={state} label="Confirmar traslado" />
    {state.error.includes("Otro usuario cambió la ubicación") && <button className="button secondary" type="button" onClick={onSaved}>Actualizar ubicación</button>}
  </form>;
}
export function AnimalLocation({ animal, onUpdated }: { animal: AnimalDetail; onUpdated: () => void }) {
  const { can } = useAuth();
  const [open, setOpen] = useState(false);
  const [page, setPage] = useState(1);
  const history = useResource<CarePage<MovementRecord>>('/api/animals/' + animal.id + '/movements?page=' + page + '&pageSize=10', can('paddocks.list'));
  const paddocks = useResource<CatalogItem[]>('/api/paddocks', can('paddocks.list'));
  const lots = useResource<CatalogItem[]>('/api/lots', can('lots.list'));
  const name = (id: string | null, items: CatalogItem[] | null) => id ? items?.find(i => i.id === id)?.data.name || 'Referencia registrada' : 'Sin asignación';
  return <section id="location" className="panel care-section" aria-labelledby="location-title"><div className="section-heading"><div><span className="eyebrow">UBICACIÓN Y TRAZABILIDAD</span><h2 id="location-title">Potrero y lote</h2><p className="muted">{animal.paddock || 'Sin potrero'} · {animal.lot || 'Sin lote'}</p></div>{can('animals.update') && can('paddocks.list') && can('lots.list') && animal.status === 'Active' && !open && <button className="button secondary" onClick={() => setOpen(true)}>Registrar traslado</button>}</div>
    {open && <MovementForm key={animal.paddockId + ':' + animal.lotId} animal={animal} onSaved={() => { setOpen(false); setPage(1); history.reload(); onUpdated(); }} />}
    {history.loading ? <p role="status">Cargando traslados…</p> : history.error ? <div className="error-banner"><p role="alert">{history.error}</p><button className="button secondary" onClick={history.reload}>Reintentar</button></div> : history.data?.items.length ? <><ol className="care-timeline">{history.data.items.map(m => <li key={m.id}><div className="care-event-heading"><strong>{name(m.fromPaddockId, paddocks.data)} → {name(m.toPaddockId, paddocks.data)}</strong><time>{date(m.date)}</time></div><p className="muted">Lote: {name(m.fromLotId, lots.data)} → {name(m.toLotId, lots.data)}</p><p>{m.reason === 'Initial location registered' ? 'Ubicación inicial registrada' : m.reason === 'Animal record updated' ? 'Ubicación cambiada desde la ficha' : m.reason}</p></li>)}</ol><CarePagination {...history.data} onPage={setPage} /></> : <p className="muted">Sin traslados registrados. No se infiere la entrada al potrero a partir de la última edición de la ficha.</p>}
  </section>;
}
