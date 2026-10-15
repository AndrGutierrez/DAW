export type PaddockData = { farmId: string; name: string; code: string | null; areaHectares: number | null; capacity: number | null; isActive: boolean; maxStayDays?: number | null; mapX?: number | null; mapY?: number | null; mapWidth?: number | null; mapHeight?: number | null };
export type PaddockSnapshot = { id: string; data: PaddockData; farm: string; occupancy: number; oldestKnownArrival: string | null; unknownArrivals: number; lots: { id: string | null; name: string | null; count: number }[] };
export type PaddockGeometry = Pick<PaddockSnapshot, 'id' | 'data'>;
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

export function permanenceState(arrival: string | null, limit: number | null | undefined, current?: string): { tone: string; label: string } {
  const days = stayDays(arrival, current);
  if (days === null) return { tone: 'neutral', label: 'Entrada sin fecha registrada' };
  if (!limit) return { tone: 'neutral', label: 'Sin límite de permanencia' };
  if (days >= limit) return { tone: 'critical', label: 'Revisar rotación: ' + days + ' / ' + limit + ' días' };
  if (days / limit >= 0.8) return { tone: 'warning', label: 'Próximo a rotación: ' + days + ' / ' + limit + ' días' };
  return { tone: 'positive', label: 'Permanencia: ' + days + ' / ' + limit + ' días' };
}

export type GrazingEstimate = {
  stage: 'unknown' | 'empty' | 'early' | 'watch' | 'rotate';
  label: string; detail: string; progress: number | null; remainingDays: number | null;
};

/** A rotation-planning proxy from the oldest resident, not measured forage biomass. */
export function grazingEstimate(item: PaddockSnapshot, current?: string): GrazingEstimate {
  const unknown = (detail: string): GrazingEstimate => ({ stage: 'unknown', label: 'Sin estimación', detail, progress: null, remainingDays: null });
  if (item.occupancy === 0) return { stage: 'empty', label: 'Sin pastoreo activo', detail: 'Sin animales. La recuperación del pasto no se calcula con estos datos.', progress: null, remainingDays: null };
  if (item.unknownArrivals > 0) return unknown('Faltan fechas de entrada de ' + item.unknownArrivals + ' animales.');
  const days = stayDays(item.oldestKnownArrival, current);
  if (days === null) return unknown('Falta una fecha de entrada válida.');
  const limit = item.data.maxStayDays;
  if (!limit || limit <= 0) return unknown('Define la permanencia máxima para estimar la rotación.');
  const progress = Math.min(1, days / limit), remainingDays = Math.max(0, limit - days);
  const stage = days >= limit ? 'rotate' : progress >= .8 ? 'watch' : 'early';
  return { stage, progress, remainingDays,
    label: stage === 'rotate' ? 'Revisar rotación' : stage === 'watch' ? 'Rotación próxima' : 'Dentro del período previsto',
    detail: days + ' / ' + limit + ' días desde la entrada más antigua' + (remainingDays ? ' · quedan ' + remainingDays + ' días del período' : ' · período alcanzado'),
  };
}
