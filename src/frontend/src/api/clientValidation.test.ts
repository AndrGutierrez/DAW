import { expect, it } from 'vitest';
import { validateInventoryLimits, validatePaddockPlan, validatePeriod } from './clientValidation';
const paddock = { farmId: 'farm-a', name: 'North', code: null, areaHectares: null, capacity: null, isActive: true };
it('requires a stock maximum strictly above the minimum', () => {
 expect(validateInventoryLimits({ stock: 10, minStock: 5, maxStock: 5 })).toHaveProperty('maxstock');
 expect(validateInventoryLimits({ stock: 10, minStock: 5, maxStock: 6 })).toEqual({});
});
it('rejects non-finite stock and precision outside the server contract', () => {
 expect(validateInventoryLimits({ stock: Infinity, minStock: .00001, maxStock: 10000000000 })).toEqual(expect.objectContaining({ stock: expect.any(String), minstock: expect.any(String), maxstock: expect.any(String) }));
 expect(validateInventoryLimits({ stock: .0001, minStock: 0, maxStock: 1 })).toEqual({});
});
it('accepts an unset spatial plan and rejects a partial plan', () => {
 expect(validatePaddockPlan(paddock)).toEqual({});
 expect(validatePaddockPlan({ ...paddock, mapX: 0 })).toHaveProperty('mapx');
});
it('allows a boundary-aligned rectangle and rejects an overflow or excess precision', () => {
 expect(validatePaddockPlan({ ...paddock, mapX: 75, mapY: 0, mapWidth: 25, mapHeight: 100 })).toEqual({});
 expect(validatePaddockPlan({ ...paddock, mapX: 76, mapY: 0, mapWidth: 25, mapHeight: 100 })).toHaveProperty('mapx');
 expect(validatePaddockPlan({ ...paddock, mapX: .00001, mapY: 0, mapWidth: 10, mapHeight: 10 })).toHaveProperty('mapx');
});
it('matches the 367 inclusive-day query limit and rejects a reversed period', () => {
 expect(validatePeriod('2025-10-07', '2026-10-08')).toBe('');
 expect(validatePeriod('2025-10-06', '2026-10-08')).toContain('367');
 expect(validatePeriod('2026-10-08', '2026-10-07')).not.toBe('');
});
