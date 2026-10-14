import { expect, it } from 'vitest';
import { resolvePeriod } from './periods';
import type { PeriodSelection } from './periods';

it.each([
  ['2024-02', '2024-02-01', '2024-02-29'],
  ['2025-02', '2025-02-01', '2025-02-28'],
  ['2025-12', '2025-12-01', '2025-12-31'],
  ['2026-10', '2026-10-01', '2026-10-12'],
])('resolves month %s including leap days and caps the current month', (month, from, to) => {
  expect(resolvePeriod({ mode: 'month', month }, '2026-10-12')).toEqual({ from, to });
});
it.each([
  ['2024', '2024-01-01', '2024-12-31'],
  ['2025', '2025-01-01', '2025-12-31'],
  ['2026', '2026-01-01', '2026-10-12'],
])('resolves year %s without including future dates', (year, from, to) => {
  expect(resolvePeriod({ mode: 'year', year }, '2026-10-12')).toEqual({ from, to });
});
it.each([
  ['1', '2026-01-03'], ['7', '2025-12-28'], ['30', '2025-12-05'], ['367', '2025-01-02'],
])('includes exactly %s calendar days even across years', (days, from) => {
  expect(resolvePeriod({ mode: 'recent', days }, '2026-01-03')).toEqual({ from, to: '2026-01-03' });
});
it('does not lose the leap day in a recent period', () => {
  expect(resolvePeriod({ mode: 'recent', days: '3' }, '2024-03-01')).toEqual({ from: '2024-02-28', to: '2024-03-01' });
});
it.each<PeriodSelection>([
  { mode: 'recent', days: '0' }, { mode: 'recent', days: '368' }, { mode: 'recent', days: '1.5' }, { mode: 'recent', days: '' },
  { mode: 'month', month: '2026-13' }, { mode: 'month', month: '2026-11' }, { mode: 'month', month: '' },
  { mode: 'year', year: '2027' }, { mode: 'year', year: '0000' }, { mode: 'year', year: '2.5' },
  { mode: 'custom', from: '2025-02-29', to: '2025-03-01' },
  { mode: 'custom', from: '2026-10-10', to: '2026-10-09' },
  { mode: 'custom', from: '2026-10-01', to: '2026-10-13' },
  { mode: 'custom', from: '2024-01-01', to: '2026-10-12' },
])('rejects an invalid or unsupported selection: %j', selection => {
  expect(() => resolvePeriod(selection, '2026-10-12')).toThrow();
});
it('preserves a valid custom range', () => {
  expect(resolvePeriod({ mode: 'custom', from: '2026-09-17', to: '2026-10-08' }, '2026-10-12')).toEqual({ from: '2026-09-17', to: '2026-10-08' });
});
