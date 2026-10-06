import { useEffect, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { useAuth } from '../auth/AuthContext';
import { ApiError } from '../auth/session';
import { errorMessage, fieldErrors } from '../api/errors';
import type { FieldErrors } from '../api/errors';
import { today } from '../api/livestock';
import { validateWeighing } from '../api/weighing';
import { Field } from './Field';
import { useFeedback } from './Feedback';
export type SavedWeighing = { id: string; replayed: boolean; data: { animalId: string; date: string; weightKg: number } };
export function WeighingForm({ animalId, initialDate, onSaved, consecutive = false, onPendingChange }: { animalId: string; initialDate?: string; onSaved: (result: SavedWeighing) => void; consecutive?: boolean; onPendingChange?: (pending: boolean) => void }) {
  const { request } = useAuth();
  const { notify } = useFeedback();
  const [values, setValues] = useState({ date: initialDate || today(), weightKg: '', bodyConditionScore: '', notes: '' });
  const [errors, setErrors] = useState<FieldErrors>({});
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [uncertain, setUncertain] = useState(false);
  const submission = useRef<{ key: string; id: string } | null>(null);
  const inFlight = useRef(false);
  useEffect(() => { onPendingChange?.(busy || uncertain); }, [busy, uncertain, onPendingChange]);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (inFlight.current) return;
    const invalid = validateWeighing(values);
    if (Object.keys(invalid).length) { setErrors(invalid); setError('Revisa los campos señalados.'); return; }
    const data = { date: values.date, weightKg: Number(values.weightKg), bodyConditionScore: values.bodyConditionScore ? Number(values.bodyConditionScore) : null, notes: values.notes.trim() || null };
    const key = JSON.stringify(data);
    if (!submission.current || submission.current.key !== key) submission.current = { key, id: crypto.randomUUID() };
    inFlight.current = true; setBusy(true); setErrors({}); setError('');
    try {
      const result = await request<SavedWeighing>('/api/animals/' + animalId + '/weights', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ submissionId: submission.current.id, ...data }) });
      setUncertain(false); notify(result.replayed ? 'Pesaje confirmado; ya estaba guardado.' : 'Pesaje guardado.'); onSaved(result);
    } catch (failure) {
      setUncertain(!(failure instanceof ApiError && failure.status < 500));
      setError(errorMessage(failure)); setErrors(fieldErrors(failure)); notify(errorMessage(failure), 'error');
    } finally { inFlight.current = false; setBusy(false); }
  }
  function change(key: keyof typeof values, value: string) { setValues(current => ({ ...current, [key]: value })); setErrors(current => ({ ...current, [key.toLowerCase()]: '' })); }
  return <form noValidate onSubmit={submit} className="weighing-form"><fieldset disabled={busy || uncertain}><div className="form-grid">
    <Field label="Fecha del pesaje *" error={errors.date}><input type="date" max={today()} value={values.date} onChange={event => change('date', event.target.value)} /></Field>
    <Field label="Peso vivo (kg) *" error={errors.weightkg}><input type="number" inputMode="decimal" min="0.01" max="999999.99" step="0.01" autoFocus={consecutive} value={values.weightKg} onChange={event => change('weightKg', event.target.value)} /></Field>
    <Field label="Condición corporal" error={errors.bodyconditionscore} hint="Opcional · escala de 1 a 5"><input type="number" min="1" max="5" step="0.01" value={values.bodyConditionScore} onChange={event => change('bodyConditionScore', event.target.value)} /></Field>
  </div><Field label="Observaciones del pesaje" error={errors.notes}><textarea maxLength={500} rows={2} value={values.notes} onChange={event => change('notes', event.target.value)} /></Field></fieldset>
    {error && <p role="alert" className="error-banner">{error}</p>}{uncertain && <p className="warning-banner">Los valores quedan bloqueados hasta confirmar este envío. Reintentar comprueba el mismo registro y evita duplicarlo.</p>}
    <div className="button-row"><button className="button primary" disabled={busy} type="submit">{busy ? 'Guardando pesaje…' : uncertain ? 'Reintentar y confirmar pesaje' : consecutive ? 'Guardar y continuar' : 'Guardar pesaje'}</button></div>
  </form>;
}
