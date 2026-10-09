import { useState } from 'react';
import type { FormEvent } from 'react';
import { useAuth } from '../auth/AuthContext';
import { useResource } from '../api/useResource';
import { errorMessage } from '../api/errors';
import { date, number } from '../api/livestock';
import { Field } from './Field';
import { SidePanel } from './SidePanel';
import { Button, Input } from './ui/Controls';
import { useFeedback } from './Feedback';
import { useUnsavedChanges } from './NavigationProtection';
const today = () => new Date(Date.now() - 4 * 3600000).toISOString().slice(0, 10);
type SyncStatus = { lastAttempt: string | null; lastSuccess: string | null; error: string | null; automaticEnabled: boolean };
type Quote = { id: string; effectiveDate: string; bolivarsPerDollar: number; recordedAt: string; source: string; entryMethod: string };
export function useValuationCurrency(enabled: boolean) {
  const { request, can } = useAuth(); const { notify, confirm } = useFeedback();
  const rate = useResource<Quote>('/api/exchange-rates/usd-ves?date=' + today(), enabled, true);
  const syncStatus = useResource<SyncStatus>('/api/exchange-rates/usd-ves/status', enabled);
  const [syncing, setSyncing] = useState(false), [syncError, setSyncError] = useState('');
  async function sync() {
    setSyncing(true); setSyncError('');
    try { await request('/api/exchange-rates/usd-ves/sync', { method: 'POST' }); rate.reload(); notify('Referencia BCV actualizada desde BCV Today.'); }
    catch (failure) { setSyncError(errorMessage(failure)); }
    finally { setSyncing(false); syncStatus.reload(); }
  }
  const [selected, setSelected] = useState<'USD' | 'VES'>('USD'); const [open, setOpen] = useState(false);
  const [effectiveDate, setEffectiveDate] = useState(today()), [value, setValue] = useState(''), [submission, setSubmission] = useState('');
  const [dirty, setDirty] = useState(false), [busy, setBusy] = useState(false), [error, setError] = useState('');
  const clearUnsaved = useUnsavedChanges(dirty);
  const currency = selected === 'VES' && rate.data ? 'VES' : 'USD';
  const factor = currency === 'VES' ? rate.data!.bolivarsPerDollar : 1;
  const formatter = new Intl.NumberFormat('es-VE', { style: 'currency', currency, minimumFractionDigits: 2, maximumFractionDigits: 2 });
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setBusy(true); setError('');
    try {
      await request('/api/exchange-rates/usd-ves', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ submissionId: submission, effectiveDate, bolivarsPerDollar: Number(value) }) });
      notify('Referencia BCV guardada.'); clearUnsaved(); rate.reload(); setDirty(false); setOpen(false);
    } catch (failure) { setError(errorMessage(failure)); } finally { setBusy(false); }
  }
  async function close() { if (!busy && (!dirty || await confirm('¿Descartar los cambios de la referencia BCV?'))) { clearUnsaved(); setDirty(false); setOpen(false); } }
  const controls = <div className="panel"><div className="section-heading"><div><h3>Moneda de valoración</h3><p className="muted">Los precios del catálogo se conservan en USD. La vista en bolívares convierte el saldo actual con la referencia fechada.</p></div><div className="toolbar"><Button aria-pressed={currency === 'USD'} onClick={() => setSelected('USD')}>USD</Button><Button aria-pressed={currency === 'VES'} disabled={!rate.data} onClick={() => setSelected('VES')}>Bs</Button>{can('products.update') && <Button variant="secondary" disabled={syncing} onClick={() => void sync()}>{syncing ? 'Actualizando…' : 'Actualizar desde BCV Today'}</Button>}{can('products.update') && <Button onClick={() => { setEffectiveDate(today()); setValue(''); setSubmission(crypto.randomUUID()); setDirty(false); setError(''); setOpen(true); }}>Registrar tasa BCV</Button>}</div></div>
    {rate.data ? <p className="muted">1 USD = {number(rate.data.bolivarsPerDollar, 6)} Bs · fecha valor: {date(rate.data.effectiveDate)} · {rate.data.entryMethod === 'automatic' ? 'consulta automática mediante BCV Today' : 'registro manual'}. <a href={rate.data.source} target="_blank" rel="noreferrer">Ver fuente de referencia</a></p> : <p className="muted">{rate.loading ? 'Consultando referencia cambiaria…' : 'Sin tasa BCV registrada para esta fecha. La valoración permanece en USD.'}</p>}{rate.error && <p role="alert" className="error-banner">{rate.error}</p>}
    {syncError && <p role="alert" className="error-banner">{syncError}</p>}{syncStatus.data?.error && !syncError && <p className="warning-banner">La actualización automática no está disponible. Se conserva la última referencia registrada.</p>}
    {syncStatus.data?.lastSuccess && <p className="muted">Última consulta correcta: {new Date(syncStatus.data.lastSuccess).toLocaleString('es-VE')}.</p>}
    {open && <SidePanel title="Registrar referencia BCV" onRequestClose={() => { void close(); }} eyebrow="VALORACIÓN FECHADA" closeLabel="Cerrar referencia BCV">
      <p>Consulta la publicación del <a href="https://www.bcv.org.ve/" target="_blank" rel="noreferrer">Banco Central de Venezuela</a> y transcribe el valor USD y su fecha valor. Este formulario permite registrar una referencia manual cuando necesites corregir el dato o el proveedor automático no esté disponible.</p>
      <form onSubmit={save} className="editor-form"><Field label="Fecha valor BCV"><Input type="date" required max={new Date(Date.now() + 14 * 86400000).toISOString().slice(0, 10)} value={effectiveDate} onChange={e => { setEffectiveDate(e.target.value); setSubmission(crypto.randomUUID()); setDirty(true); }} disabled={busy}/></Field><Field label="Bolívares por dólar" hint="Hasta 6 decimales. Una fecha futura se aplicará cuando llegue su fecha valor."><Input type="number" required min="0.000001" max="999999999.999999" step="0.000001" value={value} onChange={e => { setValue(e.target.value); setSubmission(crypto.randomUUID()); setDirty(true); }} disabled={busy}/></Field>{error && <p role="alert" className="error-banner">{error}</p>}<Button type="submit" disabled={busy}>{busy ? 'Guardando…' : 'Guardar referencia BCV'}</Button></form>
    </SidePanel>}
  </div>;
  return { controls, unit: currency === 'VES' ? 'Bs' : 'USD', formatMoney: (amount: number) => formatter.format(amount * factor) };
}
