import { describe, expect, it } from 'vitest';
import { occupancyState, stayDays } from './paddocks';
describe('paddock capacity indicators', () => {
  it.each([[0,10,'positive'],[7,10,'positive'],[8,10,'warning'],[9,10,'warning'],[10,10,'critical'],[11,10,'critical'],[0,null,'neutral'],[100,null,'neutral']] as const)('classifies %s residents against %s capacity', (count, cap, tone) => { expect(occupancyState(count, cap).tone).toBe(tone); });
});
describe('recorded arrival dates', () => {
  it('counts elapsed days in UTC across month boundaries', () => { expect(stayDays('2026-09-29', '2026-10-07')).toBe(8); });
  it('reports zero on arrival day', () => { expect(stayDays('2026-10-07', '2026-10-07')).toBe(0); });
  it.each([null, 'bad-date', '2026-10-08'])('keeps missing, invalid or future dates unknown: %s', value => { expect(stayDays(value, '2026-10-07')).toBeNull(); });
});
