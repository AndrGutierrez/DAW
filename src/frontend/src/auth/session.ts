export type User = {
  id: string;
  username: string;
  email: string;
  fullName: string;
  isSuperuser: boolean;
  roles: string[];
  permissions: string[];
};

export type Session = { accessToken: string; accessTokenExpiresAt: string; user: User };
export type AuthState = {
  status: 'checking' | 'authenticated' | 'anonymous' | 'unavailable';
  session: Session | null;
  error: string | null;
};
export type Problem = { status?: number; title?: string; detail?: string; errors?: Record<string, string[]> };

export class ApiError extends Error {
  constructor(public status: number, public problem: Problem) {
    super(problem.detail || problem.title || 'No fue posible completar la solicitud.');
  }
}

type Fetcher = typeof fetch;
type SessionLock = <T>(action: () => Promise<T>) => Promise<T>;

export class SessionClient {
  private state: AuthState = { status: 'checking', session: null, error: null };
  private listeners = new Set<() => void>();
  private csrf: Promise<string> | null = null;
  private pendingRefresh: Promise<Session> | null = null;
  private pendingInit: Promise<void> | null = null;
  private version = 0;

  constructor(
    private fetcher: Fetcher = fetch.bind(globalThis),
    private withLock: SessionLock = action => action(),
    private broadcastLogout: () => void = () => {},
  ) {}

  getSnapshot = () => this.state;
  subscribe = (listener: () => void) => {
    this.listeners.add(listener);
    return () => { this.listeners.delete(listener); };
  };

  private publish(state: AuthState) {
    this.state = state;
    this.listeners.forEach(listener => listener());
  }

  private async read<T>(response: Response, responseType: "json" | "blob" = "json"): Promise<T> {
    if (!response.ok) {
      const problem = await response.json().catch(() => ({ title: 'Error de comunicación' })) as Problem;
      throw new ApiError(response.status, problem);
    }
    if (response.status === 204) return undefined as T;
    return (responseType === "blob" ? response.blob() : response.json()) as Promise<T>;
  }

  private getCsrf(force = false): Promise<string> {
    if (force) this.csrf = null;
    if (!this.csrf) {
      this.csrf = this.fetcher('/api/auth/session/csrf', {
        credentials: 'same-origin', cache: 'no-store',
      }).then(response => this.read<{ requestToken: string }>(response))
        .then(value => value.requestToken)
        .catch(error => { this.csrf = null; throw error; });
    }
    return this.csrf;
  }

  private async sessionRequest<T>(action: string, body?: object, retry = true): Promise<T> {
    const csrf = await this.getCsrf();
    const response = await this.fetcher('/api/auth/session/' + action, {
      method: 'POST',
      credentials: 'same-origin',
      cache: 'no-store',
      headers: { 'X-CSRF-TOKEN': csrf, ...(body ? { 'Content-Type': 'application/json' } : {}) },
      body: body ? JSON.stringify(body) : undefined,
    });
    try { return await this.read<T>(response); }
    catch (error) {
      if (retry && error instanceof ApiError && error.status === 400 && /csrf/i.test(error.message)) {
        await this.getCsrf(true);
        return this.sessionRequest<T>(action, body, false);
      }
      throw error;
    }
  }

  initialize = (): Promise<void> => {
    if (!this.pendingInit) {
      this.pendingInit = this.refresh().then(() => {}, error => {
        if (!(error instanceof ApiError && error.status === 401) && !(error instanceof DOMException && error.name === "AbortError")) {
          this.publish({ status: 'unavailable', session: null, error: 'No se pudo comprobar la sesión. Revisa la conexión e inténtalo de nuevo.' });
          this.pendingInit = null;
        }
      });
    }
    return this.pendingInit;
  };

  login = async (username: string, password: string) => {
    const session = await this.withLock(() => this.sessionRequest<Session>('login', { username, password }));
    this.version++;
    this.publish({ status: 'authenticated', session, error: null });
  };

  refresh = (): Promise<Session> => {
    if (!this.pendingRefresh) {
      const version = this.version;
      this.pendingRefresh = this.withLock(() => this.sessionRequest<Session>('refresh'))
        .then(session => {
          if (version !== this.version) throw new DOMException('Session changed.', 'AbortError');
          this.publish({ status: 'authenticated', session, error: null });
          return session;
        })
        .catch(error => {
          if (version === this.version && error instanceof ApiError && error.status === 401)
            this.clearLocalSession();
          throw error;
        })
        .finally(() => { this.pendingRefresh = null; });
    }
    return this.pendingRefresh;
  };

  logout = async () => {
    await this.withLock(() => this.sessionRequest<void>('logout'));
    this.clearLocalSession();
    this.broadcastLogout();
  };

  clearLocalSession = () => {
    this.version++;
    this.publish({ status: 'anonymous', session: null, error: null });
  };

  request = async <T>(path: string, init: RequestInit = {}, retry = true, responseType: "json" | "blob" = "json"): Promise<T> => {
    if (!path.startsWith('/api/') || path.startsWith('/api/auth/session/'))
      throw new Error('Authenticated requests must target the same-origin resource API.');
    let session = this.state.session;
    if (!session) throw new ApiError(401, { detail: 'Inicia sesión para continuar.' });
    const version = this.version;
    if (Date.parse(session.accessTokenExpiresAt) <= Date.now() + 15_000)
      session = await this.refresh();
    const headers = new Headers(init.headers);
    headers.set('Authorization', 'Bearer ' + session.accessToken);
    const response = await this.fetcher(path, { ...init, headers, credentials: 'same-origin', cache: 'no-store' });
    if (version !== this.version) throw new DOMException('Session changed.', 'AbortError');
    if (response.status === 401 && retry) {
      if (this.state.session?.accessToken === session.accessToken) await this.refresh();
      return this.request<T>(path, init, false, responseType);
    }
    if (response.status === 401) this.clearLocalSession();
    return this.read<T>(response, responseType);
  };
}
