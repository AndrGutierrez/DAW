import { useState } from 'react';
import { CartesianGrid, Line, LineChart, ReferenceDot, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { date, kg, number } from '../api/livestock';
import type { GrowthPoint } from '../api/livestock';

export function GrowthChart({ points, totalDates }: { points: GrowthPoint[]; totalDates: number }) {
  const [mode, setMode] = useState<'weight' | 'gain'>('weight');
  const [active, setActive] = useState(0);
  const visible = points.filter(point => mode === 'weight' || point.dailyGainKg !== null)
    .map(point => ({ ...point, timestamp: Date.parse(point.date) }));
  const selectedIndex = Math.min(active, Math.max(visible.length - 1, 0));
  const selected = visible[selectedIndex];
  const firstDay = visible[0]?.timestamp || 0;
  const lastDay = visible.at(-1)?.timestamp || 0;
  const domain = firstDay === lastDay ? [firstDay - 43200000, lastDay + 43200000] : [firstDay, lastDay];
  const title = mode === 'weight' ? 'Curva de peso del animal' : 'Curva de ganancia diaria de peso';
  return <div className="growth-chart"><div className="chart-toolbar"><div className="segmented-control" aria-label="Medida de la curva"><button type="button" aria-pressed={mode === 'weight'} onClick={() => { setMode('weight'); setActive(0); }}>Peso</button><button type="button" aria-pressed={mode === 'gain'} onClick={() => { setMode('gain'); setActive(0); }}>GDP</button></div><span className="muted">{mode === 'weight' ? 'Peso vivo · kg' : 'Ganancia diaria · kg/día'}</span></div>
    {!visible.length ? <div className="chart-empty"><p>{mode === 'weight' ? 'Registra el primer peso para comenzar la curva.' : 'Se necesitan pesajes de al menos dos fechas distintas para calcular la GDP.'}</p></div> : <>
      <div role="group" aria-label={title} className="chart-canvas"><ResponsiveContainer width="100%" height={260} minWidth={0}>
        <LineChart data={visible} accessibilityLayer title={title} margin={{ top: 16, right: 20, bottom: 6, left: 0 }} onMouseMove={state => {
          const index = Number(state.activeTooltipIndex);
          if (state.activeTooltipIndex != null && Number.isInteger(index) && index >= 0 && index < visible.length) setActive(index);
        }}>
          <CartesianGrid stroke="var(--line)" strokeDasharray="4 4" vertical={false} />
          <XAxis type="number" dataKey="timestamp" scale="time" domain={domain} ticks={firstDay === lastDay ? [firstDay] : [firstDay, lastDay]} tickFormatter={value => date(new Date(value).toISOString().slice(0, 10))} tick={{ fill: 'var(--muted)', fontSize: 12 }} axisLine={false} tickLine={false} />
          <YAxis width={56} domain={mode === 'weight' ? [0, 'auto'] : ['auto', 'auto']} tickCount={5} tickFormatter={value => number(value, 2)} tick={{ fill: 'var(--muted)', fontSize: 12 }} axisLine={false} tickLine={false} />
          <Tooltip isAnimationActive={false} labelFormatter={label => date(new Date(Number(label)).toISOString().slice(0, 10))} formatter={value => [number(Number(value), 4) + (mode === 'weight' ? ' kg' : ' kg/día'), mode === 'weight' ? 'Peso' : 'GDP']} contentStyle={{ background: 'var(--panel)', borderColor: 'var(--line)', borderRadius: 8, color: 'var(--text)', fontSize: 13 }} />
          <Line type="linear" dataKey={mode === 'weight' ? 'weightKg' : 'dailyGainKg'} stroke="#3f8b78" strokeWidth={3} isAnimationActive={false} dot={{ r: 4, className: 'chart-point' }} activeDot={{ r: 6 }} />
          <ReferenceDot x={selected.timestamp} y={mode === 'weight' ? selected.weightKg : selected.dailyGainKg!} r={6} fill="#3f8b78" stroke="var(--panel)" />
        </LineChart>
      </ResponsiveContainer></div>
      <div className="chart-inspector" aria-live="polite"><button type="button" className="button secondary" aria-label="Pesaje anterior en la curva" disabled={selectedIndex <= 0} onClick={() => setActive(Math.max(0, selectedIndex - 1))}>←</button><div><strong>{date(selected.date)} · {kg(selected.weightKg)}</strong><span>{selected.dailyGainKg === null ? 'Sin intervalo previo' : 'GDP: ' + number(selected.dailyGainKg, 4) + ' kg/día'}</span></div><button type="button" className="button secondary" aria-label="Pesaje siguiente en la curva" disabled={selectedIndex >= visible.length - 1} onClick={() => setActive(Math.min(visible.length - 1, selectedIndex + 1))}>→</button></div>
    </>}<p className="muted chart-caption">GDP = diferencia de peso ÷ días entre fechas. Se usa el último registro de cada día; todos los pesajes se conservan en el historial.{totalDates > 60 && ' La curva muestra las últimas 60 fechas.'}</p>
  </div>;
}
