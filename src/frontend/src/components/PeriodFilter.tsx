import { useState } from 'react';
import { Field } from './Field';
import { Button, Input, Select } from './ui/Controls';
import { today } from '../api/livestock';
import type { CatalogItem } from '../api/livestock';
export type Period = { from: string; to: string; farmId: string };
export function PeriodFilter({ value, farms, onApply, busy }: { value: Period; farms: CatalogItem[]; onApply: (period: Period) => void; busy?: boolean }) {
  const [draft, setDraft] = useState(value);
  return <form className="panel filter-bar" onSubmit={event => { event.preventDefault(); onApply(draft); }}><Field label="Desde"><Input required type="date" value={draft.from} max={draft.to} onChange={e => setDraft({ ...draft, from: e.target.value })} /></Field><Field label="Hasta"><Input required type="date" value={draft.to} min={draft.from} max={today()} onChange={e => setDraft({ ...draft, to: e.target.value })} /></Field><Field label="Finca"><Select value={draft.farmId} onChange={e => setDraft({ ...draft, farmId: e.target.value })}><option value="">Mis fincas</option>{farms.map(f => <option key={f.id} value={f.id}>{f.data.name}</option>)}</Select></Field><Button type="submit" variant="primary" disabled={busy}>{busy ? 'Consultando…' : 'Aplicar filtros'}</Button></form>;
}
