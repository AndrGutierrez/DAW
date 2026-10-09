import { expect, it, vi } from 'vitest';
const registration = vi.hoisted(() => ({ onCLS: vi.fn(), onFCP: vi.fn(), onINP: vi.fn(), onLCP: vi.fn(), onTTFB: vi.fn() }));
vi.mock('web-vitals', () => registration);
import { startPerformanceMonitoring } from './monitoring';
import { getPerformanceSnapshot } from './store';
it('registers document observers once and stores only scalar vital fields', () => {
 vi.stubGlobal('PerformanceObserver', class {});
 startPerformanceMonitoring(); startPerformanceMonitoring();
 expect(registration.onLCP).toHaveBeenCalledTimes(1);
 const callback = registration.onLCP.mock.calls[0][0];
 callback({ name: 'LCP', value: 1500, rating: 'good', entries: [{ element: 'private DOM content' }], id: 'private metric id' });
 expect(getPerformanceSnapshot().vitals.find(v => v.name === 'LCP')).toEqual({ name: 'LCP', value: 1500, rating: 'good', unit: 'ms' });
 vi.unstubAllGlobals();
});
