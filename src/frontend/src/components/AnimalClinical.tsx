import { useState } from 'react';
import type { FormEvent } from 'react';
import { useAuth } from '../auth/AuthContext';
import { useResource } from '../api/useResource';
import { date, today, text } from '../api/livestock';
import type { AnimalDetail } from '../api/livestock';
import { careText, formValues } from '../api/care';
import type { ClinicalHistory } from '../api/care';
import { Field } from './Field';
import { CareFormResult, CarePagination, useCareSubmission } from './CareForm';
import { errorMessage } from '../api/errors';
import { useFeedback } from './Feedback';

type Product = { name: string; sku: string; withdrawalDays: number | null; isActive: boolean };
function ClinicalForm({ animal, onSaved }: { animal: AnimalDetail; onSaved: () => void }) {
  const { can } = useAuth();
  const [kind, setKind] = useState('Treatment');
  const [product, setProduct] = useState('');
  const [days, setDays] = useState('');
  const products = useResource<{ id: string; data: Product }[]>('/api/products', can('products.list'));
  const state = useCareSubmission('/api/animals/' + animal.id + '/clinical', onSaved);
  function submit(event: FormEvent<HTMLFormElement>) {
    const values = formValues(event.currentTarget);
    void state.submit(event, { kind, date: values.text('date'), notes: values.text('notes'), cost: values.number('cost'),
      ...(['Treatment', 'Vaccination', 'Deworming'].includes(kind) ? { productId: product || null, dose: values.number('dose') } : {}),
      ...(kind === 'Treatment' ? { route: values.text('route'), endDate: values.text('endDate'), withdrawalDays: days === '' ? null : Number(days) } : {}),
      ...(kind === 'Vaccination' ? { nextDueDate: values.text('nextDueDate') } : {}),
      ...(kind === 'DiseaseCase' ? { severity: values.text('severity'), isContagious: values.text('isContagious') === 'on' } : {}),
      ...(kind === 'Quarantine' ? { endDate: values.text('endDate'), reason: values.text('reason') } : {}),
    });
  }
  return <form className="care-form" onSubmit={submit}><fieldset disabled={state.busy || state.uncertain}><div className="form-grid">
    <Field label="Evento sanitario *" error={state.errors.kind}><select value={kind} onChange={event => { setKind(event.target.value); setProduct(''); setDays(''); }}>{['Treatment', 'Vaccination', 'Deworming', 'DiseaseCase', 'Quarantine'].map(value => <option key={value} value={value}>{careText(value)}</option>)}</select></Field>
    <Field label="Fecha del evento sanitario *" error={state.errors.date}><input name="date" type="date" required defaultValue={today()} min={animal.birthDate || undefined} max={today()} /></Field>
    {['Treatment', 'Vaccination', 'Deworming'].includes(kind) && <><Field label={'Producto' + (kind === 'Treatment' ? ' *' : '')} error={state.errors.productid}><select required={kind === 'Treatment'} value={product} onChange={event => { setProduct(event.target.value); const found = products.data?.find(item => item.id === event.target.value); setDays(found?.data.withdrawalDays?.toString() || ''); }}>
      <option value="">Selecciona un producto</option>{products.data?.filter(item => item.data.isActive).map(item => <option key={item.id} value={item.id}>{item.data.name} · {item.data.sku}</option>)}</select></Field>
      <Field label={'Dosis administrada' + (kind === 'Treatment' ? ' *' : '')} hint="Cantidad según la unidad del producto; no calcula una prescripción." error={state.errors.dose}><input name="dose" type="number" required={kind === 'Treatment'} min="0.001" max="9999999.999" step="0.001" /></Field></>}
    {kind === 'Treatment' && <><Field label="Vía de administración *" error={state.errors.route}><select name="route" defaultValue="Other">{['Oral','Subcutaneous','Intramuscular','Intravenous','Topical','Other'].map(value => <option key={value} value={value}>{careText(value)}</option>)}</select></Field>
      <Field label="Última administración *" error={state.errors.enddate} hint="Registra administraciones realizadas; para una sola dosis, usa la fecha del evento."><input name="endDate" required type="date" max={today()} defaultValue={today()} /></Field>
      <Field label="Días de retiro *" error={state.errors.withdrawaldays} hint="Según el producto y la indicación profesional. Este valor se aplica a leche y sacrificio; el último día es inclusivo."><input required type="number" min="0" max="3650" step="1" value={days} onChange={event => setDays(event.target.value)} /></Field></>}
    {kind === 'Vaccination' && <Field label="Próxima vacunación" error={state.errors.nextduedate}><input name="nextDueDate" type="date" /></Field>}
    {kind === 'DiseaseCase' && <><Field label="Severidad" error={state.errors.severity}><input name="severity" maxLength={50} /></Field><label className="check-field"><input name="isContagious" type="checkbox" /> Contagioso</label></>}
    {kind === 'Quarantine' && <><Field label="Fin previsto de cuarentena" error={state.errors.enddate} hint="Opcional; no cambia automáticamente el estado de salud."><input name="endDate" type="date" /></Field><Field label="Motivo de cuarentena *" error={state.errors.reason}><input name="reason" required maxLength={500} /></Field></>}
    <Field label="Costo del evento" error={state.errors.cost} hint="Importe registrado, sin estimación automática."><input name="cost" type="number" min="0" step="0.01" /></Field>
  </div><Field label="Observaciones sanitarias" error={state.errors.notes}><textarea name="notes" rows={2} maxLength={1000} /></Field></fieldset>
    {products.error && <p role="alert">{products.error}</p>}<CareFormResult state={state} label="Guardar evento sanitario" />
  </form>;
}
function HealthStatusForm({ animal, onSaved }: { animal: AnimalDetail; onSaved: () => void }) {
  const { request } = useAuth(); const { notify } = useFeedback();
  const [busy, setBusy] = useState(false); const [error, setError] = useState('');
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (busy) return;
    const values = formValues(event.currentTarget); setBusy(true); setError('');
    try { await request('/api/animals/' + animal.id + '/health', { method: 'PATCH', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ healthStatus: values.text('status'), reason: values.text('reason') }) }); notify('Estado de salud actualizado.'); onSaved(); }
    catch (failure) { setError(errorMessage(failure)); } finally { setBusy(false); }
  }
  return <details className="care-editor"><summary>Actualizar estado de salud</summary><form onSubmit={event => void submit(event)}><fieldset disabled={busy}><div className="form-grid">
    <Field label="Nuevo estado de salud"><select name="status" defaultValue={animal.healthStatus}>{['Healthy','UnderObservation','InTreatment','Quarantine','Critical'].map(value => <option key={value} value={value}>{text(value)}</option>)}</select></Field>
    <Field label="Motivo del cambio de salud *"><input name="reason" required maxLength={500} /></Field>
  </div></fieldset>{error && <p role="alert" className="error-banner">{error}</p>}<button className="button secondary" disabled={busy}>Guardar estado de salud</button></form></details>;
}
export function AnimalClinical({ animal, onAnimalChanged }: { animal: AnimalDetail; onAnimalChanged: () => void }) {
  const { can } = useAuth(); const [page, setPage] = useState(1); const [open, setOpen] = useState(false);
  const result = useResource<ClinicalHistory>('/api/animals/' + animal.id + '/clinical?page=' + page, can('clinical.list'), true);
  if (!can('clinical.list')) return null;
  const withdrawal = result.data?.withdrawal;
  return <section className="panel animal-details" aria-label="Sanidad"><div className="section-heading"><div><span className="eyebrow">TRAZABILIDAD SANITARIA</span><h2>Sanidad</h2></div>{can('clinical.create') && animal.status === 'Active' && <button className="button secondary" aria-expanded={open} onClick={() => setOpen(!open)}>Registrar evento sanitario</button>}</div>
    {result.loading && <p role="status">Cargando historial sanitario…</p>}{result.error && <p className="error-banner" role="alert">{result.error}<button className="text-button" onClick={result.reload}>Reintentar</button></p>}
    {withdrawal && <div className={withdrawal.blocked ? 'warning-banner' : 'care-clear'}><strong>{withdrawal.blocked ? 'Retiro activo: leche y sacrificio bloqueados' : 'Sin retiro activo registrado'}</strong><p>{withdrawal.blocked ? withdrawal.releaseDate ? 'Liberación: ' + date(withdrawal.releaseDate) + '. Último día restringido: ' + date(withdrawal.lastRestrictedDate) + '.' : 'Hay un tratamiento histórico sin un retiro conocido. Requiere revisión.' : 'La producción también se comprueba para la fecha que registres.'}</p></div>}
    {open && <ClinicalForm key={animal.id} animal={animal} onSaved={() => { setOpen(false); setPage(1); result.reload(); }} />}
    {result.data && <><ol className="care-timeline">{result.data.history.items.map(record => <li key={record.id}><div className="care-event-heading"><strong>{careText(record.data.kind)}</strong><time dateTime={record.data.date}>{date(record.data.date)}</time></div>
      {record.productName && <p>Producto: {record.productName}</p>}
      {record.data.cost != null && <p>Costo registrado: {record.data.cost.toLocaleString("es-VE", { minimumFractionDigits: 2 })}</p>}
      {record.data.kind === 'Treatment' && <p>Dosis: {record.data.dose?.toLocaleString('es-VE', { maximumFractionDigits: 3 })} · {careText(record.data.route)} · última administración {date(record.data.endDate)} · {record.data.withdrawalDays} días de retiro · restringido hasta {date(record.withdrawalEndDate)}</p>}
      {record.data.nextDueDate && <p>Próxima vacunación: {date(record.data.nextDueDate)}</p>}
      {record.data.severity && <p>Severidad: {record.data.severity}{record.data.isContagious ? ' · Contagioso' : ''}</p>}
      {record.data.reason && <p>{record.data.reason}{record.data.endDate ? ' · hasta ' + date(record.data.endDate) : ''}</p>}
      {record.data.notes && <p className="animal-notes">{record.data.notes}</p>}
    </li>)}</ol>{result.data.history.total === 0 && <p className="muted">Aún no hay eventos sanitarios registrados.</p>}
      <CarePagination {...result.data.history} onPage={setPage} />
      {result.data.statusChanges.length > 0 && <details className="care-editor"><summary>Últimos cambios de salud ({result.data.statusChanges.length})</summary><ol className="care-timeline">{result.data.statusChanges.map((change, index) => <li key={index}>{date(change.changedAt)} · {text(change.previousStatus)} → {text(change.newStatus)}{change.reason && <p>{change.reason}</p>}</li>)}</ol></details>}
    </>}{can('animals.update') && animal.status === 'Active' && <HealthStatusForm animal={animal} onSaved={onAnimalChanged} />}
  </section>;
}
