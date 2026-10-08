import { useEffect, useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { ApiError } from '../auth/session';
import { consumeAnalyticsEvents } from './liveAnalytics';
export function useAnalyticsLive(enabled: boolean, reload: () => void) {
  const { request } = useAuth(); const [state, setState] = useState<'connecting' | 'live' | 'reconnecting' | 'paused' | 'unavailable'>('connecting');
  useEffect(() => {
    if (!enabled) return;
    let stopped = false, attempt = 0; let connection: AbortController | null = null;
    let retryTimer: ReturnType<typeof setTimeout> | undefined, changeTimer: ReturnType<typeof setTimeout> | undefined;
    const changed = () => { if (!changeTimer) changeTimer = setTimeout(() => { changeTimer = undefined; if (!stopped) reload(); }, 200); };
    const connect = async () => {
      if (stopped || document.visibilityState !== 'visible') return;
      connection = new AbortController(); const signal = connection.signal;
      setState(attempt ? 'reconnecting' : 'connecting');
      try {
        const response = await request<Response>('/api/analytics/changes', { signal, headers: { Accept: 'text/event-stream' } }, true, 'stream');
        await consumeAnalyticsEvents(response, changed, () => { attempt = 0; if (!stopped) { setState('live'); changed(); } });
      } catch (error) {
        if (stopped || signal.aborted) return;
        if (error instanceof ApiError && (error.status === 401 || error.status === 403)) { setState('unavailable'); return; }
      }
      if (!stopped && !signal.aborted) { setState('reconnecting'); retryTimer = setTimeout(() => { void connect(); }, Math.min(1000 * 2 ** attempt++, 30000)); }
    };
    const visibility = () => {
      connection?.abort(); clearTimeout(retryTimer);
      if (document.visibilityState === 'visible') { reload(); attempt = 0; void connect(); } else setState('paused');
    };
    document.addEventListener('visibilitychange', visibility);
    const fallback = setInterval(() => { if (document.visibilityState === 'visible') reload(); }, 60000);
    if (document.visibilityState === 'visible') void connect(); else setState('paused');
    return () => { stopped = true; connection?.abort(); clearTimeout(retryTimer); clearTimeout(changeTimer); clearInterval(fallback); document.removeEventListener('visibilitychange', visibility); };
  }, [enabled, request, reload]);
  return state;
}
