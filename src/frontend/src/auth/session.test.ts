import { describe, it, expect, vi } from 'vitest';
import { ApiError, SessionClient } from './session';
import type { Session } from './session';

const session = (token = 'access-1'): Session => ({
  accessToken: token, accessTokenExpiresAt: new Date(Date.now() + 300_000).toISOString(),
  user: { id: 'user', username: 'admin', email: 'admin@example.test', fullName: 'Admin', isSuperuser: true, roles: ['Admin'], permissions: ['animals.list'] },
});
const json = (body: unknown, status = 200) => new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
const fetchQueue = (...responses: Response[]) => vi.fn<typeof fetch>().mockImplementation(async () => {
  const response = responses.shift();
  if (!response) throw new Error('Unexpected request.');
  return response;
});

describe('SessionClient', () => {
  it('restores once when startup runs twice and never sends a refresh token from JavaScript', async () => {
    const fetcher = fetchQueue(json({ requestToken: 'csrf' }), json(session()));
    const client = new SessionClient(fetcher);
    await Promise.all([client.initialize(), client.initialize()]);
    expect(fetcher).toHaveBeenCalledTimes(2);
    expect(client.getSnapshot().status).toBe('authenticated');
    expect(fetcher.mock.calls[1][1]).toMatchObject({ headers: { 'X-CSRF-TOKEN': 'csrf' }, credentials: 'same-origin' });
    expect(fetcher.mock.calls[1][1]?.body).toBeUndefined();
  });

  it('starts anonymously when no cookie exists', async () => {
    const client = new SessionClient(fetchQueue(json({ requestToken: 'csrf' }), json({ detail: 'No session.' }, 401)));
    await client.initialize();
    expect(client.getSnapshot()).toMatchObject({ status: 'anonymous', session: null });
  });

  it('distinguishes an unavailable API from an expired session and permits retry', async () => {
    const fetcher = fetchQueue(json({ detail: 'Unavailable' }, 503), json({ requestToken: 'csrf' }), json(session()));
    const client = new SessionClient(fetcher);
    await client.initialize();
    expect(client.getSnapshot().status).toBe('unavailable');
    await client.initialize();
    expect(client.getSnapshot().status).toBe('authenticated');
  });

  it('does not retry invalid credentials or publish a session', async () => {
    const fetcher = fetchQueue(json({ requestToken: 'csrf' }), json({ detail: 'Invalid credentials' }, 401));
    const client = new SessionClient(fetcher);
    await expect(client.login('admin', 'wrong')).rejects.toBeInstanceOf(ApiError);
    expect(client.getSnapshot().session).toBeNull();
    expect(fetcher).toHaveBeenCalledTimes(2);
  });

  it('retries a stale CSRF token once after fetching a matching token', async () => {
    const fetcher = fetchQueue(
      json({ requestToken: 'old' }), json({ detail: 'A valid CSRF token is required.' }, 400),
      json({ requestToken: 'new' }), json(session()),
    );
    const client = new SessionClient(fetcher);
    await client.login('admin', 'password');
    expect(fetcher.mock.calls[3][1]?.headers).toMatchObject({ 'X-CSRF-TOKEN': 'new' });
  });

  it('shares one rotation between concurrent refresh requests', async () => {
    const fetcher = fetchQueue(json({ requestToken: 'csrf' }), json(session()));
    const client = new SessionClient(fetcher);
    const [first, second] = await Promise.all([client.refresh(), client.refresh()]);
    expect(first).toBe(second);
    expect(fetcher).toHaveBeenCalledTimes(2);
  });

  it('renews an access token before expiry and attaches the new bearer', async () => {
    const expired = { ...session(), accessTokenExpiresAt: new Date(Date.now() - 1000).toISOString() };
    const fetcher = fetchQueue(json({ requestToken: 'csrf' }), json(expired), json(session('new')), json([]));
    const client = new SessionClient(fetcher);
    await client.initialize();
    await client.request('/api/animals');
    expect(new Headers(fetcher.mock.calls[3][1]?.headers).get('Authorization')).toBe('Bearer new');
  });

  it('retries a resource 401 once with a rotated token', async () => {
    const fetcher = fetchQueue(json({ requestToken: 'csrf' }), json(session()), json({}, 401), json(session('new')), json(['animal']));
    const client = new SessionClient(fetcher);
    await client.initialize();
    expect(await client.request('/api/animals')).toEqual(['animal']);
    expect(fetcher).toHaveBeenCalledTimes(5);
  });

  it('keeps authorization failures separate from session renewal', async () => {
    const fetcher = fetchQueue(json({ requestToken: 'csrf' }), json(session()), json({ detail: 'Forbidden' }, 403));
    const client = new SessionClient(fetcher);
    await client.initialize();
    await expect(client.request('/api/animals')).rejects.toMatchObject({ status: 403 });
    expect(fetcher).toHaveBeenCalledTimes(3);
    expect(client.getSnapshot().status).toBe('authenticated');
  });

  it('waits for server revocation before announcing successful logout', async () => {
    const fetcher = fetchQueue(json({ requestToken: 'csrf' }), json(session()), json({ detail: 'Unavailable' }, 503));
    const broadcast = vi.fn();
    const client = new SessionClient(fetcher, action => action(), broadcast);
    await client.initialize();
    await expect(client.logout()).rejects.toMatchObject({ status: 503 });
    expect(client.getSnapshot().status).toBe('authenticated');
    expect(broadcast).not.toHaveBeenCalled();
  });

  it('clears memory and broadcasts only after logout succeeds', async () => {
    const fetcher = fetchQueue(json({ requestToken: 'csrf' }), json(session()), new Response(null, { status: 204 }));
    const broadcast = vi.fn();
    const client = new SessionClient(fetcher, action => action(), broadcast);
    await client.initialize();
    await client.logout();
    expect(client.getSnapshot()).toMatchObject({ status: 'anonymous', session: null });
    expect(broadcast).toHaveBeenCalledOnce();
  });

  it('does not restore an in-flight session after another tab logs out', async () => {
    let resolve!: (response: Response) => void;
    const fetcher = vi.fn<typeof fetch>().mockResolvedValueOnce(json({ requestToken: 'csrf' }))
      .mockImplementationOnce(() => new Promise<Response>(done => { resolve = done; }));
    const client = new SessionClient(fetcher);
    const initializing = client.initialize();
    await vi.waitFor(() => expect(resolve).toBeDefined());
    client.clearLocalSession();
    resolve(json(session()));
    await initializing;
    expect(client.getSnapshot().status).toBe('anonymous');
  });

  it('refuses to send bearer tokens to an external origin', async () => {
    const fetcher = fetchQueue(json({ requestToken: 'csrf' }), json(session()));
    const client = new SessionClient(fetcher);
    await client.initialize();
    await expect(client.request('https://example.test/api/animals')).rejects.toThrow('same-origin');
    expect(fetcher).toHaveBeenCalledTimes(2);
  });
});
