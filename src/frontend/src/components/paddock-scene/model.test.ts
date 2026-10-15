import { describe, expect, it } from 'vitest';
import { paddock } from '../../test/paddocks';
import { alongRoute, contains, corners, layoutPaddocks, movementGroups, overlaps, project, projectedCount, routeBetween, terrainRect, unproject } from './model';

describe('farm scene data boundaries', () => {
  it('preserves configured geometry and places unconfigured paddocks without mutating records', () => {
    const items = [paddock('source'), paddock('new', 0, false), paddock('new2', 0, false)];
    const before = structuredClone(items), layout = layoutPaddocks(items);
    expect(layout[0]).toMatchObject({ automatic: false, rect: { x: 5, y: 10, width: 30, height: 35 } });
    expect(layout[1].automatic).toBe(true);
    expect(layout.every((item, index) => layout.slice(index + 1).every(other => !overlaps(item.rect, other.rect)))).toBe(true);
    expect(layout[1].rect).not.toEqual(layout[2].rect);
    expect(items).toEqual(before);
  });

  it('projects arrivals and departures while excluding animals already in the destination', () => {
    const animals = [{ paddockId: 'source' }, { paddockId: 'source' }, { paddockId: 'target' }, { paddockId: null }];
    const groups = movementGroups(animals, 'target');
    expect(projectedCount(paddock('source', 4), 'target', groups)).toBe(2);
    expect(projectedCount(paddock('target', 2), 'target', groups)).toBe(5);
    expect(groups.find(group => group.fromId === null)?.count).toBe(1);
    expect(projectedCount(paddock('unaffected', 3), 'target', groups)).toBe(3);
    expect(animals).toHaveLength(4);
  });

  it('selects the same map coordinates after isometric projection, including negative screen coordinates', () => {
    for (const point of [{ x: 4, y: 80 }, { x: 100, y: 0 }, { x: 45.2, y: 34.7 }]) {
      const result = unproject(project(point));
      expect(result.x).toBeCloseTo(point.x); expect(result.y).toBeCloseTo(point.y);
    }
  });

  it('keeps confirmed travellers at the route endpoints for out-of-range progress', () => {
    const layout = layoutPaddocks([paddock('source'), paddock('target')]);
    const route = routeBetween(layout[0].rect, layout[1].rect);
    expect(alongRoute(route, -1)).toEqual(route[0]);
    expect(alongRoute(route, 2)).toEqual(route[route.length - 1]);
    expect(alongRoute([], .5)).toEqual({ x: 0, y: 0 });
    expect(alongRoute([{ x: 2, y: 3 }, { x: 2, y: 3 }], .5)).toEqual({ x: 2, y: 3 });
  });
});

it('keeps automatic positions identical across catalog and destination response ordering', () => {
  const items = [paddock('target', 0, false), paddock('source', 2, false)];
  const original = structuredClone(items);
  const byId = (entries: typeof items) => Object.fromEntries(layoutPaddocks(entries).map(item => [item.id, item.rect]));
  expect(byId(items)).toEqual(byId([...items].reverse()));
  expect(items).toEqual(original);
});


describe('stable paddock placement and terrain', () => {
  const automatic = (id: string, name: string) => {
    const item = paddock(id, 0, false);
    item.data.name = name;
    return item;
  };
  const byId = (items: ReturnType<typeof paddock>[]) => Object.fromEntries(layoutPaddocks(items).map(item => [item.id, item.rect]));

  it('keeps existing automatic paddocks in place when a saved paddock occupies free ground', () => {
    const existing = [automatic('north', 'Potrero Norte'), automatic('south', 'Potrero Sur')];
    const saved = paddock('new');
    Object.assign(saved.data, { mapX: 70.4119, mapY: 4.9212, mapWidth: 29.0881, mapHeight: 35 });
    const records = structuredClone([...existing, saved]);
    const before = byId(existing), after = byId([...existing, saved]);
    expect(after.north).toEqual(before.north);
    expect(after.south).toEqual(before.south);
    expect(after.new).toEqual({ x: 70.4119, y: 4.9212, width: 29.0881, height: 35 });
    expect([...existing, saved]).toEqual(records);
    expect(byId([saved, ...existing].reverse())).toEqual(after);
  });

  it('relocates only the blocked slot without displacing an unaffected neighbour', () => {
    const existing = [automatic('north', 'Potrero Norte'), automatic('south', 'Potrero Sur')];
    const saved = paddock('new');
    Object.assign(saved.data, { mapX: 4, mapY: 5, mapWidth: 28, mapHeight: 34 });
    const before = byId(existing), after = byId([...existing, saved]);
    expect(after.south).toEqual(before.south);
    expect(after.north).toEqual({ x: 72, y: 5, width: 28, height: 34 });
    const layout = layoutPaddocks([...existing, saved]);
    expect(layout.every((item, index) => layout.slice(index + 1).every(other => !overlaps(item.rect, other.rect)))).toBe(true);
  });

  it('finds non-overlapping overflow slots and keeps every paddock inside the ground', () => {
    const saved = paddock('whole');
    Object.assign(saved.data, { mapX: 0, mapY: 0, mapWidth: 100, mapHeight: 100 });
    const items = [saved, ...Array.from({ length: 12 }, (_, index) => automatic('auto-' + index, 'Potrero ' + index))];
    const layout = layoutPaddocks(items), terrain = terrainRect(layout);
    expect(layout.some(item => item.rect.y > 120)).toBe(true);
    expect(layout.every((item, index) => layout.slice(index + 1).every(other => !overlaps(item.rect, other.rect)))).toBe(true);
    for (const item of layout) for (const point of corners(item.rect)) expect(contains(terrain, point)).toBe(true);
    expect(terrain.y + terrain.height).toBe(Math.max(...layout.map(item => item.rect.y + item.rect.height)) + 24);
    expect(byId(items.reverse())).toEqual(byId([...items].reverse()));
  });

  it('adds equal ground margins on every side and supplies empty scene bounds', () => {
    expect(terrainRect([{ rect: { x: 140, y: -10, width: 30, height: 20 } }])).toEqual({ x: 116, y: -34, width: 78, height: 68 });
    expect(terrainRect([])).toEqual({ x: 0, y: 0, width: 100, height: 100 });
  });
});
