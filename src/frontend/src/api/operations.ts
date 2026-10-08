import type { CarePage } from './care';
export type ProductData = { sku: string; name: string; categoryId: string; price: number; costPrice: number; unit: string; brand: string; withdrawalDays: number | null; requiresPrescription: boolean; isActive: boolean };
export type ProductRow = { id: string; data: ProductData };
export type CategoryRow = { id: string; data: { name: string; description: string | null; isActive: boolean } };
export type InventoryData = { farmId: string; productId: string; stock: number; minStock: number; maxStock: number; location: string };
export type InventoryRow = { id: string; data: InventoryData; farm: string; product: string; sku: string; category: string; unit: string; productActive: boolean };
export type StockRecord = { id: string; date: string; type: string; quantity: number; reason: string | null; animalId: string | null; openingBalance: boolean };
export type SeriesPoint = { label: string; value: number; count: number };
export type Analytics = { generatedAt: string; from: string; to: string; cost: number; referenceValue: number; critical: number; excess: number; categories: { category: string; cost: number; referenceValue: number }[]; stock: { id: string; farm: string; product: string; unit: string; stock: number; min: number; max: number; outflow: number; rotation: number | null; historySince: string | null }[]; milkByDay: SeriesPoint[]; milkByCurrentLot: SeriesPoint[]; excludedMilk: number; weightByAge: SeriesPoint[]; excludedWeights: number; positiveChecks: number; negativeChecks: number; uncertainChecks: number; positiveCheckPercent: number | null; calvings: number; liveBirths: number; stillbirths: number };
export type ReportRow = { id: string; date: string; farm: string; animal: string; kind: string; product: string | null; quantity: number | null; unit: string | null; notes: string | null; withdrawalEndDate: string | null; detail: string | null };
export type ReportResult = { records: CarePage<ReportRow>; generatedAt: string };
export const units: Record<string, string> = { Kilogram: 'kg', Liter: 'L', Unit: 'unidades', Dose: 'dosis', Bag: 'sacos', Micron: 'micras', Percent: '%', Hectare: 'ha', DoseWithoutUnit: 'dosis (unidad no registrada)' };
export const dashboardPermissions = ['inventory.list', 'products.list', 'production.list', 'weights.list', 'reproduction.list', 'animals.list'];
export const money = (value: number) => new Intl.NumberFormat('es-VE', { style: 'currency', currency: 'USD' }).format(value);
export const periodStart = () => new Date(Date.now() - 29 * 86400000).toISOString().slice(0, 10);
export function params(values: Record<string, string | number | null>) { const query = new URLSearchParams(); for (const [key, value] of Object.entries(values)) if (value !== '' && value !== null) query.set(key, String(value)); return query.toString(); }
