import { validatePeriod } from './clientValidation';

export type DateRange = { from: string; to: string };
export type PeriodSelection =
  | ({ mode: 'custom' } & DateRange)
  | { mode: 'month'; month: string }
  | { mode: 'year'; year: string }
  | { mode: 'recent'; days: string };

const dayMilliseconds = 86400000;
function dateTimestamp(value: string): number {
  const timestamp = Date.parse(value + 'T00:00:00Z');
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value) || !Number.isFinite(timestamp) || new Date(timestamp).toISOString().slice(0, 10) !== value || value < '0001-01-01') {
    throw new Error('Revisa las fechas del período.');
  }
  return timestamp;
}

// Date-only arithmetic follows the API's UTC calendar and includes both endpoints.
export function resolvePeriod(selection: PeriodSelection, referenceDate: string): DateRange {
  const referenceTimestamp = dateTimestamp(referenceDate);
  let range: DateRange;
  switch (selection.mode) {
    case 'custom':
      range = { from: selection.from, to: selection.to };
      break;
    case 'month': {
      if (!/^\d{4}-(0[1-9]|1[0-2])$/.test(selection.month)) throw new Error('Selecciona un mes válido.');
      const from = selection.month + '-01';
      dateTimestamp(from);
      const end = new Date(0);
      end.setUTCFullYear(Number(selection.month.slice(0, 4)), Number(selection.month.slice(5)), 0);
      range = { from, to: end.toISOString().slice(0, 10) };
      break;
    }
    case 'year': {
      if (!/^\d{4}$/.test(selection.year) || Number(selection.year) < 1) throw new Error('Escribe un año válido de cuatro dígitos.');
      range = { from: selection.year + '-01-01', to: selection.year + '-12-31' };
      break;
    }
    case 'recent': {
      const days = Number(selection.days);
      if (!/^\d+$/.test(selection.days) || days < 1 || days > 367) throw new Error('Elige entre 1 y 367 días.');
      range = { from: new Date(referenceTimestamp - (days - 1) * dayMilliseconds).toISOString().slice(0, 10), to: referenceDate };
      break;
    }
  }
  dateTimestamp(range.from);
  dateTimestamp(range.to);
  if (range.from > referenceDate) throw new Error('Selecciona un período que ya haya comenzado.');
  if (selection.mode === 'month' || selection.mode === 'year') range.to = range.to > referenceDate ? referenceDate : range.to;
  if (range.to > referenceDate) throw new Error('La fecha final no puede ser posterior a hoy.');
  const error = validatePeriod(range.from, range.to);
  if (error) throw new Error(error);
  return range;
}
