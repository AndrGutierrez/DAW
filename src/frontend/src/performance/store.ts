export type Vital = { name: string; value: number; rating: 'good' | 'needs-improvement' | 'poor'; unit: string };
export type Timing = { service: string; kind: 'http' | 'data'; duration: number; status: number | null; outcome: 'success' | 'error' | 'cancelled' };
export type PerformanceSnapshot = { vitals: Vital[]; timings: Timing[]; supported: boolean; startedAt: string };
const listeners = new Set<() => void>();
let snapshot: PerformanceSnapshot = { vitals: [], timings: [], supported: typeof PerformanceObserver !== 'undefined', startedAt: new Date().toISOString() };
export const getPerformanceSnapshot = () => snapshot;
export const subscribePerformance = (listener: () => void) => { listeners.add(listener); return () => { listeners.delete(listener); }; };
function publish(next: PerformanceSnapshot) { snapshot = next; listeners.forEach(listener => listener()); }
export function recordVital(vital: Vital) {
  if (!Number.isFinite(vital.value) || vital.value < 0) return;
  publish({ ...snapshot, vitals: [...snapshot.vitals.filter(v => v.name !== vital.name), vital] });
}
export function serviceName(path: string): string {
  const key = path.split('?')[0].split('/')[2];
  return ({ auth: 'Sesión', animals: 'Animales', weights: 'Pesajes', paddocks: 'Potreros', farms: 'Fincas', analytics: 'Indicadores', alerts: 'Seguimiento', inventory: 'Existencias', products: 'Insumos', categories: 'Categorías', reports: 'Reportes', admin: 'Administración', 'exchange-rates': 'Cambio de referencia' } as Record<string, string>)[key] || 'Otros recursos';
}
export function recordTiming(timing: Timing) {
  if (!Number.isFinite(timing.duration) || timing.duration < 0) return;
  publish({ ...snapshot, timings: [...snapshot.timings.slice(-99), { ...timing, duration: Math.round(timing.duration * 10) / 10 }] });
}
export function clearRequestTimings() { publish({ ...snapshot, timings: [] }); }
export function percentile(values: number[], proportion: number): number | null {
  if (!values.length) return null;
  const sorted = [...values].sort((a, b) => a - b);
  return sorted[Math.max(0, Math.min(sorted.length - 1, Math.ceil(sorted.length * proportion) - 1))];
}
export function monitoredFetch(fetcher: typeof fetch): typeof fetch {
  return async (input, init) => {
    // A stream does not have a finite request duration.
    if (new Headers(init?.headers).get('Accept') === 'text/event-stream') return fetcher(input, init);
    const path = typeof input === 'string' ? input : input instanceof URL ? input.pathname : input.url;
    const started = performance.now(); let status: number | null = null; let outcome: Timing['outcome'] = 'error';
    try { const response = await fetcher(input, init); status = response.status; outcome = response.ok ? 'success' : 'error'; return response; }
    catch (error) { if (init?.signal?.aborted || (error instanceof DOMException && error.name === 'AbortError')) outcome = 'cancelled'; throw error; }
    finally { recordTiming({ service: serviceName(path), kind: 'http', duration: performance.now() - started, status, outcome }); }
  };
}
