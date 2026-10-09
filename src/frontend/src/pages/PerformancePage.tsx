import { TableScroll } from '../components/TableScroll';
import { useSyncExternalStore } from 'react';
import { useAuth } from '../auth/AuthContext';
import { getPerformanceSnapshot, subscribePerformance, percentile, clearRequestTimings } from '../performance/store';
import { Button } from '../components/ui/Controls';
import { Icon } from '../components/ui/Icon';
const definitions = [ ['LCP', 'Carga del contenido principal', 'Objetivo ≤ 2.500 ms'], ['INP', 'Respuesta a interacciones', 'Objetivo ≤ 200 ms'], ['CLS', 'Estabilidad visual', 'Objetivo ≤ 0,1'], ['FCP', 'Primer contenido visible', 'Medición complementaria'], ['TTFB', 'Primer byte de la página', 'Medición complementaria'] ];
export function PerformancePage() {
  const { isAdmin } = useAuth();
  const data = useSyncExternalStore(subscribePerformance, getPerformanceSnapshot);
  if (!isAdmin) return <p role="alert">El diagnóstico requiere acceso de administrador.</p>;
  const completed = data.timings.filter(t => t.outcome !== 'cancelled');
  const http = completed.filter(t => t.kind === 'http');
  const loads = completed.filter(t => t.kind === 'data');
  const format = (value: number | null, digits = 1) => value === null ? 'Sin muestras' : new Intl.NumberFormat('es-VE', { maximumFractionDigits: digits }).format(value);
  function download() {
    const blob = new Blob([JSON.stringify({ scope: 'Current document, local browser samples; not a population percentile', ...data }, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob); const link = document.createElement('a'); link.href = url; link.download = 'frontend-performance.json'; link.click(); setTimeout(() => URL.revokeObjectURL(url), 1000);
  }
  return <section className="operations-page"><div className="page-heading"><div><h1>Rendimiento de la aplicación</h1><p className="muted">Mediciones locales para diagnosticar carga, interacción y comunicación.</p></div><Button variant="secondary" onClick={download}><Icon name="report" />Exportar mediciones</Button></div>
    {!data.supported && <p role="status">Este navegador no ofrece PerformanceObserver. Las métricas web pueden no estar disponibles.</p>}
    <div className="metric-grid performance-vitals">{definitions.map(([name, label, target]) => { const vital = data.vitals.find(v => v.name === name); return <article className="panel" key={name}><span>{name} · {label}</span><strong>{vital ? format(vital.value, name === 'CLS' ? 3 : 1) + (vital.unit ? ' ' + vital.unit : '') : 'Pendiente'}</strong><span className={vital ? 'status-pill ' + (vital.rating === 'good' ? 'positive' : vital.rating === 'poor' ? 'critical' : 'warning') : 'muted'}>{vital ? ({ good: 'Bueno', 'needs-improvement': 'Necesita mejorar', poor: 'Lento o inestable' })[vital.rating] : 'Aún sin medición'}</span><small className="muted">{target}</small></article>; })}</div>
    <p className="muted">LCP, INP y CLS corresponden a la vida de este documento, incluida la navegación SPA. INP necesita interacciones; una métrica pendiente no equivale a cero. Los tiempos de datos se miden aparte e incluyen lectura y decodificación de la respuesta. Estas muestras locales no certifican el percentil 75 de usuarios reales.</p>
    <div className="metric-grid"><article className="panel"><span>Respuesta HTTP · p75 local</span><strong>{format(percentile(http.map(t => t.duration), .75))}{http.length ? ' ms' : ''}</strong><small className="muted">{http.length} muestras hasta recibir las cabeceras</small></article><article className="panel"><span>Carga de datos · p75 local</span><strong>{format(percentile(loads.map(t => t.duration), .75))}{loads.length ? ' ms' : ''}</strong><small className="muted">{loads.length} consultas completadas</small></article><article className="panel"><span>Respuestas HTTP rechazadas</span><strong>{http.filter(t => t.outcome === 'error').length}</strong><small className="muted">Incluye rechazos esperados de sesión y validación</small></article></div>
    <article className="panel"><div className="page-heading"><h2>Últimas mediciones</h2><Button variant="secondary" onClick={clearRequestTimings}>Limpiar muestras de solicitudes</Button></div><p className="muted">Máximo 100 solicitudes/consultas, en memoria de esta pestaña. No se registran URLs, IDs, consultas, cuerpos, cookies o credenciales; no se envía telemetría a terceros. Los streams SSE quedan fuera del tiempo de respuesta.</p>{data.timings.length ? <TableScroll className="table-scroll"><table><caption className="sr-only">Mediciones locales por tipo de recurso</caption><thead><tr><th>Recurso</th><th>Medición</th><th>Duración</th><th>Resultado</th></tr></thead><tbody>{[...data.timings].reverse().map((t,i)=><tr key={i}><td>{t.service}</td><td>{t.kind === 'http' ? 'HTTP' : 'Datos disponibles'}</td><td>{format(t.duration)} ms</td><td>{t.outcome === 'cancelled' ? 'Cancelada' : t.status ?? (t.outcome === 'success' ? 'Completada' : 'Error')}</td></tr>)}</tbody></table></TableScroll> : <p className="muted">Navega por el sistema para recoger muestras.</p>}</article>
  </section>;
}
