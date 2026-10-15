// Use Pixi's CSP-compatible synchronization without enabling dynamic evaluation.
import 'pixi.js/unsafe-eval';
import { Application, Container, Graphics, Sprite, Text, Texture } from 'pixi.js';
import { Viewport } from 'pixi-viewport';
import { grazingEstimate, occupancyState } from '../../api/paddocks';
import { cowFrame, shedArt, treeArt } from './art';
import { alongRoute, contains, corners, grazingLook, project, projectedCount, routeBetween, terrainRect, unproject } from './model';
import type { ConfirmedMovement, MovementGroup, Point, ScenePaddock } from './model';

export type SceneState = {
  items: ScenePaddock[];
  selected: string;
  groups: MovementGroup[];
  incoming: number;
  disabled: boolean;
  motion: boolean;
  dark: boolean;
  movement?: ConfirmedMovement;
};
export type SceneHandle = {
  update: (state: SceneState) => void;
  zoom: (direction: number) => void;
  pan: (x: number, y: number) => void;
  fit: () => void;
  destroy: () => void;
};
type Cow = { sprite: Sprite; home: Point; item: ScenePaddock; phase: number; size: number; frames: Texture[] };
type Traveller = { sprite: Sprite; route: Point[]; offset: number; frames: Texture[] };
const tones: Record<string, number> = { positive: 0x54785b, warning: 0xa77832, critical: 0xa76351, neutral: 0x737d71 };

async function vectorTexture(svg: string): Promise<Texture> {
  const image = new Image();
  image.src = 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(svg);
  await image.decode();
  return Texture.from(image);
}

function destroyChildren(container: Container) {
  for (const child of container.removeChildren()) child.destroy({ children: true });
}

function line(graphics: Graphics, a: Point, b: Point, color: number, width: number, alpha = 1) {
  graphics.moveTo(a.x, a.y).lineTo(b.x, b.y).stroke({ color, width, alpha, cap: 'round' });
}

function dashed(graphics: Graphics, a: Point, b: Point, color: number) {
  const length = Math.hypot(b.x - a.x, b.y - a.y);
  for (let distance = 0; distance < length; distance += 17) {
    const end = Math.min(length, distance + 8);
    line(graphics, { x: a.x + (b.x - a.x) * distance / length, y: a.y + (b.y - a.y) * distance / length },
      { x: a.x + (b.x - a.x) * end / length, y: a.y + (b.y - a.y) * end / length }, color, 3, .8);
  }
}

