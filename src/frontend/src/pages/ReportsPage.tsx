import { TableScroll } from '../components/TableScroll';
import { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { useResource } from '../api/useResource';
import { errorMessage } from '../api/errors';
import { params, periodStart, units } from '../api/operations';
import type { ReportResult } from '../api/operations';
import { careText } from '../api/care';
import { today, date, number } from '../api/livestock';
import type { CatalogItem } from '../api/livestock';
import { PeriodFilter } from '../components/PeriodFilter';
import { CarePagination } from '../components/CareForm';
import { Button } from '../components/ui/Controls';
import { useFeedback } from '../components/Feedback';
export function ReportsPage() {
  const { can, request } = useAuth(); const { notify } = useFeedback();
  const clinical = can('clinical.list') && can('animals.list'); const production = can('production.list') && can('animals.list');
  const [search] = useSearchParams();
  const [kind, setKind] = useState<'clinical' | 'production'>(search.get('kind') === 'production' && production ? 'production' : clinical ? 'clinical' : 'production');
  const dateFilter = (key: string, fallback: string) => /^\d{4}-\d{2}-\d{2}$/.test(search.get(key) || '') ? search.get(key)! : fallback;
  const [period, setPeriod] = useState({ from: dateFilter('from', periodStart()), to: dateFilter('to', today()), farmId: search.get('farmId') || '' }); const [page, setPage] = useState(1); const [busy, setBusy] = useState(''); const [error, setError] = useState('');
  const data = useResource<ReportResult>('/api/reports/' + kind + '?' + params({ ...period, page, pageSize: 20 }), clinical || production);
  const farms = useResource<CatalogItem[]>('/api/farms', can('farms.list'));
  async function exportReport(format: string) {
    if (busy) return; setBusy(format); setError('');
    try { const snapshot = await request<ReportResult>('/api/reports/' + kind + '/export?' + params(period)); const exports = await import('../api/reportExport'); const info = { kind, ...period, scope: period.farmId ? farms.data?.find(f => f.id === period.farmId)?.data.name || 'Finca seleccionada' : 'Fincas autorizadas', generatedAt: snapshot.generatedAt }; await (format === 'xlsx' ? exports.exportXlsx : exports.exportPdf)(snapshot.records.items, info); notify('Reporte exportado con ' + snapshot.records.total + ' registros.'); } catch (failure) { setError(errorMessage(failure)); } finally { setBusy(''); }
  }
  if (!clinical && !production) return <p role="alert">No tienes permiso para consultar estos reportes.</p>;
  return <section className="operations-page"><span className="eyebrow">REGISTROS PARA SEGUIMIENTO</span><h1>Reportes</h1><p className="muted">Historial clínico y producción, con filtros por período y finca.</p><div className="button-row module-tabs">{clinical && <Button disabled={!!busy} aria-pressed={kind === 'clinical'} variant={kind === 'clinical' ? 'primary' : 'secondary'} onClick={() => { setKind('clinical'); setPage(1); }}>Historial clínico</Button>}{production && <Button disabled={!!busy} aria-pressed={kind === 'production'} variant={kind === 'production' ? 'primary' : 'secondary'} onClick={() => { setKind('production'); setPage(1); }}>Producción</Button>}</div><PeriodFilter value={period} farms={farms.data || []} onApply={p => { setPeriod(p); setPage(1); data.reload(); }} busy={data.loading || !!busy} />
    <div className="button-row"><Button disabled={!!busy || data.loading || !data.data || !!data.error} onClick={() => void exportReport('xlsx')}>{busy === 'xlsx' ? 'Preparando Excel…' : 'Exportar Excel'}</Button><Button disabled={!!busy || data.loading || !data.data || !!data.error} onClick={() => void exportReport('pdf')}>{busy === 'pdf' ? 'Preparando PDF…' : 'Exportar PDF'}</Button></div><p className="muted">Se exportan todos los registros de los filtros aplicados (hasta 10.000), desde una consulta consistente. El catálogo de productos es el actual. {kind === 'clinical' ? 'Las dosis conservan su valor, pero su unidad no está registrada. El retiro incluye la última fecha restringida.' : 'Cada registro conserva su unidad; no se suman unidades diferentes.'}</p>
    {(error || data.error) && <p className="error-banner" role="alert">{error || data.error}</p>}{data.loading && <p role="status">Consultando registros…</p>}{data.data && <article className="panel"><h2>{kind === 'clinical' ? 'Historial clínico' : 'Producción animal'}</h2><TableScroll className="table-scroll"><table><thead><tr><th>Fecha</th><th>Finca / animal</th><th>Registro</th><th>Producto / detalle</th><th>Cantidad</th><th>Retiro hasta</th><th>Observaciones</th></tr></thead><tbody>{data.data.records.items.map(row => <tr key={row.id}><td>{date(row.date)}</td><td>{row.farm}<small className="muted">{row.animal}</small></td><td>{careText(row.kind)}</td><td>{row.product || (row.detail ? careText(row.detail) : '—')}</td><td>{row.quantity === null ? '—' : number(row.quantity, 4) + ' ' + (row.unit ? units[row.unit] || careText(row.unit) : '')}</td><td>{row.withdrawalEndDate ? date(row.withdrawalEndDate) : '—'}</td><td className="report-notes">{row.notes || '—'}</td></tr>)}</tbody></table></TableScroll>{!data.data.records.total && <p className="muted">No hay registros para los filtros aplicados.</p>}<CarePagination {...data.data.records} onPage={setPage} /></article>}</section>;
}
