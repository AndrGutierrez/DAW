import { onCLS, onFCP, onINP, onLCP, onTTFB } from 'web-vitals';
import type { Metric } from 'web-vitals';
import { recordVital } from './store';
let started = false;
export function startPerformanceMonitoring() {
  if (started || typeof PerformanceObserver === 'undefined') return;
  started = true;
  const report = ({ name, value, rating }: Metric) => recordVital({ name, value, rating, unit: name === 'CLS' ? '' : 'ms' });
  const options = { reportAllChanges: true };
  onLCP(report, options); onINP(report, options); onCLS(report, options);
  onFCP(report, options); onTTFB(report, options);
}