export async function createFarmScene(host: HTMLElement, onSelect: (id: string) => void): Promise<SceneHandle> {
  const app = new Application();
  let initialized = false, disposed = false, alive = true;
  let ownedViewport: Viewport | undefined, resize: ResizeObserver | undefined;
  let visibility: (() => void) | undefined;
  const textures: Texture[] = [];
  function dispose() {
    if (disposed) return;
    disposed = true; alive = false;
    resize?.disconnect();
    if (visibility) document.removeEventListener('visibilitychange', visibility);
    if (initialized) {
      app.stop();
      // Viewport must remove its listener while the application's ticker is still alive.
      if (ownedViewport && !ownedViewport.destroyed) {
        app.stage.removeChild(ownedViewport);
        ownedViewport.destroy({ children: true });
      }
      app.destroy({ removeView: true }, { children: true });
    }
    for (const texture of textures) texture.destroy(true);
  }
  try {
    await app.init({ width: Math.max(1, host.clientWidth), height: Math.max(1, host.clientHeight), backgroundAlpha: 0,
      antialias: true, autoDensity: true, resolution: Math.min(window.devicePixelRatio || 1, 2), preference: 'webgl' });
    initialized = true;
    host.dataset.travelState = 'idle';
    app.ticker.maxFPS = 30;
    const loaded = await Promise.all([
      ...['#50554a', '#967453'].flatMap(color => [cowFrame(color), cowFrame(color, 4), cowFrame(color, -4), cowFrame(color, 0, true)]),
      treeArt, shedArt,
    ].map(async svg => { const texture = await vectorTexture(svg); if (disposed) texture.destroy(true); else textures.push(texture); return texture; }));
    const frameSets = [loaded.slice(0, 4), loaded.slice(4, 8)];
    app.canvas.setAttribute('aria-hidden', 'true');
    host.appendChild(app.canvas);
    const viewport = new Viewport({ screenWidth: host.clientWidth, screenHeight: host.clientHeight, worldWidth: 1500, worldHeight: 900, events: app.renderer.events, ticker: app.ticker });
    ownedViewport = viewport;
    viewport.drag().pinch().wheel().clampZoom({ minScale: .1, maxScale: 3 });
    app.stage.addChild(viewport);
    const world = new Container();
    const ground = new Container(), objects = new Container({ sortableChildren: true }), labels = new Container();
    const routes = new Graphics(), travellers = new Container({ sortableChildren: true });
    world.addChild(ground, routes, objects, travellers, labels);
    viewport.addChild(world);
    let state: SceneState = { items: [], selected: '', groups: [], incoming: 0, disabled: false, motion: true, dark: false };
    let cows: Cow[] = [], moving: Traveller[] = [];
    let elapsed = 0, travelTime = 0, lastMovement = '', geometry = '';

    function fit() {
      viewport.clampZoom({ minScale: .05, maxScale: 6 });
      viewport.fitWorld(true);
      viewport.setZoom(viewport.scale.x * .93, true);
      viewport.moveCenter(viewport.worldWidth / 2, viewport.worldHeight / 2);
      viewport.clampZoom({ minScale: viewport.scale.x * .8, maxScale: Math.max(3, viewport.scale.x * 3) });
      app.render();
    }

    function fence(a: Point, b: Point, gate = false) {
      const count = Math.max(1, Math.ceil(Math.hypot(b.x - a.x, b.y - a.y) / 40));
      for (let index = 0; index <= count; index++) {
        const ratio = index / count;
        if (gate && ratio > .39 && ratio < .61) continue;
        const x = a.x + (b.x - a.x) * ratio, y = a.y + (b.y - a.y) * ratio;
        const post = new Graphics().roundRect(x - 3.5, y - 22, 7, 25, 1.5).fill(0x9a886d);
        post.moveTo(x - 2, y - 20).lineTo(x - 2, y + 1).stroke({ color: 0xc7b99a, width: 1.5 });
        post.zIndex = y;
        objects.addChild(post);
      }
      for (let index = 0; index < count; index++) {
        const start = index / count, end = (index + 1) / count;
        if (gate && start < .61 && end > .39) continue;
        const segment = new Graphics();
        for (const height of [8, 17]) {
          line(segment, { x: a.x + (b.x - a.x) * start, y: a.y + (b.y - a.y) * start - height },
            { x: a.x + (b.x - a.x) * end, y: a.y + (b.y - a.y) * end - height }, 0xafa083, 4);
        }
        segment.zIndex = a.y + (b.y - a.y) * ((start + end) / 2);
        objects.addChild(segment);
      }
    }

    function renderScene() {
      destroyChildren(ground); destroyChildren(objects); destroyChildren(labels); cows = [];
      routes.clear();
      const terrain = corners(terrainRect(state.items)).map(project);
      const points = state.items.length ? terrain : [];
      if (!points.length) { app.render(); return; }
      const left = Math.min(...points.map(point => point.x)) - 95;
      const top = Math.min(...points.map(point => point.y)) - 100;
      const right = Math.max(...points.map(point => point.x)) + 95;
      const bottom = Math.max(...points.map(point => point.y)) + 90;
      world.position.set(-left, -top);
      viewport.worldWidth = right - left; viewport.worldHeight = bottom - top;
      const land = new Graphics().poly(terrain.flatMap(point => [point.x, point.y]))
        .fill({ color: state.dark ? 0x3d4b39 : 0xdce0c3, alpha: .7 });
      ground.addChild(land);
      for (const [index, item] of state.items.entries()) {
        const polygon = corners(item.rect).map(project);
        const flat = polygon.flatMap(point => [point.x, point.y]);
        const look = grazingLook(item, state.dark);
        const patch = new Graphics().poly(flat).fill(look.fill);
        patch.poly(flat).stroke({ color: state.dark ? 0x718264 : 0xb6c097, width: 1 });
        if (item.id === state.selected) patch.poly(flat).stroke({ color: state.dark ? 0xc4dbc0 : 0x567459, width: 4 });
        ground.addChild(patch);
        // Grass density illustrates the configured rotation window, not measured biomass.
        const grass = new Graphics();
        for (let blade = 0; blade < look.blades; blade++) {
          const spot = project({ x: item.rect.x + item.rect.width * (.15 + (blade * .271 % .7)), y: item.rect.y + item.rect.height * (.22 + (blade * .193 % .62)) });
          for (const offset of [-4, 0, 4]) grass.moveTo(spot.x, spot.y).lineTo(spot.x + offset, spot.y - (offset ? 5 : 8)).stroke({ color: look.grass, width: 1.7, alpha: .7 });
        }
        ground.addChild(grass);
        for (let side = 0; side < 4; side++) fence(polygon[side], polygon[(side + 1) % 4], side === 2);

        const cowCount = Math.min(item.occupancy, 3);
        for (let number = 0; number < cowCount; number++) {
          const fractions = [[.3, .48], [.7, .5], [.5, .73]][number];
          const home = { x: item.rect.x + item.rect.width * fractions[0], y: item.rect.y + item.rect.height * fractions[1] };
          const frames = frameSets[(index + number) % 2];
          const sprite = new Sprite(frames[0]);
          sprite.anchor.set(.5, .92);
          const size = Math.min(.52, Math.min(item.rect.width, item.rect.height) * .04);
          sprite.scale.set(size); sprite.position.copyFrom(project(home)); sprite.zIndex = sprite.y;
          objects.addChild(sprite);
          cows.push({ sprite, home, item, phase: index * 2.6 + number * 4.1, size, frames });
        }

        const projected = projectedCount(item, state.selected, state.groups, state.incoming);
        const tone = occupancyState(projected, item.data.capacity).tone;
        const position = project({ x: item.rect.x + item.rect.width / 2, y: item.rect.y + item.rect.height * .22 });
        position.y -= 38;
        const name = item.data.name.length > 23 ? item.data.name.slice(0, 21) + '…' : item.data.name;
        const label = new Text({ text: name + ' · ' + (projected !== item.occupancy ? item.occupancy + ' → ' : '') + projected + '/' + (item.data.capacity ?? '—'),
          style: { fontFamily: 'Segoe UI, sans-serif', fontSize: 17, fontWeight: '600', fill: 0x303b2f } });
        label.anchor.set(.5);
        const badge = new Container();
        const condition = new Text({ text: grazingEstimate(item).label, style: { fontFamily: 'Segoe UI, sans-serif', fontSize: 12, fill: 0x505849 } });
        const width = Math.max(label.width + 42, condition.width + 28);
        const background = new Graphics().roundRect(-width / 2, -24, width, 57, 16).fill({ color: 0xfcfbf4, alpha: .96 });
        background.circle(-width / 2 + 16, -5, 4.5).fill(tones[tone]);
        label.position.set(9, -6);
        condition.anchor.set(.5); condition.y = 18;
        badge.addChild(background, label, condition); badge.position.copyFrom(position);
        labels.addChild(badge);
      }
      // A single shelter and two trees keep the scene legible without implying extra records.
      const first = state.items[0].rect;
      for (const [x, y, texture, scale] of [
        [first.x + first.width + 4, first.y - 7, loaded[9], .62],
        [first.x - 7, first.y + 2, loaded[8], .55],
        [first.x + first.width + 17, first.y - 6, loaded[8], .45],
      ] as const) {
        const sprite = new Sprite(texture); sprite.anchor.set(.5, .92); sprite.scale.set(scale);
        sprite.position.copyFrom(project({ x, y })); sprite.zIndex = sprite.y; objects.addChild(sprite);
      }
      const destination = state.items.find(item => item.id === state.selected);
      if (destination) for (const group of state.groups) {
        const source = state.items.find(item => item.id === group.fromId);
        if (!source) continue;
        const route = routeBetween(source.rect, destination.rect);
        for (let index = 1; index < route.length; index++) dashed(routes, route[index - 1], route[index], state.dark ? 0xc1d5af : 0x647b4e);
        const midpoint = alongRoute(route, .55);
        const tag = new Text({ text: '+' + group.count, style: { fontFamily: 'Segoe UI, sans-serif', fontSize: 17, fontWeight: '600', fill: 0x354d36 } });
        tag.anchor.set(.5); tag.position.set(midpoint.x, midpoint.y - 20); labels.addChild(tag);
      }
      const nextGeometry = JSON.stringify(state.items.map(item => [item.id, item.rect]));
      if (nextGeometry !== geometry) { geometry = nextGeometry; fit(); }
      app.render();
    }

    function startMovement(movement: ConfirmedMovement) {
      if (movement.id === lastMovement) return;
      lastMovement = movement.id;
      destroyChildren(travellers); moving = []; travelTime = 0;
      host.dataset.travelState = 'idle';
      if (!state.motion || !movement.toId) return;
      const destination = state.items.find(item => item.id === movement.toId);
      if (!destination) return;
      for (const [groupIndex, group] of movement.groups.entries()) {
        const source = state.items.find(item => item.id === group.fromId);
        if (!source) continue;
        const route = routeBetween(source.rect, destination.rect);
        for (let index = 0; index < Math.min(3, group.count); index++) {
          const frames = frameSets[(groupIndex + index) % 2], sprite = new Sprite(frames[0]);
          sprite.anchor.set(.5, .92); sprite.scale.set(.42); travellers.addChild(sprite);
          moving.push({ sprite, frames, route, offset: index * .07 });
        }
      }
      host.dataset.travelState = moving.length ? 'playing' : 'idle';
    }

    viewport.on('clicked', (event: { world: Point }) => {
      if (state.disabled) return;
      const point = unproject({ x: event.world.x - world.x, y: event.world.y - world.y });
      const item = [...state.items].reverse().find(item => contains(item.rect, point));
      if (item) onSelect(item.id);
    });
    viewport.on('pointermove', event => {
      const position = viewport.toWorld(event.global);
      const point = unproject({ x: position.x - world.x, y: position.y - world.y });
      viewport.cursor = !state.disabled && state.items.some(item => contains(item.rect, point)) ? 'pointer' : 'grab';
    });

    app.ticker.add(ticker => {
      if (!state.motion) return;
      const seconds = Math.min(ticker.deltaMS / 1000, .08);
      elapsed += seconds;
      for (const cow of cows) {
        const time = elapsed + cow.phase;
        const walking = time % 18 < 5;
        const walkCycle = time % 18 / 5 * Math.PI * 2;
        const position = project({ x: cow.home.x + (walking ? Math.sin(walkCycle) : 0) * Math.min(1.3, cow.item.rect.width * .07), y: cow.home.y + (walking ? Math.sin(walkCycle) : 0) * Math.min(.7, cow.item.rect.height * .04) });
        cow.sprite.position.copyFrom(position); cow.sprite.zIndex = position.y;
        cow.sprite.texture = cow.frames[walking ? Math.floor(time * 6) % 2 + 1 : time % 18 > 10 ? 3 : 0];
        cow.sprite.scale.set(cow.size * (walking && Math.cos(walkCycle) < 0 ? -1 : 1), cow.size);
      }
      if (moving.length) {
        travelTime += seconds;
        for (const actor of moving) {
          const progress = Math.max(0, Math.min(1, travelTime / 4.5 - actor.offset));
          const point = alongRoute(actor.route, progress), next = alongRoute(actor.route, Math.min(1, progress + .01));
          actor.sprite.position.copyFrom(point); actor.sprite.zIndex = point.y;
          actor.sprite.texture = actor.frames[Math.floor(travelTime * 7) % 2 + 1];
          actor.sprite.scale.x = (next.x < point.x ? -1 : 1) * .42;
        }
        if (travelTime > 5.2) { destroyChildren(travellers); moving = []; host.dataset.travelState = 'idle'; }
      }
    });
    resize = new ResizeObserver(() => {
      if (!alive || !host.clientWidth || !host.clientHeight) return;
      app.renderer.resize(host.clientWidth, host.clientHeight);
      viewport.resize(host.clientWidth, host.clientHeight);
      fit();
    });
    resize.observe(host);
    visibility = () => document.hidden ? app.stop() : app.start();
    document.addEventListener('visibilitychange', visibility);
    visibility();
    return {
      update(next) {
        state = next;
        if (!state.motion) { destroyChildren(travellers); moving = []; host.dataset.travelState = 'idle'; }
        renderScene();
        if (state.movement) startMovement(state.movement);
      },
      zoom(direction) { viewport.zoomPercent(direction * .2, true); app.render(); },
      pan(x, y) { viewport.moveCenter(viewport.center.x + x / viewport.scale.x, viewport.center.y + y / viewport.scale.y); app.render(); },
      fit,
      destroy: dispose,
    };
  } catch (error) {
    dispose();
    throw error;
  }
}
