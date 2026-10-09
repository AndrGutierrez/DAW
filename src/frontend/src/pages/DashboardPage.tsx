import { TableScroll } from '../components/TableScroll';
import { lazy, Suspense, useState, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { useResource } from '../api/useResource';
import { useAnalyticsLive } from '../api/useAnalyticsLive';
import { dashboardPermissions } from '../api/operations';
import { useValuationCurrency } from '../components/ValuationCurrency';
import { params, periodStart, units } from '../api/operations';
import type { Analytics, SeriesPoint } from '../api/operations';
import { today, date, number } from '../api/livestock';
import type { CatalogItem } from '../api/livestock';
import { GrowthAlerts } from '../components/GrowthAlerts';
import { DashboardFilters } from '../components/DashboardFilters';
import { ReproductionSummary } from '../components/ReproductionSummary';
import { CarePagination } from '../components/CareForm';
import { Button } from '../components/ui/Controls';
import { Icon } from '../components/ui/Icon';
import type { IconName } from '../components/ui/Icon';
const Charts = lazy(() => import('../components/AnalyticsCharts'));
function SeriesTable({ title, rows, unit }: { title: string; rows: SeriesPoint[]; unit: string }) {
  return <article className="panel"><h2>{title}</h2>{rows.length ? <TableScroll className="table-scroll"><table><thead><tr><th>Grupo</th><th>Valor ({unit})</th><th>Registros</th></tr></thead><tbody>{rows.map(r => <tr key={r.label}><td>{r.label}</td><td>{number(r.value)}</td><td>{r.count}</td></tr>)}</tbody></table></TableScroll> : <p className="muted">Sin registros comparables en el período.</p>}</article>;
}
function DashboardMetric({ label, shortLabel, value, detail, icon, href, action, attention = false }: { label: string; shortLabel?: string; value: string; detail: string; icon: IconName; href: string; action: string; attention?: boolean }) {
  return <article className={`panel dashboard-metric metric-${icon}${attention ? ' needs-attention' : ''}`} aria-label={label}>
    <div className="dashboard-metric-title"><span className="dashboard-metric-icon"><Icon name={icon} size={22} /></span><h2>{shortLabel || label}</h2></div>
    <strong className={"dashboard-metric-value" + (value.startsWith("Sin ") ? " is-empty" : "")}>{value}</strong><p className="muted">{detail}</p><Link to={href}>{action}<Icon name="arrow" size={16} /></Link>
  </article>;
}
export function DashboardPage() {
  const { isAdmin, can } = useAuth(); const allowed = isAdmin && dashboardPermissions.every(can);
  const [period, setPeriod] = useState({ from: periodStart(), to: today(), farmId: '' });
  const data = useResource<Analytics>('/api/analytics/overview?' + params(period), allowed, true);
  const [liveRevision, setLiveRevision] = useState(0);
  const refresh = useCallback(() => { data.reload(); setLiveRevision(v => v + 1); }, [data.reload]);
  const live = useAnalyticsLive(allowed, refresh);
  const [stockPage, setStockPage] = useState(1);
  const valuation = useValuationCurrency(allowed);
  const farms = useResource<CatalogItem[]>('/api/farms', allowed && can('farms.list'));
  if (!allowed) return <p role="alert">El dashboard requiere acceso de administrador y permisos para sus indicadores.</p>;
  const d = data.data;
  const currentStockPage = Math.min(stockPage, Math.max(1, Math.ceil((d?.stock.length || 0) / 20)));
  const milkTotal = d?.milkByDay.reduce((total, day) => total + day.value, 0) || 0;
  const milkRecords = d?.milkByDay.reduce((total, day) => total + day.count, 0) || 0;
  const weighedCattle = d?.weightByAge.reduce((total, group) => total + group.count, 0) || 0;
  return <section className="operations-page dashboard-page">
    <div className="dashboard-heading"><div><span className="eyebrow">TU FINCA, EN PERSPECTIVA</span><h1>Dashboard</h1><p className="muted">Producción y seguimiento del período.</p></div><Link className="button secondary dashboard-animals-link" to="/animals" aria-label="Consultar animales"><Icon name="animal" /><span className="desktop-label">Consultar animales</span><span className="mobile-label" aria-hidden="true">Animales</span></Link></div>
    <DashboardFilters value={period} farms={farms.data || []} onApply={p => { if (params(p) === params(period)) data.reload(); setPeriod(p); setStockPage(1); }} busy={data.loading} />
    {data.loading && !d && <div className="panel skeleton" style={{ minHeight: 200 }} role="status" aria-label="Cargando indicadores" />}
    {data.error && <p className="error-banner" role="alert">{data.error}<Button onClick={data.reload}>Reintentar</Button></p>}
    {d && <>
      <p className="muted dashboard-live-status">Actualizado: {new Intl.DateTimeFormat('es-VE', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(d.generatedAt))} · {live === 'live' ? 'Actualización en vivo conectada.' : live === 'paused' ? 'Actualización pausada mientras la página está oculta.' : 'Reconectando actualización en vivo; respaldo cada 60 segundos.'}</p>
      {data.loading && <p role="status" className="muted dashboard-live-status">Actualizando indicadores; se muestran los últimos datos disponibles.</p>}
      <div className="metric-grid dashboard-summary" aria-busy={data.loading}>
        <DashboardMetric label="Leche registrada" value={milkRecords ? number(milkTotal, 3) + ' L' : 'Sin registros'} detail={milkRecords + (milkRecords === 1 ? ' registro' : ' registros') + ' en litros en el período'} icon="droplet" href={'/reports?' + params({ ...period, kind: 'production' })} action="Consultar producción" />
        <DashboardMetric label="Bovinos con pesaje comparable" shortLabel="Bovinos pesados" value={number(weighedCattle, 0)} detail="Último pesaje del período · edad conocida" icon="scale" href={can('weights.create') ? '/weighing' : '/animals'} action={can('weights.create') ? 'Registrar pesajes' : 'Consultar fichas'} />
        <DashboardMetric label="Preñez en hembras evaluadas" shortLabel="Preñez observada" value={d.reproduction.pregnancyPercent === null ? 'Sin evaluación concluyente' : number(d.reproduction.pregnancyPercent) + '%'} detail={d.reproduction.pregnantFemales + (d.reproduction.pregnantFemales === 1 ? ' preñada / ' : ' preñadas / ') + d.reproduction.evaluatedFemales + ' evaluadas · ' + d.reproduction.uncertainFemales + ' inciertas excluidas'} icon="leaf" href="/animals" action="Consultar fichas" />
        <DashboardMetric label="Existencias críticas" value={number(d.critical, 0)} detail="Insumos por finca en el mínimo o por debajo" icon="inventory" href="/inventory" action="Revisar existencias" attention={d.critical > 0} />
      </div>
      <GrowthAlerts farmId={period.farmId} revision={liveRevision} />
      <Link className="button secondary" to={"/monitoring" + (period.farmId ? "?farm=" + period.farmId : "")}>Configurar objetivos de crecimiento</Link>
      <h2 className="dashboard-section-heading">Producción y seguimiento</h2>
      <Suspense fallback={<p role="status">Preparando gráficos…</p>}><Charts data={d} /></Suspense>
    <div className="operations-grid"><SeriesTable title="Leche por día" rows={d.milkByDay} unit="L" /><SeriesTable title="Leche de animales por lote actual" rows={d.milkByCurrentLot} unit="L" /><SeriesTable title="Peso bovino medio por edad al pesaje" rows={d.weightByAge} unit="kg" /></div><article className="panel"><h2>Distribución de peso por edad</h2>{d.weightDistributionByAge?.length ? <TableScroll><table><thead><tr><th>Edad al pesaje</th><th>Rango de peso vivo</th><th>Bovinos</th></tr></thead><tbody>{d.weightDistributionByAge.map(p => <tr key={p.ageGroup + p.minimumKg}><td>{p.ageGroup}</td><td>{number(p.minimumKg)}{p.maximumKg === null ? ' kg o más' : ' a menos de ' + number(p.maximumKg) + ' kg'}</td><td>{p.count}</td></tr>)}</tbody></table></TableScroll> : <p className="muted">Sin pesajes con edad conocida en el período.</p>}</article><details className="metric-method"><summary>Cómo leer producción y pesajes</summary><p className="muted">Los lotes corresponden a la ubicación actual de los animales y no a una asignación histórica del ordeño. Solo se suma leche en litros ({d.excludedMilk} registros excluidos por unidad). El peso usa el último pesaje de cada bovino en el período y su edad ese día ({d.excludedWeights} bovinos excluidos por fecha de nacimiento desconocida o inválida).</p></details>
    <article className="panel reproduction-summary"><h2>Preñez y fertilidad del período</h2><ReproductionSummary value={d.reproduction} /><details className="metric-method"><summary>Cómo se calculan estos indicadores</summary><p className="muted">Preñez: una hembra por último diagnóstico del período; un parto o aborto posterior deja de contar como preñada. Fertilidad: último servicio por hembra y último diagnóstico posterior dentro del período; las hembras pendientes no cuentan como fallos. Ambos indicadores corresponden a bovinas evaluadas, sin inferir el estado de animales sin registros.</p></details><details className="metric-method"><summary>Registros de respaldo</summary><div className="metric-grid"><div><span>Diagnósticos positivos</span><strong>{d.positiveChecks}</strong></div><div><span>Diagnósticos negativos</span><strong>{d.negativeChecks}</strong></div><div><span>Positivos / diagnósticos concluyentes</span><strong>{d.positiveCheckPercent === null ? 'Sin diagnósticos concluyentes' : number(d.positiveCheckPercent) + '%'}</strong></div><div><span>Partos registrados</span><strong>{d.calvings}</strong></div></div><p className="muted">Denominador: {d.positiveChecks + d.negativeChecks} diagnósticos positivos o negativos. {d.uncertainChecks} inciertos excluidos. Es una proporción de diagnósticos, no una tasa de concepción por monta. Crías vivas: {d.liveBirths}; mortinatos: {d.stillbirths}.</p></details></article>
      <h2 className="dashboard-section-heading">Recursos de la finca</h2><p className="muted">La valoración y los límites muestran el saldo actual, independientemente del período de los registros.</p>
      {valuation.controls}
      <div className="metric-grid"><article className="panel"><span>Costo del inventario actual</span><strong>{valuation.formatMoney(d.cost)}</strong></article><article className="panel"><span>Valor de referencia actual</span><strong>{valuation.formatMoney(d.referenceValue)}</strong></article><article className="panel"><span>En el mínimo o por debajo</span><strong>{d.critical}</strong><Link to="/inventory">Revisar existencias</Link></article><article className="panel"><span>En el máximo o por encima</span><strong>{d.excess}</strong></article></div>
      <p className="muted">Base del catálogo en USD; vista actual en {valuation.unit}. El valor de referencia usa el precio unitario; no representa ingresos ni utilidad realizada.</p>
    <article className="panel"><h2>Valoración por categoría</h2><TableScroll className="table-scroll"><table><thead><tr><th>Categoría</th><th>Costo ({valuation.unit})</th><th>Referencia ({valuation.unit})</th></tr></thead><tbody>{d.categories.map(c => <tr key={c.category}><td>{c.category}</td><td>{valuation.formatMoney(c.cost)}</td><td>{valuation.formatMoney(c.referenceValue)}</td></tr>)}</tbody></table></TableScroll></article>
    <article className="panel"><h2>Límites y rotación del inventario</h2><details className="metric-method"><summary>Cómo se calcula la rotación</summary><p className="muted">Rotación = salidas del período / promedio del saldo inicial y final. Solo se calcula con un saldo inicial trazable anterior al inicio del período. El día de apertura puede estar incompleto.</p></details><TableScroll className="table-scroll"><table><thead><tr><th>Insumo / finca</th><th>Saldo actual</th><th>Mín. / Máx.</th><th>Salidas registradas en el período</th><th>Rotación</th></tr></thead><tbody>{d.stock.slice((currentStockPage - 1) * 20, currentStockPage * 20).map(s => <tr key={s.id}><td>{s.product}<small className="muted">{s.farm}</small></td><td><span className={s.stock <= s.min ? 'stock-low tag' : s.stock >= s.max ? 'stock-high tag' : ''}>{number(s.stock, 4)} {units[s.unit]}</span></td><td>{number(s.min)} / {number(s.max)}</td><td>{number(s.outflow, 4)} {units[s.unit]}</td><td>{s.rotation === null ? 'Historial insuficiente' : number(s.rotation, 4) + ' veces'}{s.historySince && <small className="muted">Desde {date(s.historySince)}</small>}</td></tr>)}</tbody></table></TableScroll>{!d.stock.length && <p className="muted">No hay existencias registradas.</p>}<CarePagination page={currentStockPage} total={d.stock.length} pageSize={20} onPage={setStockPage} /></article>
    </>}
  </section>;
}
