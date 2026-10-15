import { useId, useRef } from 'react';
import type { KeyboardEvent, PointerEvent } from 'react';
import type { PaddockGeometry } from '../api/paddocks';
import { contains, corners, layoutPaddocks, overlaps, project, unproject } from './paddock-scene/model';
import type { Point, Rect } from './paddock-scene/model';
import { Button } from './ui/Controls';

export { configuredRect, overlaps } from './paddock-scene/model';

export function suggestPlacement(items: PaddockGeometry[], excludeId?: string): Rect {
  const existing = layoutPaddocks(items).filter(item => item.id !== excludeId).map(item => item.rect);
  for (let y = 5; y <= 65; y += 5) for (let x = 5; x <= 65; x += 5) {
    const candidate = { x, y, width: 28, height: 30 };
    if (!existing.some(rect => overlaps(candidate, rect))) return candidate;
  }
  return { x: 5, y: 5, width: 28, height: 30 };
}

export function constrainRect(rect: Rect): Rect {
  const scale = 10_000, limit = 100 * scale;
  const width = Math.max(1, Math.min(limit, Math.round(rect.width * scale)));
  const height = Math.max(1, Math.min(limit, Math.round(rect.height * scale)));
  const x = Math.max(0, Math.min(limit - width, Math.round(rect.x * scale)));
  const y = Math.max(0, Math.min(limit - height, Math.round(rect.y * scale)));
  return { x: x / scale, y: y / scale, width: width / scale, height: height / scale };
}

const polygon = (rect: Rect) => corners(rect).map(project).map(point => point.x + ',' + point.y).join(' ');
type Props = { items: PaddockGeometry[]; rect: Rect | null; name: string; disabled?: boolean; excludeId?: string; onChange: (rect: Rect) => void };

