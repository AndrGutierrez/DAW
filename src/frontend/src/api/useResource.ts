import { useEffect, useState } from 'react';
import { errorMessage } from './errors';
import { useAuth } from '../auth/AuthContext';

export function useResource<T>(path: string, enabled = true, preserveOnReload = false) {
  const { request } = useAuth();
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(enabled);
  const [error, setError] = useState('');
  const [revision, setRevision] = useState(0);
  useEffect(() => {
    if (!enabled) { setLoading(false); setData(null); return; }
    const controller = new AbortController();
    setLoading(true); if (!preserveOnReload) setData(null); setError('');
    void request<T>(path, { signal: controller.signal }).then(value => {
      if (!controller.signal.aborted) setData(value);
    }).catch(error => {
      if (!controller.signal.aborted && !(error instanceof DOMException && error.name === 'AbortError'))
        setError(errorMessage(error));
    }).finally(() => { if (!controller.signal.aborted) setLoading(false); });
    return () => controller.abort();
  }, [path, request, enabled, revision, preserveOnReload]);
  return { data, loading, error, reload: () => setRevision(value => value + 1) };
}
