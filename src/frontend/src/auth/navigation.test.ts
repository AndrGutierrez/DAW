import { describe, expect, it } from 'vitest';
import { dashboardPermissions } from '../api/operations';
import { homePath, loginDestination } from './navigation';

describe('role-aware home', () => {
  it('requires the admin role and every indicator permission', () => {
    expect(homePath(true, () => true)).toBe('/dashboard');
    expect(homePath(false, () => true)).toBe('/animals');
    expect(homePath(true, permission => permission !== dashboardPermissions[0])).toBe('/animals');
  });
  it('preserves the intended animal tab and fragment', () => {
    expect(loginDestination('/animals/animal-id?tab=location#history', '/dashboard')).toBe('/animals/animal-id?tab=location#history');
  });
  it.each([undefined, null, {}, 'https://example.com', '//example.com', '//[', '/\\example.com', '/\n/example.com', '/login?from=/animals', '/animals/../login#history'])('rejects unsafe destinations or a login loop: %s', destination => {
    expect(loginDestination(destination, '/dashboard')).toBe('/dashboard');
  });
});