export function PaddockLayout({ items, rect, name, disabled, excludeId, onChange }: Props) {
  const svg = useRef<SVGSVGElement>(null);
  const drag = useRef<{ pointer: number; start: Point; rect: Rect; resize: boolean } | null>(null);
  const occupiedPattern = 'occupied-' + useId().replace(/:/g, '');
  const peers = layoutPaddocks(items).filter(item => item.id !== excludeId);
  const collisions = rect ? peers.filter(peer => overlaps(rect, peer.rect)) : [];
  const collision = collisions.length > 0;
  const points = [{ x: 0, y: 0, width: 100, height: 100 }, ...peers.map(peer => peer.rect)].flatMap(corners).map(project);
  const left = Math.min(...points.map(point => point.x)) - 40, top = Math.min(...points.map(point => point.y)) - 70;
  const viewBox = [left, top, Math.max(...points.map(point => point.x)) + 40 - left, Math.max(...points.map(point => point.y)) + 70 - top].join(' ');
  function point(event: PointerEvent<SVGSVGElement>): Point | null {
    const matrix = svg.current?.getScreenCTM();
    if (!matrix || !svg.current) return null;
    const cursor = svg.current.createSVGPoint(); cursor.x = event.clientX; cursor.y = event.clientY;
    return unproject(cursor.matrixTransform(matrix.inverse()));
  }
  function move(x: number, y: number) { if (rect && !disabled) onChange(constrainRect({ ...rect, x: rect.x + x, y: rect.y + y })); }
  function resize(amount: number) { if (rect && !disabled) onChange(constrainRect({ ...rect, width: Math.min(100 - rect.x, rect.width + amount), height: Math.min(100 - rect.y, rect.height + amount) })); }
  function key(event: KeyboardEvent<SVGSVGElement>) {
    if (disabled || !rect) return;
    const step = event.shiftKey ? 5 : 1;
    const actions: Record<string, () => void> = { ArrowLeft: () => move(-step, 0), ArrowRight: () => move(step, 0), ArrowUp: () => move(0, -step), ArrowDown: () => move(0, step), '+': () => resize(step), '=': () => resize(step), '-': () => resize(-step) };
    if (actions[event.key]) { event.preventDefault(); actions[event.key](); }
  }
  const handle = rect ? project({ x: rect.x + rect.width, y: rect.y + rect.height }) : null;
  return <div className="paddock-layout" data-overlap={collision}>
    <svg ref={svg} viewBox={viewBox} role="group" aria-label="Ubicación del potrero en el plano" tabIndex={disabled ? -1 : 0}
      aria-describedby="layout-help" onKeyDown={key} onPointerDown={event => {
        if (disabled || !rect || event.button !== 0) return;
        const start = point(event); if (!start) return;
        event.preventDefault(); event.currentTarget.focus({ preventScroll: true });
        const resize = (event.target as Element).hasAttribute('data-resize');
        const placed = contains(rect, start) || resize ? rect : constrainRect({ ...rect, x: start.x - rect.width / 2, y: start.y - rect.height / 2 });
        drag.current = { pointer: event.pointerId, start, rect: placed, resize }; onChange(placed);
        event.currentTarget.setPointerCapture(event.pointerId);
      }} onPointerMove={event => {
        const active = drag.current; if (!active || active.pointer !== event.pointerId || disabled) return;
        const cursor = point(event); if (!cursor) return;
        const dx = Math.round((cursor.x - active.start.x) * 2) / 2, dy = Math.round((cursor.y - active.start.y) * 2) / 2;
        onChange(constrainRect(active.resize ? { ...active.rect, width: Math.min(100 - active.rect.x, active.rect.width + dx), height: Math.min(100 - active.rect.y, active.rect.height + dy) } : { ...active.rect, x: active.rect.x + dx, y: active.rect.y + dy }));
      }} onPointerUp={() => { drag.current = null; }} onPointerCancel={() => { drag.current = null; }} onLostPointerCapture={() => { drag.current = null; }}>
      <defs><pattern id={occupiedPattern} width="22" height="22" patternUnits="userSpaceOnUse"><path d="M-5 5L5-5M0 22L22 0M17 27L27 17" className="layout-occupied-lines" /></pattern></defs>
      <polygon points={polygon({ x: 0, y: 0, width: 100, height: 100 })} className="layout-land" />
      {Array.from({ length: 9 }, (_, index) => (index + 1) * 10).map(value => {
        const a = project({ x: value, y: 0 }), b = project({ x: value, y: 100 }), c = project({ x: 0, y: value }), d = project({ x: 100, y: value });
        return <path key={value} d={'M' + a.x + ' ' + a.y + 'L' + b.x + ' ' + b.y + 'M' + c.x + ' ' + c.y + 'L' + d.x + ' ' + d.y} className="layout-grid" />;
      })}
      {peers.map(peer => {
        const center = project({ x: peer.rect.x + peer.rect.width / 2, y: peer.rect.y + peer.rect.height / 2 });
        const detail = [!peer.data.isActive ? 'Inactivo' : '', peer.automatic ? 'Posición automática' : ''].filter(Boolean).join(' · ');
        return <g key={peer.id} role="img" aria-label={peer.data.name + ': espacio ocupado' + (detail ? ' · ' + detail.toLowerCase() : '')} data-paddock-id={peer.id} data-automatic={peer.automatic} className={collisions.includes(peer) ? 'layout-peer-collision' : undefined}>
          <title>{peer.data.name + ' · espacio ocupado' + (detail ? ' · ' + detail.toLowerCase() : '')}</title>
          <polygon points={polygon(peer.rect)} className="layout-peer" />
          <polygon points={polygon(peer.rect)} fill={'url(#' + occupiedPattern + ')'} className="layout-peer-hatch" aria-hidden="true" />
          <text x={center.x} y={center.y - (detail ? 10 : 0)} className="layout-peer-label">{peer.data.name.slice(0, 22)}{detail && <tspan x={center.x} dy="27" className="layout-auto-label">{detail}</tspan>}</text>
        </g>;
      })}
      {rect && <g className={collision ? 'layout-draft-collision' : undefined}><polygon points={polygon(rect)} className="layout-draft" /><text x={project({ x: rect.x + rect.width / 2, y: rect.y + rect.height / 2 }).x} y={project({ x: rect.x + rect.width / 2, y: rect.y + rect.height / 2 }).y}>{(name || 'Nuevo potrero').slice(0, 22)}</text>{handle && <circle data-resize cx={handle.x} cy={handle.y} r="24" className="layout-resize" aria-hidden="true" />}</g>}
    </svg>
    <div className="layout-legend" aria-label="Leyenda de ubicación"><span><i className="layout-occupied-swatch" aria-hidden="true" />Espacio ocupado</span><span><i className="layout-draft-swatch" aria-hidden="true" />Vista previa</span></div>
    <p id="layout-help" className="muted">{rect ? 'Arrastra el potrero o toca el terreno para colocarlo. Arrastra el círculo para cambiar su tamaño. Con teclado: flechas para mover y + / − para ajustar.' : 'La distribución automática mantiene el potrero sin coordenadas guardadas.'}</p>
    {rect && <div className="layout-actions" aria-label="Ajustes de ubicación"><Button type="button" disabled={disabled} aria-label="Mover a la izquierda" onClick={() => move(-2, 0)}>←</Button><Button type="button" disabled={disabled} aria-label="Mover hacia arriba" onClick={() => move(0, -2)}>↑</Button><Button type="button" disabled={disabled} aria-label="Mover hacia abajo" onClick={() => move(0, 2)}>↓</Button><Button type="button" disabled={disabled} aria-label="Mover a la derecha" onClick={() => move(2, 0)}>→</Button><Button type="button" disabled={disabled} onClick={() => resize(2)}>Ampliar potrero</Button><Button type="button" disabled={disabled} onClick={() => resize(-2)}>Reducir potrero</Button></div>}
    {collision && <p className="warning-banner" role="status">Esta ubicación se superpone con: {collisions.map(peer => peer.data.name).join(', ')}. Mueve el potrero a una zona libre.</p>}
    {peers.some(peer => peer.automatic) && <p className="muted">Las posiciones automáticas son orientativas; esos potreros aún no tienen coordenadas guardadas.</p>}
    <small className="muted">El tamaño del dibujo es esquemático; la superficie en hectáreas se registra por separado.</small>
  </div>;
}
