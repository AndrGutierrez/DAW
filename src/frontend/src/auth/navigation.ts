import { dashboardPermissions } from '../api/operations';

export function homePath(isAdmin: boolean, can: (permission: string) => boolean) {
  return isAdmin && dashboardPermissions.every(can) ? '/dashboard' : '/animals';
}

export function loginDestination(destination: unknown, fallback: string) {
  if (typeof destination !== 'string' || !destination.startsWith('/') || destination.includes('\\')) return fallback;
  try {
    const url = new URL(destination, 'https://daw.invalid');
    if (url.origin !== 'https://daw.invalid' || url.pathname === '/login') return fallback;
    return url.pathname + url.search + url.hash;
  } catch { return fallback; }
}
