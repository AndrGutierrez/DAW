import { Button, Input, Select, Textarea } from './ui/Controls';
import { useState } from 'react';
import type { FormEvent } from 'react';
import { useAuth } from '../auth/AuthContext';
import { useResource } from '../api/useResource';
import { date, today } from '../api/livestock';
import type { AnimalDetail } from '../api/livestock';
import { careText, formValues } from '../api/care';
import type { CarePage, ProductionData } from '../api/care';
import { Field } from './Field';
import { CareFormResult, CarePagination, useCareSubmission } from './CareForm';
import { useFeedback } from './Feedback';

const operations: Record<string, { method: string; productType: string; unit: string }> = {
  milk: { method: 'Milking', productType: 'Milk', unit: 'Liter' },
  meat: { method: 'Slaughter', productType: 'Meat', unit: 'Kilogram' },
  wool: { method: 'Shearing', productType: 'Wool', unit: 'Kilogram' },
  eggs: { method: 'Collection', productType: 'Eggs', unit: 'Unit' },
};
function ProductionForm({ animal, onSaved }: { animal: AnimalDetail; onSaved: () => void }) {
  const [operation, setOperation] = useState(animal.sex === 'Female' ? 'milk' : 'meat');
  const state = useCareSubmission('/api/animals/' + animal.id + '/production', onSaved);
  const { confirm } = useFeedback();
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = formValues(event.currentTarget);
    const payload = { ...operations[operation], date: data.text('date'), quantity: data.number('quantity'), notes: data.text('notes') };
    if (operation === 'meat' && !state.uncertain && !await confirm('El sacrificio cambia el animal a fallecido y no se puede deshacer. ¿Quieres registrar esta operación?')) return;
    await state.submit(event, payload);
  }
  return <form className="care-form" onSubmit={event => void submit(event)}><fieldset disabled={state.busy || state.uncertain}><div className="form-grid">
    <Field label="Tipo de producción *" error={state.errors.producttype}><Select value={operation} onChange={event => setOperation(event.target.value)}>{Object.entries(operations).filter(([key]) => key !== 'milk' || animal.sex === 'Female').map(([key,value]) => <option key={key} value={key}>{careText(value.productType)} · {careText(value.method)}</option>)}</Select></Field>
    <Field label="Fecha de producción *" error={state.errors.date}><Input name="date" required type="date" min={animal.birthDate || undefined} max={today()} defaultValue={today()} /></Field>
    <Field label={'Cantidad (' + careText(operations[operation].unit) + ') *'} error={state.errors.quantity}><Input name="quantity" required type="number" min="0.0001" step={operation === 'eggs' ? '1' : '0.0001'} /></Field>
  </div><Field label="Observaciones de producción" error={state.errors.notes}><Textarea name="notes" maxLength={1000} rows={2} /></Field></fieldset><p className="muted">El retiro sanitario y las reglas de especie se comprueban al guardar para esta fecha.</p><CareFormResult state={state} label="Guardar producción" /></form>;
}
export function AnimalProduction({ animal, onAnimalChanged }: { animal: AnimalDetail; onAnimalChanged: () => void }) {
  const { can } = useAuth(); const [page, setPage] = useState(1); const [open, setOpen] = useState(false);
  const result = useResource<CarePage<{ id: string; data: ProductionData }>>('/api/animals/' + animal.id + '/production?page=' + page, can('production.list'), true);
  if (!can('production.list')) return null;
  return <section className="panel animal-details" aria-label="Producción"><div className="section-heading"><div><span className="eyebrow">RESULTADOS PRODUCTIVOS</span><h2>Producción</h2></div>{can('production.create') && animal.status === 'Active' && <Button className="button secondary" aria-expanded={open} onClick={() => setOpen(!open)}>Registrar producción</Button>}</div>
    {result.loading && <p role="status">Cargando producción…</p>}{result.error && <p className="error-banner" role="alert">{result.error}<Button className="text-button" onClick={result.reload}>Reintentar</Button></p>}
    {open && <ProductionForm animal={animal} onSaved={onAnimalChanged} />}
    {result.data && <><div className="table-scroll"><table><thead><tr><th>Fecha</th><th>Producto</th><th>Método</th><th>Cantidad</th><th>Observaciones</th></tr></thead><tbody>{result.data.items.map(record => <tr key={record.id}><td>{date(record.data.date)}</td><td>{careText(record.data.productType)}</td><td>{careText(record.data.method)}</td><td>{record.data.quantity.toLocaleString('es-VE')} {careText(record.data.unit)}</td><td>{record.data.notes || '—'}</td></tr>)}</tbody></table></div>{result.data.total === 0 && <p className="muted">Aún no hay producción registrada.</p>}<CarePagination {...result.data} onPage={setPage} /></>}
  </section>;
}
