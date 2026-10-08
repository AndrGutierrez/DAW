import { Button } from './ui/Controls';
import { useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { useAuth } from '../auth/AuthContext';
import { ApiError } from '../auth/session';
import { errorMessage, fieldErrors } from '../api/errors';
import type { FieldErrors } from '../api/errors';
import { useFeedback } from './Feedback';

export function useCareSubmission(path: string, onSaved: () => void) {
  const { request } = useAuth();
  const { notify } = useFeedback();
  const [busy, setBusy] = useState(false);
  const [uncertain, setUncertain] = useState(false);
  const [error, setError] = useState('');
  const [errors, setErrors] = useState<FieldErrors>({});
  const pending = useRef<{ key: string; id: string; data: Record<string, unknown> } | null>(null);
  const inFlight = useRef(false);
  async function submit(event: FormEvent<HTMLFormElement>, data: Record<string, unknown>) {
    event.preventDefault(); if (inFlight.current) return;
    const payload = uncertain && pending.current ? pending.current.data : data;
    const key = JSON.stringify(payload);
    if (!pending.current || pending.current.key !== key) pending.current = { key, id: crypto.randomUUID(), data: payload };
    inFlight.current = true; setBusy(true); setError(''); setErrors({});
    try {
      const result = await request<{ replayed: boolean }>(path, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ submissionId: pending.current.id, ...pending.current.data }) });
      setUncertain(false); pending.current = null; notify(result.replayed ? 'Registro confirmado; ya estaba guardado.' : 'Registro guardado.');
      onSaved();
    } catch (failure) {
      setUncertain(!(failure instanceof ApiError && failure.status < 500));
      setError(errorMessage(failure)); setErrors(fieldErrors(failure)); notify(errorMessage(failure), 'error');
    } finally { inFlight.current = false; setBusy(false); }
  }
  return { busy, uncertain, error, errors, submit };
}
export function CareFormResult({ state, label }: { state: ReturnType<typeof useCareSubmission>; label: string }) {
  return <>{state.error && <p className="error-banner" role="alert">{state.error}</p>}{state.uncertain && <p className="warning-banner">Conservamos este envío. Reintentar confirma el mismo registro y evita duplicarlo.</p>}
    <div className="button-row"><Button type="submit" className="button primary" disabled={state.busy}>{state.busy ? 'Guardando…' : state.uncertain ? 'Reintentar y confirmar registro' : label}</Button></div></>;
}
export function CarePagination({ page, total, pageSize, onPage }: { page: number; total: number; pageSize: number; onPage: (page: number) => void }) {
  return <div className="pagination"><small className="muted">{total} registros · página {page} de {Math.max(1, Math.ceil(total / pageSize))}</small><div>
    <Button className="button secondary" disabled={page <= 1} onClick={() => onPage(page - 1)}>Anterior</Button>
    <Button className="button secondary" disabled={page * pageSize >= total} onClick={() => onPage(page + 1)}>Siguiente</Button>
  </div></div>;
}
