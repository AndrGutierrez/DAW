import { useState } from 'react';
import { BarChart, Bar, LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend } from 'recharts';
import type { Analytics } from '../api/operations';
import { Select } from './ui/Controls';
import { Field } from './Field';
import { date, number } from '../api/livestock';

export default function AnalyticsCharts({ data }: { data: Analytics }) {
  const [view, setView] = useState('milk');
  const [lotKey, setLotKey] = useState('');
  const seriesKey = (series: Analytics['milkByDayAndCurrentLot'][number]) => series.farmId + ':' + (series.lotId || 'unassigned');
  const lot = data.milkByDayAndCurrentLot.find(series => seriesKey(series) === lotKey) || data.milkByDayAndCurrentLot[0];
  const lotPoints = new Map(lot?.points.map(point => [point.label, point.value]));
  const dailyRows: { label: string; value: number | null }[] = [];
  if (view === 'lot-daily' && lot) {
    const end = Date.parse(data.to + 'T00:00:00Z');
    for (let time = Date.parse(data.from + 'T00:00:00Z'); time <= end; time += 86400000) {
      const label = new Date(time).toISOString().slice(0, 10);
      dailyRows.push({ label, value: lotPoints.get(label) ?? null });
    }
  }
  const rows: Record<string, string | number | null>[] = view === 'valuation' ? data.categories
    : view === 'milk' ? data.milkByDay : view === 'lot' ? data.milkByCurrentLot : view === 'lot-daily' ? dailyRows : data.weightByAge;
  return <div className="panel analytics-chart">
    <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
      <Field label="Gráfico"><Select value={view} onChange={event => setView(event.target.value)}>
        <option value="valuation">Valoración por categoría (USD)</option>
        <option value="milk">Leche por día (L)</option>
        <option value="lot-daily">Leche por día y lote actual (L)</option>
        <option value="lot">Leche de animales por lote actual (L)</option>
        <option value="weight">Peso medio por edad al pesaje (kg)</option>
      </Select></Field>
      {view === 'lot-daily' && <Field label="Lote de la curva diaria"><Select value={lot ? seriesKey(lot) : ''} onChange={event => setLotKey(event.target.value)} disabled={!lot}>
        {!lot && <option value="">Sin registros de leche en litros</option>}
        {data.milkByDayAndCurrentLot.map(series => <option key={seriesKey(series)} value={seriesKey(series)}>{series.label}</option>)}
      </Select></Field>}
    </div>
    {!rows.length ? <p className="muted">No hay datos comparables para este gráfico.</p> : <div className="chart-container" style={{ height: 320 }} aria-label="Gráfico de los indicadores seleccionados">
      <ResponsiveContainer width="100%" height="100%">{view === 'milk' || view === 'lot-daily' ? <LineChart data={view === 'milk' ? data.milkByDay : dailyRows}>
        <CartesianGrid strokeDasharray="3 3" stroke="var(--line)" /><XAxis dataKey="label" stroke="var(--muted)" /><YAxis stroke="var(--muted)" /><Tooltip />
        <Line type="monotone" dataKey="value" name="Leche (L)" stroke="var(--primary)" strokeWidth={3} connectNulls={false} />
      </LineChart> : <BarChart data={rows}>
        <CartesianGrid strokeDasharray="3 3" stroke="var(--line)" /><XAxis dataKey={view === 'valuation' ? 'category' : 'label'} stroke="var(--muted)" /><YAxis stroke="var(--muted)" /><Tooltip /><Legend />
        {view === 'valuation' ? <><Bar dataKey="cost" name="Costo (USD)" fill="var(--primary)" radius={[4, 4, 0, 0]} /><Bar dataKey="referenceValue" name="Referencia (USD)" fill="var(--info)" radius={[4, 4, 0, 0]} /></>
          : <Bar dataKey="value" name={view === 'weight' ? 'Peso medio (kg)' : 'Leche (L)'} fill="var(--primary)" radius={[4, 4, 0, 0]} />}
      </BarChart>}</ResponsiveContainer>
    </div>}
    {view === 'lot-daily' && <>
      <p className="muted">Agrupación según el lote actual del animal. Las fechas sin registros aparecen como huecos; no representan producción cero.</p>
      {lot && <div className="table-scroll overflow-x-auto"><table><caption>Leche diaria · {lot.label}</caption><thead><tr><th>Fecha</th><th>Leche (L)</th><th>Registros</th></tr></thead>
        <tbody>{lot.points.map(point => <tr key={point.label}><td>{date(point.label)}</td><td>{number(point.value, 3)}</td><td>{point.count}</td></tr>)}</tbody>
      </table></div>}
    </>}
    <p className="muted">Los valores también están disponibles en las tablas del dashboard.</p>
  </div>;
}
