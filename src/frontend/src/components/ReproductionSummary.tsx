import type { Analytics } from '../api/operations';
import { number } from '../api/livestock';
import { Icon } from './ui/Icon';
import type { CSSProperties } from 'react';

type Reproduction = Analytics['reproduction'];
function Outcome({ title, percent, positive, total, pending, kind }: { title: string; percent: number | null; positive: number; total: number; pending: number; kind: 'pregnancy' | 'fertility' }) {
  const pregnancy = kind === 'pregnancy';
  return <section className={'reproduction-outcome ' + kind} aria-label={title}>
    <div className="reproduction-outcome-heading"><Icon name={pregnancy ? 'leaf' : 'heart'} size={21} /><h3>{title}</h3></div>
    <div className="reproduction-outcome-body">
      <div className={'reproduction-ring' + (percent === null ? ' is-empty' : '')} style={{ '--outcome': Math.max(0, Math.min(100, percent ?? 0)) + '%' } as CSSProperties} aria-hidden="true"><span>{percent === null ? '—' : number(percent) + '%'}</span></div>
      <div className="reproduction-outcome-result"><strong className={percent === null ? undefined : "sr-only"}>{percent === null ? (pregnancy ? 'Sin evaluación concluyente' : 'Sin servicios evaluados') : number(percent) + '%'}</strong>
        <p>{positive} {pregnancy ? (positive === 1 ? 'preñada' : 'preñadas') : (positive === 1 ? 'positivo' : 'positivos')} de {total} {pregnancy ? (total === 1 ? 'bovina evaluada' : 'bovinas evaluadas') : (total === 1 ? 'servicio evaluado' : 'servicios evaluados')}</p>
        <span className="reproduction-pending"><Icon name="info" size={15} />{pending} {pregnancy ? (pending === 1 ? 'diagnóstico incierto excluido' : 'diagnósticos inciertos excluidos') : (pending === 1 ? 'servicio pendiente' : 'servicios pendientes')}</span>
      </div>
    </div>
  </section>;
}
export function ReproductionSummary({ value }: { value: Reproduction }) {
  return <div className="reproduction-outcomes">
    <Outcome title="Preñez en hembras evaluadas" percent={value.pregnancyPercent} positive={value.pregnantFemales} total={value.evaluatedFemales} pending={value.uncertainFemales} kind="pregnancy" />
    <Outcome title="Fertilidad de servicios evaluados" percent={value.fertilityPercent} positive={value.positiveServices} total={value.evaluatedServices} pending={value.pendingServices} kind="fertility" />
    <p className="reproduction-cohort-note">{value.servedFemales} {value.servedFemales === 1 ? 'hembra con servicio' : 'hembras con servicio'} en el período. Los servicios pendientes no cuentan como fallos.</p>
  </div>;
}
