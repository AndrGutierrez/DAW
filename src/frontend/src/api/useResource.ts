import { useCallback, useEffect, useState } from 'react';
import { errorMessage } from './errors';
import { useAuth } from '../auth/AuthContext';
import { recordTiming, serviceName } from '../performance/store';

export function useResource<T>(path: string, enabled = true, preserveOnReload = false) {
  const { request } = useAuth();
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(enabled);
  const [error, setError] = useState('');
  const [revision, setRevision] = useState(0);
  useEffect(() => {
    if (!enabled) { setLoading(false); setData(null); return; }
    const controller = new AbortController();
    const started = performance.now();
    let outcome: 'success' | 'error' | 'cancelled' = 'success';
    setLoading(true); if (!preserveOnReload) setData(null); setError('');
    void request<T>(path, { signal: controller.signal }).then(value => {
      if (!controller.signal.aborted) setData(value);
    }).catch(error => {
      outcome = controller.signal.aborted ? 'cancelled' : 'error';
      if (!controller.signal.aborted && !(error instanceof DOMException && error.name === 'AbortError'))
        setError(errorMessage(error));
    }).finally(() => {
      recordTiming({ service: serviceName(path), kind: 'data', duration: performance.now() - started, status: null, outcome: controller.signal.aborted ? 'cancelled' : outcome });
      if (!controller.signal.aborted) setLoading(false);
    });
    return () => controller.abort();
  }, [path, request, enabled, revision, preserveOnReload]);
  const reload = useCallback(() => setRevision(value => value + 1), []);
  return { data, loading, error, reload };
}
