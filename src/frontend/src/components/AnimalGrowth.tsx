import { TableScroll } from './TableScroll';
import { Button, Input } from './ui/Controls';
import { useFeedback } from './Feedback';
import { errorMessage } from '../api/errors';
import { StatusNotice } from './ui/StatusNotice';
import { lazy, Suspense, useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { useResource } from '../api/useResource';
import { date, kg, number } from '../api/livestock';
import type { GrowthResult } from '../api/livestock';
const GrowthChart = lazy(() => import('./GrowthChart').then(module => ({ default: module.GrowthChart })));
export function AnimalGrowth({ animalId }: { animalId: string }) {
  const { can, request } = useAuth();
  const { notify } = useFeedback();
  const [saving, setSaving] = useState(false), [saveError, setSaveError] = useState('');
  const [page, setPage] = useState(1);
  const { data, loading, error, reload } = useResource<GrowthResult>('/api/animals/' + animalId + '/growth?page=' + page + '&pageSize=20', can('weights.list'), true);
  const [target, setTarget] = useState('');
  async function saveGoal(inherit = false) {
    setSaving(true); setSaveError('');
    try { await request('/api/animals/' + animalId + '/growth-goal', { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ dailyGainKg: inherit || target === '' ? null : Number(target) }) }); setTarget(''); reload(); notify('Objetivo del animal guardado.'); }
    catch (failure) { setSaveError(errorMessage(failure)); } finally { setSaving(false); }
  }
  if (!can('weights.list')) return <section id="growth" className="panel animal-details"><h2>Crecimiento</h2><p className="muted">Necesitas permiso para consultar los pesajes.</p></section>;
  const latest = data?.points.at(-1);
  const gain = latest?.dailyGainKg;
  const previous = data?.points.at(-2);
  const weightLoss = latest && previous && latest.weightKg < previous.weightKg;
  const effectiveTarget = target !== '' ? Number(target) : data?.targetDailyGainKg;
  const rawGain = latest && previous ? (latest.weightKg - previous.weightKg) / ((Date.parse(latest.date) - Date.parse(previous.date)) / 86400000) : null;
  const belowTarget = effectiveTarget != null && Number.isFinite(effectiveTarget) && effectiveTarget >= 0 && rawGain != null && rawGain < effectiveTarget;
  return <section className="panel animal-details" id="growth"><div className="section-heading"><div><h2>Crecimiento e historial de peso</h2><p className="muted">Seguimiento del animal a partir de pesajes registrados.</p></div>{gain != null && <div className="metric-inline"><strong>{number(gain, 4)}</strong><span>kg/día · última GDP</span></div>}</div>
    {loading && !data ? <div className="skeleton chart-skeleton" role="status" aria-label="Cargando pesajes" /> : error ? <div role="alert"><p>{error}</p><Button className="button secondary" onClick={reload}>Reintentar</Button></div> : data && <>
      {loading && <p role="status" className="muted">Actualizando historial…</p>}<Suspense fallback={<div className="skeleton chart-skeleton" role="status" aria-label="Preparando gráfica" />}><GrowthChart points={data.points} totalDates={data.totalDates} /></Suspense><div className="gain-target"><label htmlFor="gain-target">Objetivo de GDP (kg/día)</label><Input id="gain-target" type="number" step="0.01" min="0" max="1000" value={target} onChange={event => setTarget(event.target.value)} placeholder="Sin objetivo definido" /><small className="muted">Objetivo guardado: {data.targetDailyGainKg === null ? 'sin definir' : number(data.targetDailyGainKg, 4) + ' kg/día'}{data.targetSource === 'farm' ? ' · heredado de la finca' : data.targetSource === 'animal' ? ' · individual' : ''}. Cambiar el campo permite comparar antes de guardar.</small>{can('animals.update') && <div className="button-row"><Button disabled={saving || target === '' || !Number.isFinite(Number(target)) || Number(target) < 0 || Number(target) > 1000} onClick={() => void saveGoal()}>Guardar objetivo individual</Button><Button disabled={saving} onClick={() => void saveGoal(true)}>Usar objetivo de finca</Button></div>}{saveError && <p role="alert">{saveError}</p>}</div>
      {weightLoss && <StatusNotice title="Descenso de peso detectado" kind="warning"><p>El peso disminuyó {kg(previous.weightKg - latest.weightKg)} entre {date(previous.date)} y {date(latest.date)}. Revisa las mediciones y las condiciones del animal.</p></StatusNotice>}
      {belowTarget && !weightLoss && <p className="warning-banner" role="status">La última GDP está por debajo del objetivo de {number(effectiveTarget!, 4)} kg/día. Revisa el intervalo y los registros antes de interpretar el resultado.</p>}
      <div className="section-heading history-heading"><h3>Pesajes registrados</h3><span className="tag">{data.total} registros</span></div>
      {!data.records.length ? <p className="muted">Aún no hay pesajes registrados.</p> : <><TableScroll className="table-scroll" aria-busy={loading}><table><caption className="sr-only">Historial completo de pesajes del animal</caption><thead><tr><th>Fecha</th><th>Peso</th><th>Condición corporal</th><th>Observaciones</th></tr></thead><tbody>{data.records.map(record => <tr key={record.id}><td>{date(record.date)}<small>{record.usedForCurve ? 'Peso seleccionado del día' : 'Otro pesaje del mismo día'}</small></td><td className="numeric-cell">{kg(record.weightKg)}</td><td>{record.bodyConditionScore === null ? '—' : number(record.bodyConditionScore)}</td><td>{record.notes || '—'}</td></tr>)}</tbody></table></TableScroll>{data.total > data.pageSize && <div className="pagination"><span className="muted">Página {data.page} de {Math.ceil(data.total / data.pageSize)}</span><div><Button className="button secondary" disabled={loading || data.page <= 1} onClick={() => setPage(data.page - 1)}>Anterior</Button><Button className="button secondary" disabled={loading || data.page * data.pageSize >= data.total} onClick={() => setPage(data.page + 1)}>Siguiente</Button></div></div>}</>}
    </>}
  </section>;
}
