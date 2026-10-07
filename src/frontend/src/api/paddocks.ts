export type PaddockData = { farmId: string; name: string; code: string | null; areaHectares: number | null; capacity: number | null; isActive: boolean };
export type PaddockSnapshot = { id: string; data: PaddockData; farm: string; occupancy: number; oldestKnownArrival: string | null; unknownArrivals: number; lots: { id: string | null; name: string | null; count: number }[] };
export type Resident = { id: string; internalTag: string; name: string | null; lot: string | null; lotId: string | null; speciesId: string; arrivalDate: string | null };
export type MovementRecord = { id: string; fromPaddockId: string | null; toPaddockId: string | null; fromLotId: string | null; toLotId: string | null; date: string; reason: string | null };
export function occupancyState(occupancy: number, capacity: number | null): { tone: string; label: string } {
  if (capacity === null) return { tone: 'neutral', label: 'Sin capacidad definida' };
  if (occupancy > capacity) return { tone: 'critical', label: 'Capacidad excedida' };
  if (occupancy === capacity) return { tone: 'critical', label: 'Capacidad completa' };
  if (occupancy / capacity >= 0.8) return { tone: 'warning', label: 'Poco espacio disponible' };
  return { tone: 'positive', label: 'Espacio disponible' };
}
export function stayDays(arrival: string | null, current = new Date().toISOString().slice(0, 10)): number | null {
  if (!arrival) return null;
  const difference = (Date.parse(current + 'T00:00:00Z') - Date.parse(arrival + 'T00:00:00Z')) / 86400000;
  return Number.isFinite(difference) && difference >= 0 ? difference : null;
}
