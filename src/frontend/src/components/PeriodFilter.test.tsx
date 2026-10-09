// @vitest-environment jsdom
import { render, screen, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expect, it, vi } from 'vitest';
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
