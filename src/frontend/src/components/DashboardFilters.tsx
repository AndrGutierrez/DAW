import { useState } from 'react';
import type { CatalogItem } from '../api/livestock';
import { date } from '../api/livestock';
import { PeriodFilter } from './PeriodFilter';
import type { Period } from './PeriodFilter';
import { SidePanel } from './SidePanel';
import { Button } from './ui/Controls';
import { Icon } from './ui/Icon';

export function DashboardFilters({ value, farms, onApply, busy }: { value: Period; farms: CatalogItem[]; onApply: (period: Period) => void; busy: boolean }) {
  const [open, setOpen] = useState(false);
  const farm = farms.find(item => item.id === value.farmId)?.data.name || (value.farmId ? 'Finca seleccionada' : 'Mis fincas');
  return <>
    <div className="dashboard-toolbar">
      <div className="dashboard-filter-summary"><span><Icon name="location" size={16} />{farm}</span><span>{date(value.from)} — {date(value.to)}</span></div>
      <Button aria-haspopup="dialog" aria-expanded={open} onClick={() => setOpen(true)}><Icon name="filter" size={18} />Filtros</Button>
    </div>
    {open && <SidePanel title="Filtros del dashboard" eyebrow="PERÍODO Y FINCA" closeLabel="Cerrar filtros" onRequestClose={() => setOpen(false)}>
      <div className="dashboard-filter-form"><PeriodFilter value={value} farms={farms} busy={busy} onApply={period => { onApply(period); setOpen(false); }} />
      <Button onClick={() => setOpen(false)}>Cancelar</Button></div>
    </SidePanel>}
  </>;
}
