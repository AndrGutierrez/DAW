import type { PaddockSnapshot } from '../api/paddocks';

export function paddock(id: string, occupancy = 2, mapped = true): PaddockSnapshot {
  return { id, farm: 'Finca', occupancy, oldestKnownArrival: null, unknownArrivals: 0, lots: [], data: {
    farmId: 'farm', name: id === 'source' ? 'Norte' : 'Sur', code: null, capacity: 4, areaHectares: null, isActive: true,
    ...(mapped ? { mapX: id === 'source' ? 5 : 45, mapY: 10, mapWidth: 30, mapHeight: 35 } : {}),
  } };
}
