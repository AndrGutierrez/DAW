import { expect, it } from 'vitest';
import { auditChanges, auditValue, auditActor } from './audit';
import type { AuditDetail } from './audit';
const base: AuditDetail = { entry: { id: 'event', occurredAt: '2026-10-08T00:00:00Z', action: 'Modified', entityName: 'Animal', entityId: 'animal', userId: null, actorName: null, farmId: null, farmName: null }, oldValues: null, newValues: null, ipAddress: null };
it('distinguishes absent fields from null values when an entity is created or deleted', () => {
  const created = auditChanges({ ...base, newValues: '{"Name":null}' })[0]; expect(created.changed).toBe(true); expect(created.hadBefore).toBe(false); expect(created.hasAfter).toBe(true);
  const deleted = auditChanges({ ...base, oldValues: '{"Name":"Prior"}' })[0]; expect(deleted.hadBefore).toBe(true); expect(deleted.hasAfter).toBe(false);
});
it('compares nested objects regardless of property order while preserving array order', () => {
  const changes = auditChanges({ ...base, oldValues: '{"Details":{"a":1,"b":2},"Roles":["A","B"]}', newValues: '{"Details":{"b":2,"a":1},"Roles":["B","A"]}' });
  expect(changes.find(c => c.field === 'Details')?.changed).toBe(false); expect(changes.find(c => c.field === 'Roles')?.changed).toBe(true);
});
it('uses domain labels and does not turn missing data into a zero or an authenticated actor', () => {
  expect(auditValue(2, 'Status', 'Animal')).toBe('Fallecido'); expect(auditValue(1, 'Sex', 'Animal')).toBe('Hembra'); expect(auditValue(null, 'WeightKg', 'WeightRecord')).toBe('Sin registro');
  expect(auditActor({ ...base.entry, action: 'LoginRejected' })).toBe('Identidad no validada');
});
