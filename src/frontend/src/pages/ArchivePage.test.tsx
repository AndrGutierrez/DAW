// @vitest-environment jsdom
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, expect, it, vi } from 'vitest';
import { ArchivePage } from './ArchivePage';
const auth = vi.hoisted(() => ({ isAdmin: true, can: vi.fn(() => true), request: vi.fn() }));
const feedback = vi.hoisted(() => ({ notify: vi.fn(), confirm: vi.fn(async () => true) }));
vi.mock('../auth/AuthContext', () => ({ useAuth: () => auth }));
vi.mock('../components/Feedback', () => ({ useFeedback: () => feedback }));
const entry = { id: 'archived-1', resource: 'animals', label: 'ARC-1 · Aurora', farmId: 'farm-1', deletedAt: '2026-10-08T12:00:00Z', deletedByUserId: 'admin-1' };
beforeEach(() => {
  auth.isAdmin = true; auth.can.mockReset().mockReturnValue(true); feedback.notify.mockClear(); feedback.confirm.mockReset().mockResolvedValue(true);
  auth.request.mockReset().mockImplementation(async (path: string) => path.endsWith('/archived-1') ? { entry, values: JSON.stringify({ InternalTag: 'ARC-1', IsDeleted: true }) } : path.endsWith('/restore') ? undefined : { items: [entry], total: 1, page: 1, pageSize: 25 });
  HTMLDialogElement.prototype.showModal = vi.fn(function(this: HTMLDialogElement) { this.open = true; }); HTMLDialogElement.prototype.close = vi.fn(function(this: HTMLDialogElement) { this.open = false; });
});
it('filters on the server and restores only after confirmation', async () => {
  const user = userEvent.setup(); render(<ArchivePage/>); await screen.findByRole('table', { name: 'Registros archivados' });
  await user.type(screen.getByLabelText('Buscar registro archivado'), 'ARC-1'); await user.selectOptions(screen.getByLabelText('Tipo de registro archivado'), 'animals'); await user.click(screen.getByRole('button', { name: 'Filtrar' }));
  await waitFor(() => expect(auth.request).toHaveBeenCalledWith(expect.stringContaining('search=ARC-1'), expect.anything()));
  await user.click(await screen.findByRole('button', { name: 'Revisar ARC-1 · Aurora' })); await screen.findByRole('table', { name: 'Datos conservados' });
  feedback.confirm.mockResolvedValueOnce(false); await user.click(screen.getByRole('button', { name: 'Restaurar registro' })); expect(auth.request.mock.calls.some(c => String(c[0]).endsWith('/restore'))).toBe(false);
  await user.click(screen.getByRole('button', { name: 'Restaurar registro' })); await waitFor(() => expect(auth.request).toHaveBeenCalledWith('/api/admin/archive/animals/archived-1/restore', { method: 'POST' }));
  expect(feedback.notify).toHaveBeenCalledWith('Registro restaurado.'); await waitFor(() => expect(screen.queryByRole('table', { name: 'Datos conservados' })).not.toBeInTheDocument());
});
it('keeps the archived detail visible when restoration violates a business rule', async () => {
  const original = auth.request.getMockImplementation()!; auth.request.mockImplementation((path: string, init: RequestInit) => path.endsWith('/restore') ? Promise.reject(Error('Restore the parent farm first.')) : original(path, init));
  const user = userEvent.setup(); render(<ArchivePage/>); await user.click(await screen.findByRole('button', { name: 'Revisar ARC-1 · Aurora' })); await screen.findByRole('table', { name: 'Datos conservados' });
  await user.click(screen.getByRole('button', { name: 'Restaurar registro' })); expect(await screen.findByRole('alert')).toHaveTextContent('parent farm'); expect(screen.getByRole('table', { name: 'Datos conservados' })).toBeInTheDocument();
});
it('does not fetch archived information for employees', () => { auth.isAdmin = false; render(<ArchivePage/>); expect(screen.getByRole('alert')).toHaveTextContent('administrador'); expect(auth.request).not.toHaveBeenCalled(); });
