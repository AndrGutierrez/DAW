// @vitest-environment jsdom
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, expect, it, vi } from 'vitest';
import { DashboardPage } from './DashboardPage';
const auth = vi.hoisted(() => ({ isAdmin: true, can: vi.fn(() => true), request: vi.fn() }));
vi.mock('../auth/AuthContext', () => ({ useAuth: () => auth }));
vi.mock('../api/useAnalyticsLive', () => ({ useAnalyticsLive: () => 'live' }));
vi.mock('../components/AnalyticsCharts', () => ({ default: () => <div>Gráfico de indicadores</div> }));
vi.mock('../components/ValuationCurrency', () => ({ useValuationCurrency: () => ({ controls: null, unit: 'USD', formatMoney: (value: number) => value + ' USD' }) }));
const overview = { generatedAt: '2026-10-08T12:00:00Z', from: '2026-10-01', to: '2026-10-08', cost: 10, referenceValue: 20, critical: 2, excess: 0, categories: [], stock: [], milkByDay: [{ label: '2026-10-07', value: 18, count: 2 }], milkByCurrentLot: [], milkByDayAndCurrentLot: [], reproduction: { evaluatedFemales: 4, pregnantFemales: 3, uncertainFemales: 1, pregnancyPercent: 75, servedFemales: 5, evaluatedServices: 4, positiveServices: 3, pendingServices: 1, fertilityPercent: 75 }, excludedMilk: 0, weightDistributionByAge: [{ ageGroup: '12–23 meses', minimumKg: 300, maximumKg: 400, count: 3 }], weightByAge: [{ label: '12–24 meses', value: 300, count: 3 }], excludedWeights: 0, positiveChecks: 3, negativeChecks: 1, uncertainChecks: 1, positiveCheckPercent: 75, calvings: 1, liveBirths: 1, stillbirths: 0 };
beforeEach(() => { HTMLDialogElement.prototype.showModal = vi.fn(function(this: HTMLDialogElement) { this.open = true; }); HTMLDialogElement.prototype.close = vi.fn(function(this: HTMLDialogElement) { this.open = false; }); auth.isAdmin = true; auth.can.mockReturnValue(true); auth.request.mockReset(); auth.request.mockImplementation((path: string) => Promise.resolve(path.startsWith('/api/analytics/overview?') ? overview : path === '/api/farms' ? [{ id: 'farm-a', data: { name: 'Farm A', isActive: true } }] : { items: [], total: 0 })); });
function show() { render(<MemoryRouter><DashboardPage /></MemoryRouter>); }
it('renders indicators from the protected API response and preserves their denominator', async () => {
 show(); await screen.findByRole('article', { name: 'Leche registrada' });
 expect(screen.getByRole('article', { name: 'Leche registrada' })).toHaveTextContent('18 L');
 expect(screen.getByRole('article', { name: 'Bovinos con pesaje comparable' })).toHaveTextContent('3');
 expect(screen.getByRole('article', { name: 'Preñez en hembras evaluadas' })).toHaveTextContent('3 preñadas / 4');
 expect(screen.getByRole('article', { name: 'Existencias críticas' })).toHaveTextContent('2');
});
it('does not request protected analytics for an employee', () => {
 auth.isAdmin = false; show();
 expect(screen.getByRole('alert')).toHaveTextContent('administrador');
 expect(auth.request).not.toHaveBeenCalled();
});
it('does not confuse unavailable diagnoses with a zero pregnancy percentage', async () => {
 auth.request.mockImplementation((path: string) => Promise.resolve(path.startsWith('/api/analytics/overview?') ? { ...overview, reproduction: { ...overview.reproduction, pregnancyPercent: null } } : path === '/api/farms' ? [] : { items: [], total: 0 }));
 show(); expect(await screen.findByRole('article', { name: 'Preñez en hembras evaluadas' })).toHaveTextContent('Sin evaluación concluyente');
});
it('queries the applied farm and period, without sending each local edit', async () => {
 const user = userEvent.setup(); show(); await screen.findByRole('article', { name: 'Leche registrada' });
 const before = auth.request.mock.calls.filter(([path]) => path.startsWith('/api/analytics/overview?')).length;
 expect(screen.queryByLabelText('Desde')).not.toBeInTheDocument();
 await user.click(screen.getByRole('button', { name: 'Filtros' }));
 fireEvent.change(screen.getByLabelText('Desde'), { target: { value: '2026-10-01' } });
 await user.selectOptions(screen.getByLabelText('Finca'), 'farm-a');
 expect(auth.request.mock.calls.filter(([path]) => path.startsWith('/api/analytics/overview?'))).toHaveLength(before);
 await user.click(screen.getByRole('button', { name: 'Aplicar filtros' }));
 await waitFor(() => expect(auth.request.mock.calls.some(([path]) => path.includes('from=2026-10-01') && path.includes('farmId=farm-a'))).toBe(true));
 expect(screen.queryByRole('dialog', { name: 'Filtros del dashboard' })).not.toBeInTheDocument();
});

it('shows population counts in weight bands instead of presenting a mean as a distribution', async () => {
 show(); await screen.findByRole('article', { name: 'Leche registrada' });
 const heading = screen.getByRole('heading', { name: 'Distribución de peso por edad' });
 expect(heading.parentElement).toHaveTextContent('300 a menos de 400 kg');
 expect(heading.parentElement).toHaveTextContent('12–23 meses');
});

it('cancels a filter draft without requesting data and reopens the applied values', async () => {
 const user = userEvent.setup(); show(); await screen.findByRole('article', { name: 'Leche registrada' });
 const trigger = screen.getByRole('button', { name: 'Filtros' }); await user.click(trigger);
 const original = (screen.getByLabelText('Desde') as HTMLInputElement).value;
 const before = auth.request.mock.calls.filter(([path]) => path.startsWith('/api/analytics/overview?')).length;
 fireEvent.change(screen.getByLabelText('Desde'), { target: { value: '2026-01-01' } });
 await user.click(screen.getByRole('button', { name: 'Cancelar' }));
 expect(trigger).toHaveFocus(); expect(auth.request.mock.calls.filter(([path]) => path.startsWith('/api/analytics/overview?'))).toHaveLength(before);
 await user.click(trigger); expect(screen.getByLabelText('Desde')).toHaveValue(original);
});
