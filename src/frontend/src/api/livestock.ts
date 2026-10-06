export type Animal = {
  id: string; farmId: string; speciesId: string; internalTag: string; name: string | null;
  species: string; breed: string | null; sex: string; status: string; healthStatus: string;
  birthDate: string | null; currentWeightKg: number | null; lot: string | null; farm: string;
  coverPhotoUrl: string | null; photoCount: number; updatedAt: string;
};
export type AnimalDetail = Animal & {
  officialId: string | null; rfid: string | null; origin: string; purpose: string; birthWeightKg: number | null;
  bodyConditionScore: number | null; color: string | null; markings: string | null; paddock: string | null;
  photos: { id: string; url: string; uploadedAt: string }[]; notes: string | null;
  breedId: string | null; lotId: string | null; paddockId: string | null; damId: string | null; sireId: string | null;
};
export type AnimalPageResult = { items: Animal[]; total: number; page: number; pageSize: number };
export type CatalogItem = { id: string; data: { name: string; code?: string; isActive: boolean; farmId?: string; speciesId?: string; paddockId?: string | null; purpose?: string } };
export type GrowthPoint = { recordId: string; date: string; weightKg: number; bodyConditionScore: number | null; dailyGainKg: number | null };
export type WeightHistoryItem = { id: string; date: string; weightKg: number; bodyConditionScore: number | null; notes: string | null; createdAt: string; usedForCurve: boolean };
export type GrowthResult = { points: GrowthPoint[]; records: WeightHistoryItem[]; total: number; page: number; pageSize: number; totalDates: number };
export const labels: Record<string, string> = {
  Active: 'Activo', Sold: 'Vendido', Dead: 'Fallecido', Transferred: 'Transferido', Lost: 'Extraviado',
  Healthy: 'Sano', UnderObservation: 'En observación', InTreatment: 'En tratamiento', Quarantine: 'Cuarentena', Critical: 'Crítico',
  Male: 'Macho', Female: 'Hembra', Born: 'Nacimiento', Purchased: 'Compra',
  Meat: 'Carne', Milk: 'Leche', Wool: 'Lana', Eggs: 'Huevos', DualPurpose: 'Doble propósito', Work: 'Trabajo',
};
export const text = (value: string | null | undefined) => value ? labels[value] || value : 'Sin registro';
export const number = (value: number, digits = 2) => new Intl.NumberFormat('es-VE', { maximumFractionDigits: digits }).format(value);
export const kg = (value: number | null) => value === null ? 'Sin pesaje' : number(value) + ' kg';
export const date = (value: string | null) => value ? new Date(value.length === 10 ? value + 'T12:00:00' : value).toLocaleDateString('es-VE') : 'Sin registro';
export const today = () => new Date().toISOString().slice(0, 10);
