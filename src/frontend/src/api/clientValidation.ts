import type { FieldErrors } from './errors';
import type { PaddockData } from './paddocks';
export function validateInventoryLimits(values: { stock: number | null; minStock: number | null; maxStock: number | null }): FieldErrors {
 const errors: FieldErrors = {};
 for (const key of ['stock', 'minStock', 'maxStock'] as const) {
  const value = values[key];
  if (value === null || !Number.isFinite(value) || value < 0 || value >= 10000000000 || Math.round(value * 10000) / 10000 !== value) errors[key.toLowerCase()] = 'Usa un valor no negativo con hasta cuatro decimales.';
 }
 if (!errors.minstock && !errors.maxstock && values.maxStock! <= values.minStock!) errors.maxstock = 'El máximo debe ser mayor que el mínimo.';
 return errors;
}
export function validatePaddockPlan(data: PaddockData): FieldErrors {
 const values = [data.mapX, data.mapY, data.mapWidth, data.mapHeight];
 if (values.every(value => value === null || value === undefined)) return {};
 if (values.some(value => value === null || value === undefined)) return { mapx: 'Completa las cuatro coordenadas o déjalas vacías.' };
 const [x, y, width, height] = values as number[];
 if (values.some(value => !Number.isFinite(value) || Math.round(value! * 10000) / 10000 !== value) || x < 0 || y < 0 || width <= 0 || height <= 0 || x + width > 100 || y + height > 100) return { mapx: 'El área debe caber en el plano de 100 × 100, con hasta cuatro decimales.' };
 return {};
}
export function validatePeriod(from: string, to: string): string {
 const start = Date.parse(from + 'T00:00:00Z'), end = Date.parse(to + 'T00:00:00Z');
 if (!Number.isFinite(start) || !Number.isFinite(end) || start > end) return 'Revisa las fechas del período.';
 return (end - start) / 86400000 > 366 ? 'Selecciona un período de hasta 367 días.' : '';
}
