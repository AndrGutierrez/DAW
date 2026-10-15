import { useEffect, useId, useMemo, useRef, useState } from 'react';
import type { KeyboardEvent } from 'react';
import { grazingEstimate, occupancyState, permanenceState } from '../api/paddocks';
import type { PaddockGeometry, PaddockSnapshot } from '../api/paddocks';
import { Button } from './ui/Controls';
import { corners, grazingLook, layoutPaddocks, movementGroups, project, projectedCount, terrainRect } from './paddock-scene/model';
import { PaddockCondition } from './PaddockCondition';
import type { AnimalPosition, ConfirmedMovement, ScenePaddock } from './paddock-scene/model';
import type { SceneHandle, SceneState } from './paddock-scene/renderer';

type Props = {
  items: PaddockSnapshot[]; geometryItems?: PaddockGeometry[]; selected: string; onSelect: (id: string) => void;
  destinationMode?: boolean; disabled?: boolean; incoming?: number;
  selectedAnimals?: AnimalPosition[]; movement?: ConfirmedMovement;
  heading?: string;
  compact?: boolean;
};
const noAnimals: AnimalPosition[] = [];

function StaticPlan({ items, selected }: { items: ScenePaddock[]; selected: string }) {
  const terrain = terrainRect(items);
  const points = [...corners(terrain), ...items.flatMap(item => corners(item.rect))].map(project);
  const left = Math.min(...points.map(point => point.x)) - 90, top = Math.min(...points.map(point => point.y)) - 60;
  const width = Math.max(...points.map(point => point.x)) - left + 90, height = Math.max(...points.map(point => point.y)) - top + 60;
  return <svg className="farm-static-plan" viewBox={[left, top, width, height].join(' ')} aria-hidden="true">
    <polygon className="farm-static-land" points={corners(terrain).map(project).map(point => point.x + ',' + point.y).join(' ')} fill="#dce0c3" fillOpacity=".7" />
    {items.map(item => {
      const center = project({ x: item.rect.x + item.rect.width / 2, y: item.rect.y + item.rect.height / 2 });
      return <g key={item.id}><polygon points={corners(item.rect).map(project).map(point => point.x + ',' + point.y).join(' ')} fill={grazingLook(item).fill} stroke={selected === item.id ? '#54785b' : '#9a886d'} strokeWidth={selected === item.id ? 5 : 3}/><text x={center.x} y={center.y} textAnchor="middle" fontSize="17" fill="#303b2f">{item.data.name}</text><text x={center.x} y={center.y + 24} textAnchor="middle" fontSize="16" fill="#303b2f">{item.occupancy} / {item.data.capacity ?? '—'}</text></g>;
    })}
  </svg>;
}

