import { useEffect, useState } from 'react';
import { useAuth } from '../auth/AuthContext';

export function useResource<T>(path: string, enabled = true) {
  const { request } = useAuth();
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(enabled);
  const [error, setError] = useState('');
  const [revision, setRevision] = useState(0);
  useEffect(() => {
    if (!enabled) { setLoading(false); setData(null); return; }
    const controller = new AbortController();
    setLoading(true); setData(null); setError('');
    void request<T>(path, { signal: controller.signal }).then(value => {
      if (!controller.signal.aborted) setData(value);
    }).catch(error => {
      if (!controller.signal.aborted && !(error instanceof DOMException && error.name === 'AbortError'))
        setError(error instanceof Error ? error.message : 'No se pudo cargar la información.');
    }).finally(() => { if (!controller.signal.aborted) setLoading(false); });
    return () => controller.abort();
  }, [path, request, enabled, revision]);
  return { data, loading, error, reload: () => setRevision(value => value + 1) };
}
