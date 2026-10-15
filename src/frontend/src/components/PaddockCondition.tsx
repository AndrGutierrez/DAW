import { grazingEstimate } from '../api/paddocks';
import type { PaddockSnapshot } from '../api/paddocks';
import { number } from '../api/livestock';

export function PaddockCondition({ item, compact = false }: { item: PaddockSnapshot; compact?: boolean }) {
  const estimate = grazingEstimate(item);
  return <div className={'paddock-condition ' + estimate.stage}>
    <div className="condition-heading"><span className="eyebrow">PASTOREO ESTIMADO · ESTADO ACTUAL</span><strong>{estimate.label}</strong></div>
    {estimate.progress !== null && <meter min={0} max={100} value={estimate.progress * 100} aria-label={'Período de pastoreo utilizado en ' + item.data.name} />}
    <p>{estimate.detail}</p>
    {!compact && <><p className="muted">{item.data.areaHectares ? number(item.occupancy / item.data.areaHectares) + ' animales/ha' : 'Superficie sin registrar'} · {item.data.capacity ? item.occupancy + ' / ' + item.data.capacity + ' de capacidad' : 'Capacidad sin definir'}</p><small>Referencia de permanencia para planificar la rotación; no mide la cantidad real de pasto.</small></>}
  </div>;
}
