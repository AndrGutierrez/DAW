import { useState } from 'react';
import type { FormEvent } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { useResource } from '../api/useResource';
import type { CatalogItem } from '../api/livestock';
import { errorMessage } from '../api/errors';
import { GrowthAlerts } from '../components/GrowthAlerts';
import { Field } from '../components/Field';
import { Button, Input, Select } from '../components/ui/Controls';
import { useFeedback } from '../components/Feedback';

type Goal = { dailyGainKg: number | null; source: string };
function FarmGoal({ farmId, onSaved }: { farmId: string; onSaved: () => void }) {
  const { request, can, isAdmin } = useAuth(); const { notify } = useFeedback();
  const goal = useResource<Goal>('/api/farms/' + farmId + '/growth-policy', can('farms.get'));
  const [busy, setBusy] = useState(false), [error, setError] = useState('');
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); const form = new FormData(event.currentTarget); const value = String(form.get('goal') || '').trim(); setBusy(true); setError('');
    try { await request('/api/farms/' + farmId + '/growth-policy', { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ dailyGainKg: value === '' ? null : Number(value) }) }); notify('Objetivo de la finca guardado.'); goal.reload(); onSaved(); }
    catch (failure) { setError(errorMessage(failure)); } finally { setBusy(false); }
  }
  return <article className="panel"><h2>Objetivo de crecimiento de la finca</h2><p className="muted">Se aplica a animales sin objetivo individual. Un campo vacío desactiva el umbral; el descenso de peso sigue generando avisos. Define el valor según la edad, alimentación y propósito del ganado.</p>
    {goal.loading ? <p role="status">Cargando objetivo…</p> : goal.error ? <p role="alert">{goal.error}</p> : goal.data && <form className="filter-bar" key={String(goal.data.dailyGainKg)} onSubmit={save}><Field label="GDP mínima de la finca (kg/día)"><Input name="goal" type="number" min="0" max="1000" step="0.0001" defaultValue={goal.data.dailyGainKg ?? ''} disabled={busy || !isAdmin || !can('farms.update')}/></Field>{isAdmin && can('farms.update') && <Button type="submit" disabled={busy}>{busy ? 'Guardando…' : 'Guardar objetivo de finca'}</Button>}</form>}{error && <p className="error-banner" role="alert">{error}</p>}
  </article>;
}
export function MonitoringPage() {
  const { can } = useAuth(); const [params, setParams] = useSearchParams(); const farmId = params.get('farm') || '';
  const farms = useResource<CatalogItem[]>('/api/farms', can('farms.list')); const [revision, setRevision] = useState(0);
  if (!can('animals.list') || !can('weights.list')) return <p role="alert">Necesitas permisos de consulta de animales y pesajes.</p>;
  return <section className="operations-page"><div className="page-heading"><div><span className="eyebrow">DECISIONES CON REGISTROS</span><h1>Seguimiento</h1><p className="muted">Objetivos persistentes y avisos automáticos para revisar el rendimiento de tus bovinos.</p></div></div><div className="panel filter-bar"><Field label="Finca del seguimiento"><Select value={farmId} onChange={e => setParams(e.target.value ? { farm: e.target.value } : {})}><option value="">Todas las fincas asignadas</option>{farms.data?.map(f => <option key={f.id} value={f.id}>{f.data.name}</option>)}</Select></Field></div>{farms.error && <p role="alert">{farms.error}</p>}{farmId ? <FarmGoal key={farmId} farmId={farmId} onSaved={() => setRevision(r => r + 1)}/> : <p className="muted">Selecciona una finca para consultar o configurar su objetivo.</p>}<GrowthAlerts farmId={farmId} revision={revision}/></section>;
}
