import * as Popover from '@radix-ui/react-popover';
import { useEffect, useId, useRef, useState } from 'react';
import type { ButtonHTMLAttributes } from 'react';
import { Button, Input } from './Controls';
import { Icon } from './Icon';

export type PickerOption = { value: string; label: string; detail?: string };
export type PickerProps = Omit<ButtonHTMLAttributes<HTMLButtonElement>, 'value' | 'onChange'> & {
  label: string; value: string; selectedLabel?: string; emptyLabel: string;
  options: PickerOption[]; search: string; onSearch: (text: string) => void;
  onChange: (value: string) => void; onOpenChange?: (open: boolean) => void;
  loading?: boolean; error?: string | null; total?: number; page?: number;
  pageSize?: number; onPage?: (page: number) => void;
};
export function SearchPicker({ label, value, selectedLabel, emptyLabel, options, search, onSearch, onChange, onOpenChange, loading, error, total = options.length, page = 1, pageSize = 20, onPage, disabled, ...buttonProps }: PickerProps) {
  const [open, setOpen] = useState(false);
  const trigger = useRef<HTMLButtonElement>(null);
  const searchInput = useRef<HTMLInputElement>(null);
  const content = useRef<HTMLDivElement>(null);
  const restoreFocus = useRef(true);
  const explicitClose = useRef(false);
  const titleId = useId();
  function changeOpen(next: boolean) { if (next) { restoreFocus.current = true; explicitClose.current = false; } setOpen(next); onOpenChange?.(next); }
  useEffect(() => { if (disabled && open) changeOpen(false); }, [disabled, open]);
  const display = options.find(option => option.value === value)?.label || selectedLabel;
  function choose(next: string) { explicitClose.current = true; onChange(next); changeOpen(false); }
  return <Popover.Root open={open && !disabled} onOpenChange={changeOpen}>
    <Popover.Trigger asChild><Button {...buttonProps} ref={trigger} type="button" disabled={disabled} className="picker-trigger" aria-label={label}>
      <Icon name="animal" /><span className={value ? '' : 'muted'}>{value ? display || 'Animal seleccionado' : emptyLabel}</span><Icon name="chevron" size={18} />
    </Button></Popover.Trigger>
    <Popover.Portal container={trigger.current?.closest('dialog') || undefined}>
      <Popover.Content ref={content} className="search-picker-popup" side="bottom" align="start" sideOffset={8} collisionPadding={12} aria-labelledby={titleId}
        onOpenAutoFocus={event => { event.preventDefault(); searchInput.current?.focus(); }}
        onPointerDownOutside={event => { if (!trigger.current?.contains(event.target as Node)) restoreFocus.current = false; }}
        onFocusOutside={event => { if (!explicitClose.current && !trigger.current?.contains(event.target as Node)) restoreFocus.current = false; }}
        onCloseAutoFocus={event => { event.preventDefault(); if (restoreFocus.current) trigger.current?.focus({ preventScroll: true }); }}
        onEscapeKeyDown={event => { event.preventDefault(); event.stopPropagation(); explicitClose.current = true; changeOpen(false); }}>
        <div className="picker-heading"><strong id={titleId}>Seleccionar {label.toLowerCase()}</strong><Popover.Close asChild><Button className="icon-button" type="button" aria-label="Cerrar selector" onClick={() => { explicitClose.current = true; }}><Icon name="close" size={18} /></Button></Popover.Close></div>
        <div className="picker-search"><Icon name="search" /><Input ref={searchInput} type="search" value={search} maxLength={100} aria-label={'Buscar ' + label.toLowerCase()} placeholder="Buscar por arete o nombre" onChange={event => onSearch(event.target.value)} onKeyDown={event => { if (event.key === 'Enter') event.preventDefault(); if (event.key === 'ArrowDown') { event.preventDefault(); content.current?.querySelector<HTMLButtonElement>('.picker-options button')?.focus(); } }} /></div>
        {loading && <p className="muted" role="status"><Icon name="spinner" className="loading-icon" size={16} />Buscando animales…</p>}
        {error && <p className="field-error" role="alert">{error}</p>}
        <div className="picker-options" aria-label="Candidatos" aria-busy={!!loading} onKeyDown={event => { if (!['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key)) return; const buttons = Array.from(event.currentTarget.querySelectorAll<HTMLButtonElement>('button:not(:disabled)')); const index = buttons.indexOf(document.activeElement as HTMLButtonElement); const next = event.key === 'Home' ? 0 : event.key === 'End' ? buttons.length - 1 : (index + (event.key === 'ArrowDown' ? 1 : -1) + buttons.length) % buttons.length; event.preventDefault(); buttons[next]?.focus(); }}>
          <Button type="button" aria-pressed={!value} onClick={() => choose('')}><span>{emptyLabel}</span>{!value && <Icon name="check" size={18} />}</Button>
          {!loading && !error && options.map(option => <Button key={option.value} type="button" aria-pressed={value === option.value} onClick={() => choose(option.value)}><span>{option.label}{option.detail && <small className="muted">{option.detail}</small>}</span>{value === option.value && <Icon name="check" size={18} />}</Button>)}
        </div>
        {!loading && !error && !options.length && <p className="muted">No hay animales que coincidan con la búsqueda.</p>}
        {onPage && !error && <div className="picker-pagination"><small className="muted">{total} candidatos · página {page}</small><Button className="text-button" type="button" disabled={loading || page <= 1} onClick={() => onPage(page - 1)}>Anterior</Button><Button className="text-button" type="button" disabled={loading || page * pageSize >= total} onClick={() => onPage(page + 1)}>Siguiente</Button></div>}
      </Popover.Content>
    </Popover.Portal>
  </Popover.Root>;
}

export default SearchPicker;
