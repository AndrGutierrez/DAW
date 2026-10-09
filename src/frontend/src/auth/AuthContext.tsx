import { createContext, useContext, useEffect, useSyncExternalStore } from 'react';
import type { PropsWithChildren } from 'react';
import { SessionClient } from './session';
import type { AuthState } from './session';
import { monitoredFetch } from '../performance/store';

const channel = typeof BroadcastChannel !== 'undefined' ? new BroadcastChannel('daw-session-events') : null;
const client = new SessionClient(monitoredFetch(fetch.bind(globalThis)),
  async action => navigator.locks ? await navigator.locks.request('daw-session-cookie', action) : await action(),
  () => channel?.postMessage('logout'),
);
channel?.addEventListener('message', event => {
  if (event.data === 'logout') client.clearLocalSession();
});

type AuthContextValue = AuthState & {
  login: SessionClient['login'];
  logout: SessionClient['logout'];
  request: SessionClient['request'];
  retry: SessionClient['initialize'];
  can: (permission: string) => boolean;
  isAdmin: boolean;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: PropsWithChildren) {
  const state = useSyncExternalStore(client.subscribe, client.getSnapshot);
  useEffect(() => { void client.initialize(); }, []);
  const user = state.session?.user;
  const isAdmin = Boolean(user?.roles.some(role => role === 'Admin' || role === 'Administrador'));
  return <AuthContext.Provider value={{
    ...state, login: client.login, logout: client.logout, request: client.request, retry: client.initialize,
    can: permission => Boolean(user?.isSuperuser || user?.permissions.includes(permission)),
    isAdmin,
  }}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const value = useContext(AuthContext);
  if (!value) throw new Error('useAuth requires AuthProvider.');
  return value;
}
