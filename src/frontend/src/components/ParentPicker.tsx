import { useEffect, useState } from 'react';
import type { AnimalDetail, AnimalPageResult } from '../api/livestock';
import { useResource } from '../api/useResource';
import { Field } from './Field';
import { SearchPicker } from './ui/SearchPicker';

export function ParentPicker({ label, sex, farmId, speciesId, excludeId, value, onChange, error, damId, disabled, emptyLabel = 'Sin progenitor registrado', hint = 'Misma finca y especie; la edad y la genealogía se comprueban al guardar.' }: {
  label: string; sex: string; farmId: string; speciesId: string; excludeId?: string;
  damId?: string; emptyLabel?: string; hint?: string; disabled?: boolean;
  value: string; onChange: (value: string) => void; error?: string;
}) {
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [open, setOpen] = useState(false);
  const [page, setPage] = useState(1);
  useEffect(() => { const timer = setTimeout(() => { setQuery(search.trim()); setPage(1); }, 250); return () => clearTimeout(timer); }, [search]);
  const params = new URLSearchParams({ farmId, speciesId, ...(sex ? { sex } : {}), ...(damId ? { damId } : {}), page: String(page), pageSize: '20', ...(query ? { search: query } : {}), ...(excludeId ? { excludeId } : {}) });
  const results = useResource<AnimalPageResult>('/api/animals/page?' + params, !!farmId && !!speciesId && open);
  const selected = useResource<AnimalDetail>('/api/animals/' + value, !!value);
  return <div className="parent-picker"><Field label={label} error={error} hint={hint}><SearchPicker label={label} value={value} onChange={onChange} emptyLabel={emptyLabel}
    disabled={disabled || !farmId || !speciesId} search={search} onSearch={setSearch} onOpenChange={setOpen}
    selectedLabel={selected.data ? selected.data.internalTag + (selected.data.name ? ' · ' + selected.data.name : '') : 'Animal registrado'}
    options={results.data?.items.map(item => ({ value: item.id, label: item.internalTag + (item.name ? ' · ' + item.name : '') })) || []}
    loading={results.loading || search.trim() !== query} error={results.error} total={results.data?.total} page={page} onPage={setPage} /></Field></div>;
}
