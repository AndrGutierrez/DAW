// @vitest-environment jsdom
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expect, it, vi } from 'vitest';
import { constrainRect, configuredRect, overlaps, PaddockLayout, suggestPlacement } from './PaddockLayout';
import { paddock } from '../test/paddocks';
import { layoutPaddocks } from './paddock-scene/model';

it('suggests a free location around both stored and automatic paddocks', () => {
  const item = paddock('source');
  const items = [item, paddock('target', 0, false)];
  const proposed = suggestPlacement(items);
  for (const peer of layoutPaddocks(items)) expect(overlaps(proposed, peer.rect)).toBe(false);
  expect(proposed.x + proposed.width).toBeLessThanOrEqual(100);
  expect(configuredRect(paddock('target', 0, false))).toBeNull();
});
it('keeps both movement and resizing inside the persisted coordinate limits', () => {
  expect(constrainRect({ x: -10, y: 95, width: 40, height: 20 })).toEqual({ x: 0, y: 80, width: 40, height: 20 });
  expect(constrainRect({ x: 10, y: 10, width: 120, height: -3 })).toEqual({ x: 0, y: 10, width: 100, height: 0.0001 });
});
it('offers keyboard and button adjustments without a mouse or WebGL', async () => {
  const user = userEvent.setup(), change = vi.fn();
  render(<PaddockLayout items={[]} rect={{ x: 5, y: 5, width: 28, height: 30 }} name="Este" onChange={change} />);
  const map = screen.getByRole('group', { name: 'Ubicación del potrero en el plano' });
  map.focus(); await user.keyboard('{ArrowRight}');
  expect(change).toHaveBeenLastCalledWith({ x: 6, y: 5, width: 28, height: 30 });
  await user.click(screen.getByRole('button', { name: 'Ampliar potrero' }));
  expect(change).toHaveBeenLastCalledWith({ x: 5, y: 5, width: 30, height: 32 });
});

it('shows unnamed positions as occupied and identifies the paddock under the draft', () => {
  const items = [paddock('source', 2, false), paddock('target', 0, false)];
  const { container, rerender } = render(<PaddockLayout items={items} rect={{ x: 5, y: 5, width: 28, height: 30 }} name="Este" onChange={vi.fn()} />);
  expect(screen.getByRole('img', { name: 'Norte: espacio ocupado · posición automática' })).toBeVisible();
  expect(screen.getByRole('img', { name: 'Sur: espacio ocupado · posición automática' })).toBeVisible();
  expect(screen.getByRole('status')).toHaveTextContent('se superpone con: Norte');
  expect(container.querySelector('.layout-draft-collision')).not.toBeNull();
  rerender(<PaddockLayout items={items} rect={{ x: 5, y: 50, width: 28, height: 30 }} name="Este" onChange={vi.fn()} />);
  expect(screen.queryByRole('status')).not.toBeInTheDocument();
  expect(container.querySelector('.layout-draft-collision')).toBeNull();
});
it('keeps other automatic paddocks in place and excludes the one being edited', () => {
  const items = [paddock('source', 2, false), paddock('target', 0, false)];
  const { container } = render(<PaddockLayout items={items} excludeId="source" rect={{ x: 4, y: 5, width: 28, height: 34 }} name="Norte" onChange={vi.fn()} />);
  expect(screen.queryByRole('img', { name: /Norte: espacio ocupado/ })).not.toBeInTheDocument();
  expect(screen.getByRole('img', { name: /Sur: espacio ocupado/ })).toBeVisible();
  expect(screen.queryByRole('status')).not.toBeInTheDocument();
  expect(container.querySelector('[data-paddock-id="target"] polygon')).toHaveAttribute('points', '231,150.5 427,248.5 189,367.5 -7,269.5');
});

it('quantizes pointer coordinates to the four decimals accepted by the form and storage', () => {
  expect(constrainRect({ x: 70.411865234375, y: 4.921247209821431, width: 29.088134765625, height: 35 }))
    .toEqual({ x: 70.4119, y: 4.9212, width: 29.0881, height: 35 });
  const boundary = constrainRect({ x: 70.41189999999, y: 80.123456, width: 29.58810000001, height: 19.87655 });
  expect(boundary).toEqual({ x: 70.4119, y: 80.1234, width: 29.5881, height: 19.8766 });
  expect(boundary.x + boundary.width).toBeLessThanOrEqual(100);
  expect(boundary.y + boundary.height).toBeLessThanOrEqual(100);
});

it('preserves the size of a small valid paddock when moving it', () => {
  expect(constrainRect({ x: 10.12345, y: 15, width: .5, height: .0001 }))
    .toEqual({ x: 10.1235, y: 15, width: .5, height: .0001 });
});
