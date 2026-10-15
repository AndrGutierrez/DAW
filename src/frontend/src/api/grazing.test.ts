import { describe, expect, it } from 'vitest';
import { grazingEstimate } from './paddocks';
import { paddock } from '../test/paddocks';
import { grazingLook } from '../components/paddock-scene/model';

function occupied() { const item = paddock('source'); item.oldestKnownArrival = '2026-10-01'; item.data.maxStayDays = 10; return item; }
describe('rotation window estimate', () => {
  it('advances from the oldest arrival to upcoming and overdue rotation', () => {
    const item = occupied();
    expect(grazingEstimate(item, '2026-10-01')).toMatchObject({ stage: 'early', progress: 0, remainingDays: 10 });
    expect(grazingEstimate(item, '2026-10-09')).toMatchObject({ stage: 'watch', progress: .8, remainingDays: 2 });
    expect(grazingEstimate(item, '2026-10-11')).toMatchObject({ stage: 'rotate', progress: 1, remainingDays: 0 });
    expect(grazingEstimate(item, '2026-10-14').progress).toBe(1);
  });
  it('does not infer fresh grass from an empty paddock or fabricate missing arrival dates', () => {
    const item = occupied(); item.occupancy = 0;
    expect(grazingEstimate(item).stage).toBe('empty');
    item.occupancy = 2; item.unknownArrivals = 1;
    expect(grazingEstimate(item, '2026-10-05')).toMatchObject({ stage: 'unknown', progress: null });
    item.unknownArrivals = 0; item.oldestKnownArrival = null;
    expect(grazingEstimate(item).stage).toBe('unknown');
  });
  it('requires a configured window and rejects future or invalid arrival dates', () => {
    const item = occupied(); item.data.maxStayDays = null;
    expect(grazingEstimate(item, '2026-10-05').stage).toBe('unknown');
    item.data.maxStayDays = 10;
    expect(grazingEstimate(item, '2026-09-30').stage).toBe('unknown');
    item.oldestKnownArrival = 'invalid';
    expect(grazingEstimate(item).stage).toBe('unknown');
  });
  it('uses descriptive density only and leaves unmeasured terrain without grass claims', () => {
    const item = paddock('source');
    expect(grazingLook(item)).toMatchObject({ stage: 'unknown', blades: 0 });
    const before = grazingEstimate(occupied(), '2026-10-09');
    const other = occupied(); other.data.areaHectares = 100; other.data.capacity = 1000;
    expect(grazingEstimate(other, '2026-10-09')).toEqual(before);
  });
});
