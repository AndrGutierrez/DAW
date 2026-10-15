import { grazingEstimate } from '../../api/paddocks';
import type { PaddockGeometry, PaddockSnapshot } from '../../api/paddocks';

export type Point = { x: number; y: number };
export type Rect = Point & { width: number; height: number };
export type ScenePaddock = PaddockSnapshot & { rect: Rect; automatic: boolean };
export type AnimalPosition = { paddockId?: string | null };
export type MovementGroup = { fromId: string | null; count: number };
export type ConfirmedMovement = { id: string; toId: string | null; groups: MovementGroup[] };

export function grazingLook(item: PaddockSnapshot, dark = false) {
  const stage = grazingEstimate(item).stage;
  const colors = {
    early: ['#c3cf9e', '#627652', 9], watch: ['#cec39a', '#8e814e', 4],
    rotate: ['#c9b18e', '#93805b', 1], unknown: ['#c6c9b7', '#7d8874', 0], empty: ['#d0d3be', '#7d8874', 0],
  } as const;
  const [fill, grass, blades] = colors[stage];
  return { stage, fill: dark ? { early: '#5e704b', watch: '#766a45', rotate: '#775f46', unknown: '#606858', empty: '#626a56' }[stage] : fill, grass, blades };
}

export function project(point: Point): Point {
  return { x: (point.x - point.y) * 7, y: (point.x + point.y) * 3.5 };
}

export function unproject(point: Point): Point {
  return { x: point.x / 14 + point.y / 7, y: point.y / 7 - point.x / 14 };
}

export function overlaps(a: Rect, b: Rect): boolean {
  return a.x < b.x + b.width && a.x + a.width > b.x && a.y < b.y + b.height && a.y + a.height > b.y;
}

export function configuredRect(item: PaddockGeometry): Rect | null {
  const { mapX: x, mapY: y, mapWidth: width, mapHeight: height } = item.data;
  return [x, y, width, height].every(value => typeof value === 'number' && Number.isFinite(value)) && width! > 0 && height! > 0
    ? { x: x!, y: y!, width: width!, height: height! } : null;
}

function automaticRect(slot: number): Rect {
  return { x: 4 + slot % 3 * 34, y: 5 + Math.floor(slot / 3) * 42, width: 28, height: 34 };
}

export function layoutPaddocks<T extends PaddockGeometry>(items: T[]): (T & { rect: Rect; automatic: boolean })[] {
  const configured = new Map(items.flatMap(item => {
    const rect = configuredRect(item);
    return rect ? [[item.id, rect] as const] : [];
  }));
  const automatic = items.filter(item => !configured.has(item.id))
    .sort((a, b) => a.data.name.localeCompare(b.data.name, 'es') || a.id.localeCompare(b.id));
  const occupied = [...configured.values()];
  const placements = new Map<string, Rect>();
  // Reserve every unaffected slot before relocating a blocked paddock.
  automatic.forEach((item, slot) => {
    const rect = automaticRect(slot);
    if (!occupied.some(other => overlaps(rect, other))) {
      placements.set(item.id, rect);
      occupied.push(rect);
    }
  });
  for (const item of automatic) {
    if (placements.has(item.id)) continue;
    let slot = 0, rect = automaticRect(slot);
    while (occupied.some(other => overlaps(rect, other))) rect = automaticRect(++slot);
    placements.set(item.id, rect);
    occupied.push(rect);
  }
  return items.map(item => ({ ...item, automatic: !configured.has(item.id), rect: configured.get(item.id) ?? placements.get(item.id)! }));
}

/** The schematic ground and camera use the same bounds, including overflow slots. */
export function terrainRect(items: { rect: Rect }[]): Rect {
  if (!items.length) return { x: 0, y: 0, width: 100, height: 100 };
  const margin = 24;
  const left = Math.min(...items.map(item => item.rect.x)) - margin;
  const top = Math.min(...items.map(item => item.rect.y)) - margin;
  const right = Math.max(...items.map(item => item.rect.x + item.rect.width)) + margin;
  const bottom = Math.max(...items.map(item => item.rect.y + item.rect.height)) + margin;
  return { x: left, y: top, width: right - left, height: bottom - top };
}

export function corners(rect: Rect): Point[] {
  return [{ x: rect.x, y: rect.y }, { x: rect.x + rect.width, y: rect.y }, { x: rect.x + rect.width, y: rect.y + rect.height }, { x: rect.x, y: rect.y + rect.height }];
}

export function contains(rect: Rect, point: Point): boolean {
  return point.x >= rect.x && point.x <= rect.x + rect.width && point.y >= rect.y && point.y <= rect.y + rect.height;
}

export function movementGroups(animals: AnimalPosition[], destination: string | null): MovementGroup[] {
  const counts = new Map<string | null, number>();
  for (const animal of animals) {
    const source = animal.paddockId ?? null;
    if (source === destination) continue;
    counts.set(source, (counts.get(source) || 0) + 1);
  }
  return [...counts].map(([fromId, count]) => ({ fromId, count }));
}

export function projectedCount(item: PaddockSnapshot, destination: string, groups: MovementGroup[], incoming = 0): number {
  if (item.id === destination) return item.occupancy + (groups.length ? groups.reduce((total, group) => total + group.count, 0) : incoming);
  return Math.max(0, item.occupancy - groups.filter(group => group.fromId === item.id).reduce((total, group) => total + group.count, 0));
}

/** A schematic passage between gates, not a geographical or traversability claim. */
export function routeBetween(source: Rect, destination: Rect): Point[] {
  const sourceGate = { x: source.x + source.width / 2, y: source.y + source.height };
  const targetGate = { x: destination.x + destination.width / 2, y: destination.y + destination.height };
  const outsideY = Math.max(sourceGate.y, targetGate.y) + 4;
  return [
    { x: sourceGate.x, y: source.y + source.height * .65 }, sourceGate,
    { x: sourceGate.x, y: outsideY }, { x: targetGate.x, y: outsideY }, targetGate,
    { x: targetGate.x, y: destination.y + destination.height * .65 },
  ].filter((point, index, points) => !index || point.x !== points[index - 1].x || point.y !== points[index - 1].y).map(project);
}

export function alongRoute(points: Point[], progress: number): Point {
  if (!points.length) return { x: 0, y: 0 };
  const lengths = points.slice(1).map((point, index) => Math.hypot(point.x - points[index].x, point.y - points[index].y));
  let distance = Math.max(0, Math.min(1, progress)) * lengths.reduce((sum, value) => sum + value, 0);
  for (let index = 0; index < lengths.length; index++) {
    if (distance <= lengths[index] && lengths[index] > 0) {
      const ratio = distance / lengths[index];
      return { x: points[index].x + (points[index + 1].x - points[index].x) * ratio, y: points[index].y + (points[index + 1].y - points[index].y) * ratio };
    }
    distance -= lengths[index];
  }
  return points[points.length - 1];
}
