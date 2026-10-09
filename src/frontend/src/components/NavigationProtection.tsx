import { createContext, useCallback, useContext, useEffect, useId, useLayoutEffect, useRef } from 'react';
import type { ReactNode } from 'react';
import { useBeforeUnload, useBlocker } from 'react-router-dom';
import type { BlockerFunction } from 'react-router-dom';
import { useFeedback } from './Feedback';

type Protection = { setDirty: (id: string, dirty: boolean) => void; confirmExit: () => Promise<boolean> };
const Context = createContext<Protection | null>(null);
export function NavigationProtectionProvider({ children }: { children: ReactNode }) {
  const forms = useRef(new Set<string>());
  const { confirm } = useFeedback();
  const setDirty = useCallback((id: string, dirty: boolean) => { if (dirty) forms.current.add(id); else forms.current.delete(id); }, []);
  const confirmExit = useCallback(async () => !forms.current.size || await confirm('Hay cambios sin guardar. ¿Quieres salir y descartarlos?', { destructive: true, confirmLabel: 'Salir sin guardar' }), [confirm]);
  const blocker = useBlocker(useCallback<BlockerFunction>(({ currentLocation, nextLocation }) => forms.current.size > 0 && (currentLocation.pathname !== nextLocation.pathname || currentLocation.search !== nextLocation.search), []));
  const prompted = useRef<string | null>(null);
  useEffect(() => {
    if (blocker.state !== 'blocked') { prompted.current = null; return; }
    if (prompted.current === blocker.location.key) return;
    prompted.current = blocker.location.key;
    void confirmExit().then(leave => { if (leave) blocker.proceed(); else blocker.reset(); });
  }, [blocker, confirmExit]);
  useBeforeUnload(useCallback(event => { if (forms.current.size) { event.preventDefault(); event.returnValue = ''; } }, []));
  return <Context.Provider value={{ setDirty, confirmExit }}>{children}</Context.Provider>;
}
export function useNavigationProtection() {
  const context = useContext(Context);
  if (!context) throw new Error('NavigationProtectionProvider is required.');
  return context;
}
export function useUnsavedChanges(dirty: boolean) {
  const id = useId();
  const { setDirty } = useNavigationProtection();
  useLayoutEffect(() => { setDirty(id, dirty); return () => setDirty(id, false); }, [id, dirty, setDirty]);
  return useCallback(() => setDirty(id, false), [id, setDirty]);
}
