import { today } from './livestock';
import type { FieldErrors } from './errors';
export type WeighingValues = { date: string; weightKg: string; bodyConditionScore: string; notes: string };
export function validateWeighing(values: WeighingValues, latestDate = today()): FieldErrors {
  const errors: FieldErrors = {};
  if (!/^\d{4}-\d{2}-\d{2}$/.test(values.date) || Number.isNaN(Date.parse(values.date)) || new Date(values.date).toISOString().slice(0, 10) !== values.date || values.date > latestDate)
    errors.date = 'Selecciona una fecha válida que no sea posterior a hoy.';
  const weight = Number(values.weightKg);
  if (!values.weightKg.trim() || !Number.isFinite(weight) || weight <= 0 || weight > 999999.99 || Math.abs(weight * 100 - Math.round(weight * 100)) > 0.000001)
    errors.weightkg = 'Ingresa un peso mayor que cero, con máximo dos decimales.';
  const condition = Number(values.bodyConditionScore);
  if (values.bodyConditionScore && (!Number.isFinite(condition) || condition < 1 || condition > 5 || Math.abs(condition * 100 - Math.round(condition * 100)) > 0.000001))
    errors.bodyconditionscore = 'La condición corporal debe estar entre 1 y 5, con máximo dos decimales.';
  if (values.notes.length > 500) errors.notes = 'Usa un máximo de 500 caracteres.';
  return errors;
}
