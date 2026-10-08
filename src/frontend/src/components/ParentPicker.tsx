import { Button, Input, Select } from './ui/Controls';
import { useState } from 'react';
import type { AnimalDetail, AnimalPageResult } from '../api/livestock';
import { useResource } from '../api/useResource';
import { Field } from './Field';
export function ParentPicker({ label, sex, farmId, speciesId, excludeId, value, onChange, error, damId, emptyLabel = 'Sin progenitor registrado', hint = 'Misma finca y especie; la edad y la genealogía se comprueban al guardar.' }: {
  label: string; sex: string; farmId: string; speciesId: string; excludeId?: string;
  damId?: string; emptyLabel?: string; hint?: string;
  value: string; onChange: (value: string) => void; error?: string;
}) {
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const params = new URLSearchParams({ farmId, speciesId, ...(sex ? { sex } : {}), ...(damId ? { damId } : {}), page: String(page), pageSize: '20', ...(query ? { search: query } : {}), ...(excludeId ? { excludeId } : {}) });
  const results = useResource<AnimalPageResult>('/api/animals/page?' + params, !!farmId && !!speciesId && query !== null);
  const selected = useResource<AnimalDetail>('/api/animals/' + value, !!value);
  return <div className="parent-picker"><Field label={label} error={error} hint={hint}><Select value={value} onChange={event => onChange(event.target.value)} disabled={!farmId || !speciesId}>
    <option value="">{emptyLabel}</option>
    {value && !results.data?.items.some(item => item.id === value) && <option value={value}>{selected.data ? selected.data.internalTag + (selected.data.name ? ' · ' + selected.data.name : '') : 'Progenitor registrado'}</option>}
    {results.data?.items.map(item => <option key={item.id} value={item.id}>{item.internalTag}{item.name ? ' · ' + item.name : ''}</option>)}
  </Select></Field><div className="compact-search"><Input aria-label={'Buscar ' + label.toLowerCase()} value={search} maxLength={100} disabled={!farmId || !speciesId} placeholder="Buscar por arete o nombre" onChange={event => setSearch(event.target.value)} /><Button type="button" className="button secondary" disabled={!farmId || !speciesId || results.loading} onClick={() => { setPage(1); setQuery(search.trim()); }}>Buscar</Button></div>
    {results.loading && <small role="status">Buscando progenitores…</small>}{results.error && <small className="field-error" role="alert">{results.error}</small>}
    {results.data && <div className="picker-pagination"><small className="muted">{results.data.total} candidatos · página {page}</small><Button type="button" className="text-button" disabled={page === 1} onClick={() => setPage(page - 1)}>Anterior</Button><Button type="button" className="text-button" disabled={page * 20 >= results.data.total} onClick={() => setPage(page + 1)}>Siguiente</Button></div>}
  </div>;
}