export function PaddockPlan({ items, geometryItems, selected, onSelect, destinationMode = false, disabled = false, incoming = 0, selectedAnimals = noAnimals, movement, heading, compact = false }: Props) {
  const host = useRef<HTMLDivElement>(null), scene = useRef<SceneHandle | null>(null);
  const select = useRef(onSelect); select.current = onSelect;
  const [phase, setPhase] = useState<'loading' | 'ready' | 'fallback'>('loading');
  const [attempt, setAttempt] = useState(0);
  const [confirmed, setConfirmed] = useState(false);
  const [motion, setMotion] = useState(() => !window.matchMedia?.('(prefers-reduced-motion: reduce)').matches);
  const manualMotion = useRef(false);
  const [dark, setDark] = useState(() => document.documentElement.classList.contains('dark'));
  const instructions = useId();
  const layout = useMemo(() => {
    const visible = new Map(items.map(item => [item.id, item]));
    const geometry = new Map((geometryItems || items).map(item => [item.id, item]));
    for (const item of items) geometry.set(item.id, item);
    return layoutPaddocks([...geometry.values()]).filter(item => visible.has(item.id))
      .map(item => ({ ...visible.get(item.id)!, rect: item.rect, automatic: item.automatic }));
  }, [items, geometryItems]);
  const groups = useMemo(() => destinationMode && selected ? movementGroups(selectedAnimals, selected) : [], [destinationMode, selectedAnimals, selected]);
  const current = useRef<SceneState>({ items: [], selected: '', groups: [], incoming: 0, disabled, motion, dark });
  current.current = { items: layout, selected, groups, incoming: destinationMode ? incoming : 0, disabled, motion, dark, movement };
  const hasItems = items.length > 0;

  useEffect(() => {
    if (!movement) { setConfirmed(false); return; }
    setConfirmed(true);
    const timer = window.setTimeout(() => setConfirmed(false), 6500);
    return () => window.clearTimeout(timer);
  }, [movement]);

  useEffect(() => {
    const preference = window.matchMedia?.('(prefers-reduced-motion: reduce)');
    const changed = () => { if (!manualMotion.current) setMotion(!preference?.matches); };
    preference?.addEventListener('change', changed);
    const observer = new MutationObserver(() => setDark(document.documentElement.classList.contains('dark')));
    observer.observe(document.documentElement, { attributes: true, attributeFilter: ['class'] });
    return () => { preference?.removeEventListener('change', changed); observer.disconnect(); };
  }, []);

  useEffect(() => {
    if (!hasItems || !host.current) return;
    if (!window.WebGLRenderingContext && !window.WebGL2RenderingContext) { setPhase('fallback'); return; }
    let disposed = false;
    const element = host.current;
    setPhase('loading');
    const contextLost = () => { scene.current?.destroy(); scene.current = null; if (!disposed) setPhase('fallback'); };
    element.addEventListener('webglcontextlost', contextLost, true);
    void import('./paddock-scene/renderer').then(module => module.createFarmScene(element, id => select.current(id))).then(handle => {
      if (disposed) { handle.destroy(); return; }
      scene.current = handle; handle.update(current.current); setPhase('ready');
    }).catch(() => { scene.current?.destroy(); scene.current = null; if (!disposed) setPhase('fallback'); });
    return () => {
      disposed = true; element.removeEventListener('webglcontextlost', contextLost, true);
      scene.current?.destroy(); scene.current = null;
    };
  }, [hasItems, attempt]);

  useEffect(() => { scene.current?.update(current.current); }, [layout, selected, groups, incoming, disabled, motion, dark, movement]);

  function navigate(event: KeyboardEvent<HTMLDivElement>) {
    const keys: Record<string, () => void> = {
      ArrowLeft: () => scene.current?.pan(-80, 0), ArrowRight: () => scene.current?.pan(80, 0),
      ArrowUp: () => scene.current?.pan(0, -80), ArrowDown: () => scene.current?.pan(0, 80),
      '+': () => scene.current?.zoom(1), '=': () => scene.current?.zoom(1), '-': () => scene.current?.zoom(-1),
      r: () => scene.current?.fit(), R: () => scene.current?.fit(),
    };
    if (event.target === event.currentTarget && keys[event.key]) { event.preventDefault(); keys[event.key](); }
  }
  const destination = items.find(item => item.id === selected);
  const entries = groups.reduce((total, group) => total + group.count, 0) || incoming;

  return <article className={'panel paddock-plan-panel farm-viewer' + (compact ? ' farm-viewer-compact' : '')} data-scene-state={phase}>
    <div className="farm-viewer-heading"><div><span className="eyebrow">TU FINCA, A SIMPLE VISTA</span><h2>{heading || (destinationMode ? 'Selecciona el destino en el plano' : 'Plano de potreros de la finca')}</h2></div>{hasItems && <div className="farm-viewer-controls" aria-label="Controles del plano">
      <Button type="button" aria-label="Acercar el plano" disabled={phase !== 'ready'} onClick={() => scene.current?.zoom(1)}>+</Button>
      <Button type="button" aria-label="Alejar el plano" disabled={phase !== 'ready'} onClick={() => scene.current?.zoom(-1)}>−</Button>
      <Button type="button" disabled={phase !== 'ready'} onClick={() => scene.current?.fit()}>Centrar</Button>
      <Button type="button" aria-pressed={!motion} disabled={phase !== 'ready'} onClick={() => { manualMotion.current = true; setMotion(value => !value); }}>{motion ? 'Pausar animación' : 'Activar animación'}</Button>
    </div>}</div>
    {hasItems ? <div role="group" aria-label="Plano interactivo de potreros">
      <div className="farm-scene" role="region" aria-label="Vista isométrica de la finca" aria-describedby={instructions} tabIndex={phase === 'ready' ? 0 : -1} onKeyDown={navigate} data-motion={motion ? 'playing' : 'paused'}>
        <div className="farm-canvas-host" ref={host}/>
        {phase === 'loading' && <p className="farm-scene-notice" role="status">Preparando la vista de la finca…</p>}
        {phase === 'fallback' && <><StaticPlan items={layout} selected={selected}/><div className="farm-scene-notice"><span>Vista sencilla disponible.</span><Button type="button" onClick={() => setAttempt(value => value + 1)}>Reintentar vista animada</Button></div></>}
        {destinationMode && destination && entries > 0 && <span className="farm-preview-label">Vista previa · {entries} {entries === 1 ? 'entrada' : 'entradas'} a {destination.data.name}</span>}
        {confirmed && movement && <span key={movement.id} className="farm-confirmed-label" role="status">Traslado confirmado</span>}
      </div>
      <p className="farm-instructions muted" id={instructions}>{phase === 'ready' ? 'Arrastra para explorar; usa la rueda o dos dedos para ampliar. Con teclado: flechas, +, − y R para centrar.' : 'Selecciona un potrero con los controles de abajo.'}</p>
      <div className="farm-paddock-options">{layout.map(item => {
        const count = projectedCount(item, destinationMode ? selected : '', groups, destinationMode ? incoming : 0);
        const capacity = occupancyState(count, item.data.capacity), stay = permanenceState(item.oldestKnownArrival, item.data.maxStayDays);
        return <Button type="button" key={item.id} className={'farm-paddock-option ' + capacity.tone + (selected === item.id ? ' selected' : '')} disabled={disabled} aria-pressed={selected === item.id}
          aria-label={item.data.name + ': ' + count + ' animales; ' + capacity.label + '; ' + stay.label + '; ' + grazingEstimate(item).label} onClick={() => onSelect(item.id)}
          title={capacity.label + ' · ' + stay.label + (item.automatic ? ' · Posición automática' : '')}>
          <span className={'map-dot ' + capacity.tone}/><span>{item.data.name}</span><strong>{count !== item.occupancy ? item.occupancy + ' → ' : ''}{count} / {item.data.capacity ?? '—'}</strong>
          {stay.tone === 'warning' || stay.tone === 'critical' ? <small className={'farm-stay ' + stay.tone}>{stay.label}</small> : null}
          <small className={'farm-grazing ' + grazingEstimate(item).stage}>{grazingEstimate(item).label}</small>
        </Button>;
      })}</div>
    </div> : <p className="farm-empty">Aún no hay potreros activos en esta finca. Registra uno para comenzar.</p>}
    {destination && !compact && <PaddockCondition item={destination} />}
    <div className="farm-viewer-footer">{compact ? <details><summary>Cómo leer el plano</summary><p className="muted">Las vacas representan grupos; los números indican la ocupación real. El terreno muestra una referencia de permanencia, no una medición del pasto. Las posiciones automáticas son esquemáticas y las flechas anticipan el traslado que confirmes.</p></details> : <><p className="map-legend muted"><span className="map-dot positive"/>Disponible <span className="map-dot warning"/>Desde 80 % <span className="map-dot critical"/>Completo <span className="map-dot neutral"/>Sin límite</p><p className="grazing-legend"><span className="grazing-swatch early"/>Período inicial <span className="grazing-swatch watch"/>Rotación próxima <span className="grazing-swatch rotate"/>Revisar rotación <span className="grazing-swatch unknown"/>Sin estimación</p><p className="muted">El color del terreno y la hierba representan la permanencia estimada, no una medición del pasto. Vacas agrupadas; los números indican la ocupación registrada.{destinationMode && selected && entries > 0 ? ' Las flechas muestran la proyección; el servidor valida el traslado al confirmar.' : ''}{layout.some(item => item.automatic) ? ' Los potreros sin coordenadas usan una distribución automática.' : ''} Plano esquemático, sin coordenadas GPS.</p></>}</div>
  </article>;
}
