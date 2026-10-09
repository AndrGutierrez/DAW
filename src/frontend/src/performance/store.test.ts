import { beforeEach, expect, it, vi } from 'vitest';
import { clearRequestTimings, getPerformanceSnapshot, monitoredFetch, percentile, recordTiming, recordVital, serviceName, subscribePerformance } from './store';
beforeEach(() => clearRequestTimings());
it('calculates nearest-rank p75 and preserves the input', () => {
 const values = [100, 20, 40, 80]; expect(percentile(values, .75)).toBe(80);
 expect(values).toEqual([100, 20, 40, 80]); expect(percentile([], .75)).toBeNull();
});
it('keeps only the last 100 timings and rejects invalid measurements', () => {
 for (let n = 0; n < 105; n++) recordTiming({ service: 'Animales', kind: 'data', duration: n, status: null, outcome: 'success' });
 recordTiming({ service: 'Animales', kind: 'data', duration: NaN, status: null, outcome: 'success' });
 expect(getPerformanceSnapshot().timings).toHaveLength(100);
 expect(getPerformanceSnapshot().timings[0].duration).toBe(5);
});
it('stores service categories without identifiers, query values or unknown paths', () => {
 expect(serviceName('/api/animals/private-animal-id?search=private-name')).toBe('Animales');
 expect(serviceName('/api/unknown/private-id')).toBe('Otros recursos');
});
it('updates a vital and notifies subscribers once per accepted update', () => {
 const listener = vi.fn(); const unsubscribe = subscribePerformance(listener);
 recordVital({ name: 'CLS', value: .05, rating: 'good', unit: '' });
 recordVital({ name: 'CLS', value: .08, rating: 'good', unit: '' });
 recordVital({ name: 'CLS', value: -1, rating: 'poor', unit: '' });
 expect(getPerformanceSnapshot().vitals.filter(v => v.name === 'CLS')).toEqual([{ name: 'CLS', value: .08, rating: 'good', unit: '' }]);
 expect(listener).toHaveBeenCalledTimes(2); unsubscribe();
});
it('records an HTTP rejection without retaining its body or request credentials', async () => {
 const fetcher = vi.fn().mockResolvedValue(new Response('private-body', { status: 403 }));
 const response = await monitoredFetch(fetcher)('/api/animals/private-id?secret=yes', { headers: { Authorization: 'Bearer private-token' } });
 expect(response.status).toBe(403);
 const sample = getPerformanceSnapshot().timings[0];
 expect(sample).toMatchObject({ service: 'Animales', kind: 'http', status: 403, outcome: 'error' });
 expect(JSON.stringify(sample)).not.toMatch(/private|secret|Bearer/);
});
it('distinguishes aborted requests from network failures', async () => {
 const abort = new AbortController(); abort.abort();
 await expect(monitoredFetch(vi.fn().mockRejectedValue(new DOMException('Aborted', 'AbortError')))('/api/animals', { signal: abort.signal })).rejects.toThrow();
 await expect(monitoredFetch(vi.fn().mockRejectedValue(new TypeError('Offline')))('/api/weights')).rejects.toThrow();
 expect(getPerformanceSnapshot().timings.map(t => t.outcome)).toEqual(['cancelled', 'error']);
});
it('excludes infinite live streams from finite response timings', async () => {
 await monitoredFetch(vi.fn().mockResolvedValue(new Response()))('/api/analytics/live', { headers: { Accept: 'text/event-stream' } });
 expect(getPerformanceSnapshot().timings).toHaveLength(0);
});
it('clears request history without inventing or clearing document vitals', () => {
 recordTiming({ service: 'Fincas', kind: 'http', status: 200, outcome: 'success', duration: 15 });
 const before = getPerformanceSnapshot().vitals; clearRequestTimings();
 expect(getPerformanceSnapshot().timings).toEqual([]); expect(getPerformanceSnapshot().vitals).toBe(before);
});
