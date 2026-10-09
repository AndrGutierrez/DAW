// @vitest-environment jsdom
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, expect, it, vi } from 'vitest';
import { useValuationCurrency } from './ValuationCurrency';
const auth = vi.hoisted(() => ({ can: vi.fn(() => true), request: vi.fn() }));
const feedback = vi.hoisted(() => ({ notify: vi.fn(), confirm: vi.fn(async () => true) }));
vi.mock('../auth/AuthContext', () => ({ useAuth: () => auth })); vi.mock('./Feedback', () => ({ useFeedback: () => feedback }));
vi.mock('./NavigationProtection', () => ({ useUnsavedChanges: () => () => {} }));
function Harness() { const currency = useValuationCurrency(true); return <>{currency.controls}<output>{currency.formatMoney(2)}</output></>; }
beforeEach(() => { auth.can.mockReset().mockReturnValue(true); feedback.notify.mockClear(); auth.request.mockReset().mockImplementation(async (path: string) => path.endsWith('/status') ? { lastSuccess: null, error: null, automaticEnabled: true } : { id: 'quote-1', effectiveDate: '2026-10-08', bolivarsPerDollar: 874.7321, source: 'https://bcv.today/api/v1/rate.json', entryMethod: 'automatic' }); });
it('converts the view from the saved reference and preserves it after a provider failure', async () => {
  const original = auth.request.getMockImplementation()!; auth.request.mockImplementation((path: string, init: RequestInit) => path.endsWith('/sync') ? Promise.reject(Error('Provider unavailable')) : original(path, init));
  const user = userEvent.setup(); render(<Harness/>); await screen.findByText(/consulta automática mediante BCV Today/); await user.click(screen.getByRole('button', { name: 'Bs' }));
  const before = screen.getByRole('status').textContent; await user.click(screen.getByRole('button', { name: 'Actualizar desde BCV Today' })); expect(await screen.findByRole('alert')).toHaveTextContent('Provider unavailable'); expect(screen.getByRole('status').textContent).toBe(before);
});
it('refreshes the saved quotation after an automatic retrieval succeeds', async () => { const user = userEvent.setup(); render(<Harness/>); await screen.findByText(/consulta automática mediante BCV Today/); const count = auth.request.mock.calls.length; await user.click(screen.getByRole('button', { name: 'Actualizar desde BCV Today' })); await waitFor(() => expect(auth.request.mock.calls.length).toBeGreaterThan(count + 1)); expect(feedback.notify).toHaveBeenCalledWith('Referencia BCV actualizada desde BCV Today.'); });
it('hides reference maintenance when the user cannot change product valuations', async () => { auth.can.mockReturnValue(false); render(<Harness/>); await screen.findByText(/consulta automática mediante BCV Today/); expect(screen.queryByRole('button', { name: 'Actualizar desde BCV Today' })).not.toBeInTheDocument(); });
