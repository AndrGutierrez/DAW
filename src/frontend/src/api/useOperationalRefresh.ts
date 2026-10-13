import { useEffect, useRef } from 'react';

export function useOperationalRefresh(refresh: () => void, enabled = true) {
  const latest = useRef(refresh); latest.current = refresh;
  useEffect(() => {
    if (!enabled) return;
    const update = () => { if (!document.hidden) latest.current(); };
    const timer = window.setInterval(update, 30_000);
    window.addEventListener('focus', update);
    window.addEventListener('daw:data-changed', update);
    document.addEventListener('visibilitychange', update);
    return () => { window.clearInterval(timer); window.removeEventListener('focus', update); window.removeEventListener('daw:data-changed', update); document.removeEventListener('visibilitychange', update); };
  }, [enabled]);
}
