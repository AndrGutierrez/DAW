// @vitest-environment jsdom
import { act, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expect, it, vi } from 'vitest';
import { PerformancePage } from './PerformancePage';
import { clearRequestTimings, recordTiming, recordVital } from '../performance/store';
const auth = vi.hoisted(() => ({ isAdmin: true }));
vi.mock('../auth/AuthContext', () => ({ useAuth: () => auth }));
it('keeps an unavailable vital pending and renders measured values without inventing zeroes', () => {
 clearRequestTimings(); render(<PerformancePage />);
 expect(screen.getAllByText('Pendiente').length).toBeGreaterThan(0);
 act(() => recordVital({ name: 'LCP', value: 1234, rating: 'good', unit: 'ms' }));
 expect(screen.getByText('1.234 ms')).toBeInTheDocument();
 expect(screen.getByText('Bueno')).toBeInTheDocument();
});
it('allows clearing request samples while retaining page metrics', async () => {
 const user = userEvent.setup();
 recordTiming({ service: 'Animales', kind: 'http', duration: 20, status: 200, outcome: 'success' });
 render(<PerformancePage />);
 expect(screen.getByRole('cell', { name: 'Animales' })).toBeInTheDocument();
 await user.click(screen.getByRole('button', { name: 'Limpiar muestras de solicitudes' }));
 expect(screen.queryByRole('table')).not.toBeInTheDocument();
});
it('does not expose diagnostic controls to an employee', () => {
 auth.isAdmin = false; render(<PerformancePage />);
 expect(screen.getByRole('alert')).toHaveTextContent('administrador');
 expect(screen.queryByRole('button')).not.toBeInTheDocument(); auth.isAdmin = true;
});
