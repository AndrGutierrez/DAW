import { TableScroll } from './TableScroll';
import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { useResource } from '../api/useResource';
import { date, number } from '../api/livestock';
import { CarePagination } from './CareForm';
import { Button } from './ui/Controls';

type GrowthAlert = { animalId: string; farmId: string; tag: string; name: string | null; previousDate: string; currentDate: string; previousWeight: number; currentWeight: number; dailyGainKg: number; targetDailyGainKg: number | null; targetSource: string; reason: string };
type Alerts = { items: GrowthAlert[]; total: number; page: number; pageSize: number; activeBovines: number; insufficientMeasurements: number };
export function GrowthAlerts({ farmId = '', revision = 0 }: { farmId?: string; revision?: number }) {
  const { can } = useAuth(); const allowed = can('animals.list') && can('weights.list');
  const [page, setPage] = useState(1);
  const alerts = useResource<Alerts>('/api/alerts/growth?page=' + page + '&pageSize=10' + (farmId ? '&farmId=' + farmId : ''), allowed, true);
  useEffect(() => { setPage(1); alerts.reload(); }, [farmId, revision, alerts.reload]);
  useEffect(() => { if (!allowed) return; const timer = window.setInterval(() => { if (document.visibilityState === 'visible') alerts.reload(); }, 60000); return () => window.clearInterval(timer); }, [allowed, alerts.reload]);
  if (!allowed) return null;
  return <article className="panel growth-alerts"><div className="section-heading"><div><h2>Animales que requieren revisión</h2><p className="muted">Avisos actuales según los últimos pesajes y objetivos guardados.</p></div><Button onClick={alerts.reload} disabled={alerts.loading}>Actualizar avisos</Button></div>
    {alerts.loading && <p role="status">Consultando rendimiento…</p>}{alerts.error && <p className="error-banner" role="alert">{alerts.error}</p>}
    {alerts.data && <><p className="muted">{alerts.data.total} avisos · {alerts.data.activeBovines} bovinos activos · {alerts.data.insufficientMeasurements} sin dos fechas de pesaje comparables.</p>{alerts.data.items.length ? <TableScroll className="table-scroll"><table aria-label="Alertas de crecimiento"><thead><tr><th>Animal</th><th>Intervalo registrado</th><th>GDP</th><th>Objetivo</th><th>Motivo</th></tr></thead><tbody>{alerts.data.items.map(a => <tr key={a.animalId}><td><Link to={'/animals/' + a.animalId + '?tab=growth'}>{a.name || a.tag}</Link><small>{a.tag}</small></td><td>{date(a.previousDate)} a {date(a.currentDate)}<small>{number(a.previousWeight)} → {number(a.currentWeight)} kg</small></td><td>{number(a.dailyGainKg, 4)} kg/día</td><td>{a.targetDailyGainKg === null ? 'Sin objetivo' : number(a.targetDailyGainKg, 4) + ' kg/día'}<small>{a.targetSource === 'animal' ? 'Individual' : a.targetSource === 'farm' ? 'De la finca' : ''}</small></td><td><span className={'tag ' + (a.reason === 'weight-loss' ? 'stock-high' : 'stock-low')}>{a.reason === 'weight-loss' ? 'Descenso de peso' : 'Bajo objetivo'}</span></td></tr>)}</tbody></table></TableScroll> : <p>No hay avisos de bajo rendimiento con los registros y objetivos disponibles.</p>}{alerts.data.total > 0 && <CarePagination {...alerts.data} onPage={setPage}/>}</>}
  </article>;
}
