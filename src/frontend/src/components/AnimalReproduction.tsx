import { Button, Input, Select, Textarea } from './ui/Controls';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import type { FormEvent } from 'react';
import { useAuth } from '../auth/AuthContext';
import { useResource } from '../api/useResource';
import { date, today, kg } from '../api/livestock';
import type { AnimalDetail } from '../api/livestock';
import { careText, formValues } from '../api/care';
import type { ReproductiveHistory } from '../api/care';
import { Field } from './Field';
import { ParentPicker } from './ParentPicker';
import { CareFormResult, CarePagination, useCareSubmission } from './CareForm';

function ReproductionForm({ animal, onSaved }: { animal: AnimalDetail; onSaved: () => void }) {
  const [kind, setKind] = useState('PregnancyCheck');
  const [result, setResult] = useState('Positive');
  const [sire, setSire] = useState('');
  const [offspring, setOffspring] = useState('');
  const state = useCareSubmission('/api/animals/' + animal.id + '/reproduction', onSaved);
  function submit(event: FormEvent<HTMLFormElement>) {
    const values = formValues(event.currentTarget);
    void state.submit(event, { kind, date: values.text('date'), notes: values.text('notes'),
      ...(['Heat','PregnancyCheck'].includes(kind) ? { method: values.text('method') } : {}),
      ...(['Mating','Insemination'].includes(kind) ? { sireId: sire || null } : {}),
      ...(kind === 'PregnancyCheck' ? { result, expectedCalvingDate: result === 'Positive' ? values.text('expectedCalvingDate') : null } : {}),
      ...(kind === 'Calving' ? { offspringCount: values.number('offspringCount'), stillbornCount: values.number('stillbornCount'), difficulty: values.text('difficulty') } : {}),
      ...(kind === 'Weaning' ? { offspringId: offspring || null, weightKg: values.number('weightKg') } : {}),
      ...(kind === 'Abortion' ? { reason: values.text('reason') } : {}),
    });
  }
  return <form className="care-form" onSubmit={submit}><fieldset disabled={state.busy || state.uncertain}><div className="form-grid">
    <Field label="Evento reproductivo *" error={state.errors.kind}><Select value={kind} onChange={event => setKind(event.target.value)}>{['Heat','Mating','Insemination','PregnancyCheck','Calving','Weaning','Abortion'].map(value => <option key={value} value={value}>{careText(value)}</option>)}</Select></Field>
    <Field label="Fecha del evento reproductivo *" error={state.errors.date}><Input name="date" required type="date" defaultValue={today()} min={animal.birthDate || undefined} max={today()} /></Field>
    {['Heat','PregnancyCheck'].includes(kind) && <Field label="Método de observación" error={state.errors.method}><Input name="method" maxLength={50} /></Field>}
    {['Mating','Insemination'].includes(kind) && <ParentPicker label="Reproductor" emptyLabel="Sin reproductor identificado" hint="Misma finca y especie; se comprueba la edad para la fecha del evento." sex="Male" farmId={animal.farmId} speciesId={animal.speciesId} excludeId={animal.id} value={sire} onChange={setSire} error={state.errors.sireid} />}
    {kind === 'PregnancyCheck' && <><Field label="Resultado del diagnóstico *" error={state.errors.result}><Select value={result} onChange={event => setResult(event.target.value)}>{['Positive','Negative','Uncertain'].map(value => <option key={value} value={value}>{careText(value)}</option>)}</Select></Field>
      {result === 'Positive' && <Field label="Fecha prevista de parto" error={state.errors.expectedcalvingdate} hint="Estimación indicada en el diagnóstico; no es una confirmación de parto."><Input name="expectedCalvingDate" type="date" /></Field>}</>}
    {kind === 'Calving' && <><Field label="Crías totales *" error={state.errors.offspringcount}><Input name="offspringCount" type="number" min="1" max="100" step="1" defaultValue="1" required /></Field>
      <Field label="Crías nacidas muertas *" error={state.errors.stillborncount}><Input name="stillbornCount" type="number" min="0" max="100" step="1" defaultValue="0" required /></Field>
      <Field label="Dificultad del parto" error={state.errors.difficulty}><Select name="difficulty">{['Easy','Assisted','Difficult','Cesarean'].map(value => <option key={value} value={value}>{careText(value)}</option>)}</Select></Field></>}
    {kind === 'Weaning' && <><ParentPicker label="Cría" emptyLabel="Sin cría identificada" hint="Descendientes de esta madre en la misma finca y especie." sex="" damId={animal.id} farmId={animal.farmId} speciesId={animal.speciesId} excludeId={animal.id} value={offspring} onChange={setOffspring} error={state.errors.offspringid} /><Field label="Peso al destete (kg)" error={state.errors.weightkg}><Input name="weightKg" type="number" min="0.01" max="999999.99" step="0.01" /></Field></>}
    {kind === 'Abortion' && <Field label="Motivo del aborto" error={state.errors.reason}><Input name="reason" maxLength={500} /></Field>}
  </div>{kind === 'Calving' && <p className="muted">Registra después cada cría en Animales y selecciona esta madre para completar su genealogía.</p>}
    <Field label="Observaciones reproductivas" error={state.errors.notes}><Textarea name="notes" maxLength={1000} rows={2} /></Field></fieldset><CareFormResult state={state} label="Guardar evento reproductivo" />
  </form>;
}
export function AnimalReproduction({ animal }: { animal: AnimalDetail }) {
  const { can } = useAuth(); const [page, setPage] = useState(1); const [open, setOpen] = useState(false);
  const result = useResource<ReproductiveHistory>('/api/animals/' + animal.id + '/reproduction?page=' + page, can('reproduction.list'), true);
  if (!can('reproduction.list')) return null;
  return <section className="panel animal-details" aria-label="Reproducción"><div className="section-heading"><div><span className="eyebrow">CICLO REPRODUCTIVO</span><h2>Reproducción</h2></div>{can('reproduction.create') && animal.status === 'Active' && animal.sex === 'Female' && <Button className="button secondary" aria-expanded={open} onClick={() => setOpen(!open)}>Registrar evento reproductivo</Button>}</div>
    {result.loading && <p role="status">Cargando historial reproductivo…</p>}{result.error && <p className="error-banner" role="alert">{result.error}<Button className="text-button" onClick={result.reload}>Reintentar</Button></p>}
    {result.data && <div className="care-state"><strong>{careText(result.data.status.state)}</strong>{result.data.status.date && <span> · {date(result.data.status.date)}</span>}{result.data.status.expectedCalvingDate && <p>Parto previsto: {date(result.data.status.expectedCalvingDate)}</p>}<small className="muted">El estado sigue el último diagnóstico, parto o aborto por fecha. Una monta no confirma gestación.</small></div>}
    {open && <ReproductionForm animal={animal} onSaved={() => { setOpen(false); setPage(1); result.reload(); }} />}
    {result.data && <><ol className="care-timeline">{result.data.history.items.map(record => <li key={record.id}><div className="care-event-heading"><strong>{careText(record.data.kind)}</strong><time dateTime={record.data.date}>{date(record.data.date)}</time></div>
      {record.data.result && <p>{careText(record.data.result)}{record.data.expectedCalvingDate ? ' · parto previsto ' + date(record.data.expectedCalvingDate) : ''}</p>}
      {record.data.offspringCount != null && <p>{record.data.offspringCount} crías · {record.data.stillbornCount} nacidas muertas · {careText(record.data.difficulty)}</p>}
      {record.data.sireId && <Link className="back-link" to={'/animals/' + record.data.sireId}>Consultar reproductor</Link>}
      {record.data.method && <p>Método: {record.data.method}</p>}{record.data.weightKg != null && <p>Peso al destete: {kg(record.data.weightKg)}</p>}
      {record.data.reason && <p>{record.data.reason}</p>}{record.data.notes && <p className="animal-notes">{record.data.notes}</p>}
    </li>)}</ol>{result.data.history.total === 0 && <p className="muted">Aún no hay eventos reproductivos registrados.</p>}<CarePagination {...result.data.history} onPage={setPage} /></>}
  </section>;
}
