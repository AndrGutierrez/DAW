// @vitest-environment jsdom
import { act, renderHook, waitFor } from '@testing-library/react';
import { beforeEach, expect, it, vi } from 'vitest';
import { useResource } from './useResource';
const request = vi.hoisted(() => vi.fn());
vi.mock('../auth/AuthContext', () => ({ useAuth: () => ({ request }) }));
beforeEach(() => request.mockReset());
it('does not fetch a resource without permission', () => {
 const hook = renderHook(() => useResource('/api/animals', false));
 expect(request).not.toHaveBeenCalled(); expect(hook.result.current.loading).toBe(false);
});
it('loads decoded data and supports retry after a failed request', async () => {
 request.mockRejectedValueOnce(new Error('Connection failed')).mockResolvedValueOnce({ count: 3 });
 const hook = renderHook(() => useResource<{ count: number }>('/api/animals'));
 await waitFor(() => expect(hook.result.current.loading).toBe(false));
 expect(hook.result.current.error).not.toBe('');
 act(() => hook.result.current.reload());
 await waitFor(() => expect(hook.result.current.data).toEqual({ count: 3 }));
 expect(hook.result.current.error).toBe('');
});
it('aborts the previous query and ignores its late result after changing the path', async () => {
 let finish!: (value: string) => void;
 request.mockImplementationOnce(() => new Promise<string>(resolve => { finish = resolve; })).mockResolvedValueOnce('current');
 const hook = renderHook(({ path }) => useResource<string>(path), { initialProps: { path: '/api/animals/old' } });
 const signal = request.mock.calls[0][1].signal;
 hook.rerender({ path: '/api/animals/current' });
 expect(signal.aborted).toBe(true);
 await waitFor(() => expect(hook.result.current.data).toBe('current'));
 await act(async () => { finish('outdated'); });
 expect(hook.result.current.data).toBe('current');
});
it('keeps the displayed data during a background refresh', async () => {
 let finish!: (value: string) => void;
 request.mockResolvedValueOnce('previous').mockImplementationOnce(() => new Promise<string>(resolve => { finish = resolve; }));
 const hook = renderHook(() => useResource<string>('/api/animals', true, true));
 await waitFor(() => expect(hook.result.current.data).toBe('previous'));
 act(() => hook.result.current.reload()); expect(hook.result.current.data).toBe('previous');
 await act(async () => { finish('updated'); }); expect(hook.result.current.data).toBe('updated');
});
