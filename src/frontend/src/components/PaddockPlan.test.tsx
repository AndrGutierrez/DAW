// @vitest-environment jsdom
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import { PaddockPlan } from './PaddockPlan';
import { paddock } from '../test/paddocks';
import { corners, layoutPaddocks, project, terrainRect } from './paddock-scene/model';
import type { SceneHandle } from './paddock-scene/renderer';

const engine = vi.hoisted(() => ({ create: vi.fn() }));
vi.mock('./paddock-scene/renderer', () => ({ createFarmScene: engine.create }));
let handle: SceneHandle;
beforeEach(() => {
  handle = { update: vi.fn(), zoom: vi.fn(), pan: vi.fn(), fit: vi.fn(), destroy: vi.fn() };
  engine.create.mockReset().mockResolvedValue(handle);
  vi.stubGlobal('WebGLRenderingContext', function WebGLRenderingContext() {});
});
afterEach(() => vi.unstubAllGlobals());

it('offers the same projected capacity and keyboard selection when the renderer fails', async () => {
  engine.create.mockRejectedValue(new Error('GPU unavailable'));
  const user = userEvent.setup(), select = vi.fn();
  render(<PaddockPlan items={[paddock('source', 4), paddock('target')]} selected="target" onSelect={select} destinationMode selectedAnimals={[{ paddockId: 'source' }, { paddockId: 'source' }]}/>);
  await screen.findByText('Vista sencilla disponible.');
  const target = screen.getByRole('button', { name: /^Sur: 4 animales; Capacidad completa/ });
  expect(target).toHaveAttribute('aria-pressed', 'true');
  expect(target).toHaveTextContent('2 → 4 / 4');
  await user.click(screen.getByRole('button', { name: /^Norte: 2 animales;/ }));
  expect(select).toHaveBeenCalledWith('source');
});

it('updates a running scene, pauses it and supports keyboard camera controls', async () => {
  const user = userEvent.setup();
  const view = render(<PaddockPlan items={[paddock('source')]} selected="" onSelect={vi.fn()}/>);
  const pause = await screen.findByRole('button', { name: 'Pausar animación' });
  await waitFor(() => expect(pause).toBeEnabled());
  await user.click(pause);
  expect(handle.update).toHaveBeenLastCalledWith(expect.objectContaining({ motion: false }));
  const map = screen.getByRole('region', { name: 'Vista isométrica de la finca' });
  map.focus(); await user.keyboard('{ArrowRight}+r');
  expect(handle.pan).toHaveBeenCalledWith(80, 0); expect(handle.zoom).toHaveBeenCalledWith(1); expect(handle.fit).toHaveBeenCalled();
  view.unmount(); expect(handle.destroy).toHaveBeenCalledOnce();
});

it('disposes a renderer that finishes initialization after navigation away', async () => {
  let finish!: (handle: SceneHandle) => void;
  engine.create.mockImplementation(() => new Promise<SceneHandle>(resolve => { finish = resolve; }));
  const view = render(<PaddockPlan items={[paddock('source')]} selected="" onSelect={vi.fn()}/>);
  await waitFor(() => expect(engine.create).toHaveBeenCalled());
  view.unmount(); finish(handle);
  await waitFor(() => expect(handle.destroy).toHaveBeenCalledOnce());
  expect(handle.update).not.toHaveBeenCalled();
});

it('locks selection while the server is confirming the movement', async () => {
  const user = userEvent.setup(), select = vi.fn();
  render(<PaddockPlan items={[paddock('source')]} selected="" onSelect={select} disabled/>);
  const button = screen.getByRole('button', { name: /^Norte:/ });
  expect(button).toBeDisabled(); await user.click(button); expect(select).not.toHaveBeenCalled();
});

it('reserves hidden paddock geometry before filtering the visible scene and keeps current occupancy', async () => {
  const inactive = paddock('inactive', 0, false); inactive.data.name = 'Anterior'; inactive.data.isActive = false;
  const source = paddock('source', 3, false), target = paddock('target', 0, false);
  const catalog = [inactive, { ...source, occupancy: 0 }, target];
  const expected = layoutPaddocks(catalog).find(item => item.id === source.id)!;
  const view = render(<PaddockPlan items={[source]} geometryItems={catalog} selected="" onSelect={vi.fn()} />);
  await waitFor(() => expect(handle.update).toHaveBeenCalledWith(expect.objectContaining({ items: [expect.objectContaining({ id: source.id, occupancy: 3, rect: expected.rect })] })));
  expect(screen.queryByRole('button', { name: /^Anterior:/ })).not.toBeInTheDocument();
  view.rerender(<PaddockPlan items={[source, target]} geometryItems={catalog} selected="" onSelect={vi.fn()} />);
  await waitFor(() => expect(handle.update).toHaveBeenLastCalledWith(expect.objectContaining({ items: [expect.objectContaining({ id: source.id, rect: expected.rect }), expect.objectContaining({ id: target.id })] })));
});

it('uses the shared terrain bounds in its static fallback', async () => {
  engine.create.mockRejectedValue(new Error('GPU unavailable'));
  const items = Array.from({ length: 12 }, (_, index) => paddock('auto-' + index, 0, false));
  const { container } = render(<PaddockPlan items={items} selected="" onSelect={vi.fn()} />);
  await screen.findByText('Vista sencilla disponible.');
  const expected = corners(terrainRect(layoutPaddocks(items))).map(project).map(point => point.x + ',' + point.y).join(' ');
  expect(container.querySelector('.farm-static-land')).toHaveAttribute('points', expected);
});
