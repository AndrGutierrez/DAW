// @vitest-environment jsdom
import { render, screen, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, expect, it, vi } from 'vitest';
afterEach(() => vi.useRealTimers());
import { PeriodFilter } from './PeriodFilter';
const value = { from: '2026-10-01', to: '2026-10-08', farmId: '' };
it('holds local edits until the user applies a valid period', async () => {
 const apply = vi.fn(); const user = userEvent.setup();
 render(<PeriodFilter value={value} farms={[{ id: 'farm-a', data: { name: 'Farm A', isActive: true } }]} onApply={apply} />);
 fireEvent.change(screen.getByLabelText('Desde'), { target: { value: '2026-10-02' } });
 await user.selectOptions(screen.getByLabelText('Finca'), 'farm-a');
 expect(apply).not.toHaveBeenCalled();
 expect(screen.getByLabelText('Hasta')).toHaveAttribute('min', '2026-10-02');
 await user.click(screen.getByRole('button', { name: 'Aplicar filtros' }));
 expect(apply).toHaveBeenCalledWith({ ...value, from: '2026-10-02', farmId: 'farm-a' });
});
it('disables repeated requests while the query is busy', () => {
 render(<PeriodFilter value={value} farms={[]} onApply={vi.fn()} busy />);
 expect(screen.getByRole('button', { name: 'Consultando…' })).toBeDisabled();
});

it('announces an excessive period and does not request analytics', async () => {
 const apply = vi.fn(); const user = userEvent.setup();
 render(<PeriodFilter value={{ from: '2024-01-01', to: '2026-10-08', farmId: '' }} farms={[]} onApply={apply} />);
 await user.click(screen.getByRole('button', { name: 'Aplicar filtros' }));
 expect(apply).not.toHaveBeenCalled();
 expect(screen.getByLabelText('Hasta')).toHaveAccessibleDescription('Selecciona un período de hasta 367 días.');
});

function fixedToday() { vi.useFakeTimers({ toFake: ['Date'] }); vi.setSystemTime(new Date('2026-10-12T12:00:00Z')); }
it('applies a complete selected month and farm only on submission', async () => {
 fixedToday(); const apply = vi.fn(); const user = userEvent.setup();
 render(<PeriodFilter value={value} farms={[{ id: 'farm-a', data: { name: 'Farm A', isActive: true } }]} onApply={apply} />);
 await user.selectOptions(screen.getByLabelText('Período'), 'month');
 fireEvent.change(screen.getByLabelText('Mes'), { target: { value: '2024-02' } });
 await user.selectOptions(screen.getByLabelText('Finca'), 'farm-a');
 expect(screen.getByRole('status')).toHaveTextContent('29');
 expect(apply).not.toHaveBeenCalled();
 await user.click(screen.getByRole('button', { name: 'Aplicar filtros' }));
 expect(apply).toHaveBeenCalledWith({ from: '2024-02-01', to: '2024-02-29', farmId: 'farm-a' });
});
it('applies a past year and caps the current year at today', async () => {
 fixedToday(); const apply = vi.fn(); const user = userEvent.setup();
 render(<PeriodFilter value={value} farms={[]} onApply={apply} />);
 await user.selectOptions(screen.getByLabelText('Período'), 'year');
 fireEvent.change(screen.getByLabelText('Año'), { target: { value: '2025' } });
 await user.click(screen.getByRole('button', { name: 'Aplicar filtros' }));
 expect(apply).toHaveBeenLastCalledWith({ from: '2025-01-01', to: '2025-12-31', farmId: '' });
 fireEvent.change(screen.getByLabelText('Año'), { target: { value: '2026' } });
 await user.click(screen.getByRole('button', { name: 'Aplicar filtros' }));
 expect(apply).toHaveBeenLastCalledWith({ from: '2026-01-01', to: '2026-10-12', farmId: '' });
 expect(screen.getByRole('status')).toHaveTextContent('termina hoy');
});
it('applies recent presets and a custom number of days including today', async () => {
 fixedToday(); const apply = vi.fn(); const user = userEvent.setup();
 render(<PeriodFilter value={value} farms={[]} onApply={apply} />);
 await user.selectOptions(screen.getByLabelText('Período'), 'recent');
 await user.selectOptions(screen.getByLabelText('Últimos días'), '7');
 await user.click(screen.getByRole('button', { name: 'Aplicar filtros' }));
 expect(apply).toHaveBeenLastCalledWith({ from: '2026-10-06', to: '2026-10-12', farmId: '' });
 await user.selectOptions(screen.getByLabelText('Últimos días'), 'custom');
 fireEvent.change(screen.getByLabelText('Cantidad de días'), { target: { value: '15' } });
 await user.click(screen.getByRole('button', { name: 'Aplicar filtros' }));
 expect(apply).toHaveBeenLastCalledWith({ from: '2026-09-28', to: '2026-10-12', farmId: '' });
});
it('retains the computed range when switching back to editable dates', async () => {
 fixedToday(); const apply = vi.fn(); const user = userEvent.setup();
 render(<PeriodFilter value={value} farms={[]} onApply={apply} />);
 await user.selectOptions(screen.getByLabelText('Período'), 'recent');
 await user.selectOptions(screen.getByLabelText('Últimos días'), '90');
 await user.selectOptions(screen.getByLabelText('Período'), 'custom');
 expect(screen.getByLabelText('Desde')).toHaveValue('2026-07-15');
 expect(screen.getByLabelText('Hasta')).toHaveValue('2026-10-12');
 expect(apply).not.toHaveBeenCalled();
});
it('keeps invalid custom-day selections out of API queries', () => {
 fixedToday(); const apply = vi.fn();
 render(<PeriodFilter value={value} farms={[]} onApply={apply} />);
 fireEvent.change(screen.getByLabelText('Período'), { target: { value: 'recent' } });
 fireEvent.change(screen.getByLabelText('Últimos días'), { target: { value: 'custom' } });
 fireEvent.change(screen.getByLabelText('Cantidad de días'), { target: { value: '368' } });
 fireEvent.submit(screen.getByRole('button', { name: 'Aplicar filtros' }).closest('form')!);
 expect(apply).not.toHaveBeenCalled();
 expect(screen.getByLabelText('Cantidad de días')).toHaveAccessibleDescription('Elige entre 1 y 367 días.');
});
