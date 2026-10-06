import { describe, expect, it } from 'vitest';
import { validateWeighing } from './weighing';
const valid = { date: '2026-10-06', weightKg: '125.50', bodyConditionScore: '3', notes: '' };
describe('weighing validation', () => {
  it('accepts optional condition and a positive weight', () => expect(validateWeighing({ ...valid, bodyConditionScore: '' }, valid.date)).toEqual({}));
  it.each(['0', '-1', '', 'Infinity', 'NaN', '1.234', '1000000'])('rejects invalid weight %s', weightKg =>
    expect(validateWeighing({ ...valid, weightKg }, valid.date)).toHaveProperty('weightkg'));
  it.each(['0.9', '5.1', '3.123'])('rejects invalid condition %s', bodyConditionScore =>
    expect(validateWeighing({ ...valid, bodyConditionScore }, valid.date)).toHaveProperty('bodyconditionscore'));
  it.each(['2026-10-07', '2026-02-30', '', '2026-99-10'])('rejects invalid or future date %s', date =>
    expect(validateWeighing({ ...valid, date }, valid.date)).toHaveProperty('date'));
});
