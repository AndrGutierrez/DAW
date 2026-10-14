import { useState } from 'react';
import { Field } from './Field';
import { Button, Input, Select } from './ui/Controls';
import { today, date } from '../api/livestock';
import type { CatalogItem } from '../api/livestock';
import { resolvePeriod } from '../api/periods';
import type { DateRange, PeriodSelection } from '../api/periods';

export type Period = DateRange & { farmId: string };
type PeriodMode = PeriodSelection['mode'];
const recentOptions = [7, 30, 90, 180, 365];

export function PeriodFilter({ value, farms, onApply, busy }: { value: Period; farms: CatalogItem[]; onApply: (period: Period) => void; busy?: boolean }) {
  const [draft, setDraft] = useState(value);
  const [mode, setMode] = useState<PeriodMode>('custom');
  const [month, setMonth] = useState(value.from.slice(0, 7));
  const [year, setYear] = useState(value.from.slice(0, 4));
  const [recent, setRecent] = useState('30');
  const [days, setDays] = useState('30');
  const [error, setError] = useState('');
  const referenceDate = today();
  const selection: PeriodSelection = mode === 'month' ? { mode, month } : mode === 'year' ? { mode, year } : mode === 'recent' ? { mode, days: recent === 'custom' ? days : recent } : { mode, from: draft.from, to: draft.to };
  let range: DateRange | null = null;
  let validation = '';
  try { range = resolvePeriod(selection, referenceDate); } catch (failure) { validation = (failure as Error).message; }

  function changeMode(next: PeriodMode) {
    // Retain the preview when switching back to editable dates.
    if (next === 'custom' && range) setDraft({ ...draft, ...range });
    setMode(next); setError('');
  }

  return <form className="panel filter-bar period-filter" onSubmit={event => {
    event.preventDefault();
    setError(validation);
    if (range) onApply({ ...range, farmId: draft.farmId });
  }}>
    <Field label="Período"><Select value={mode} onChange={event => changeMode(event.target.value as PeriodMode)}>
      <option value="custom">Rango personalizado</option><option value="month">Un mes concreto</option><option value="year">Un año concreto</option><option value="recent">Últimos días</option>
    </Select></Field>
    {mode === 'custom' && <>
      <Field label="Desde"><Input required type="date" value={draft.from} max={draft.to || referenceDate} onChange={event => { setDraft({ ...draft, from: event.target.value }); setError(''); }} /></Field>
      <Field label="Hasta" error={error}><Input required type="date" value={draft.to} min={draft.from} max={referenceDate} onChange={event => { setDraft({ ...draft, to: event.target.value }); setError(''); }} /></Field>
    </>}
    {mode === 'month' && <Field label="Mes" error={error}><Input required type="month" value={month} max={referenceDate.slice(0, 7)} onChange={event => { setMonth(event.target.value); setError(''); }} /></Field>}
    {mode === 'year' && <Field label="Año" error={error}><Input required type="number" min="1" max={Number(referenceDate.slice(0, 4))} step="1" value={year} placeholder="2025" onChange={event => { setYear(event.target.value); setError(''); }} /></Field>}
    {mode === 'recent' && <>
      <Field label="Últimos días" error={recent !== 'custom' ? error : undefined}><Select value={recent} onChange={event => { setRecent(event.target.value); setError(''); }}>
        {recentOptions.map(count => <option key={count} value={count}>Últimos {count} días</option>)}<option value="custom">Otra cantidad de días</option>
      </Select></Field>
      {recent === 'custom' && <Field label="Cantidad de días" error={error} hint="De 1 a 367 días, incluyendo hoy."><Input required type="number" min="1" max="367" step="1" value={days} onChange={event => { setDays(event.target.value); setError(''); }} /></Field>}
    </>}
    <Field label="Finca"><Select value={draft.farmId} onChange={event => setDraft({ ...draft, farmId: event.target.value })}><option value="">Mis fincas</option>{farms.map(farm => <option key={farm.id} value={farm.id}>{farm.data.name}</option>)}</Select></Field>
    <Button type="submit" variant="primary" disabled={busy}>{busy ? 'Consultando…' : 'Aplicar filtros'}</Button>
    {mode !== 'custom' && <p className="period-filter-preview muted" role="status">{range ? 'Desde ' + date(range.from) + ' hasta ' + date(range.to) + '. Incluye ambos días.' : validation}{range && (mode === 'month' || mode === 'year') && range.to === referenceDate && ' El período actual termina hoy.'}</p>}
  </form>;
}
