// @vitest-environment jsdom
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, expect, it, vi } from 'vitest';
import { AuditLogsPage } from './AuditLogsPage';
const auth = vi.hoisted(() => ({ isAdmin: true, can: vi.fn(() => true), request: vi.fn() }));
vi.mock('../auth/AuthContext', () => ({ useAuth: () => auth }));
const entry = { id: 'event-1', occurredAt: '2026-10-08T12:00:00Z', action: 'Modified', entityName: 'Animal', entityId: 'animal-1', userId: 'user-1', actorName: 'Operador de prueba', farmId: 'farm-1', farmName: 'Finca de prueba' };
const detail = { entry, ipAddress: '192.0.2.10', oldValues: JSON.stringify({ Name: 'Anterior', InternalTag: 'A-1' }), newValues: JSON.stringify({ Name: 'Actual', InternalTag: 'A-1' }) };
beforeEach(() => {
  auth.isAdmin = true; auth.can.mockReset().mockReturnValue(true); auth.request.mockReset().mockImplementation(async (path: string) =>
    path.endsWith('/options') ? { actions: ['Modified'], entities: ['Animal'], farms: [{ id: 'farm-1', name: 'Finca de prueba' }] } : path.endsWith('/event-1') ? detail : { items: [entry], total: 26, page: new URL('http://localhost' + path).searchParams.get('page') === '2' ? 2 : 1, pageSize: 25 });
  HTMLDialogElement.prototype.showModal = vi.fn(function(this: HTMLDialogElement) { this.open = true; }); HTMLDialogElement.prototype.close = vi.fn(function(this: HTMLDialogElement) { this.open = false; });
});
it('fetches snapshots only on demand and distinguishes changed from unchanged fields', async () => {
  const user = userEvent.setup(); render(<AuditLogsPage/>);
  const open = await screen.findByRole('button', { name: /Ver evento de Animal/ });
  expect(auth.request).not.toHaveBeenCalledWith('/api/admin/auditlogs/event-1', expect.anything());
  await user.click(open); expect(await screen.findByRole('table', { name: 'Cambios del evento' })).toBeInTheDocument();
  expect(screen.getByRole('row', { name: 'Nombre Anterior Actual' })).toBeInTheDocument();
  expect(screen.queryByRole('row', { name: /Identificación interna/ })).not.toBeInTheDocument();
  await user.click(screen.getByLabelText('Mostrar campos sin cambios'));
  expect(screen.getByRole('row', { name: 'Identificación interna A-1 A-1' })).toBeInTheDocument();
  expect(screen.getByText('192.0.2.10')).toBeInTheDocument();
  await user.click(screen.getByRole('button', { name: 'Cerrar detalle de auditoría' }));
  expect(screen.queryByRole('table', { name: 'Cambios del evento' })).not.toBeInTheDocument();
});
it('submits combined filters, resets pagination, and clears the controls', async () => {
  const user = userEvent.setup(); render(<AuditLogsPage/>); await screen.findByRole('table', { name: 'Eventos de auditoría' });
  await user.type(screen.getByLabelText('Buscar registro'), 'animal-1'); await user.type(screen.getByLabelText('Usuario'), 'Operador');
  await user.selectOptions(screen.getByLabelText('Acción'), 'Modified'); await user.selectOptions(screen.getByLabelText('Tipo de registro'), 'Animal');
  await user.selectOptions(screen.getByLabelText('Finca'), 'farm-1'); await user.type(screen.getByLabelText('Desde (UTC)'), '2026-10-01'); await user.type(screen.getByLabelText('Hasta (UTC)'), '2026-10-08');
  await user.click(screen.getByRole('button', { name: 'Filtrar' }));
  await waitFor(() => expect(auth.request).toHaveBeenCalledWith(expect.stringContaining('search=animal-1'), expect.anything()));
  const requested = auth.request.mock.calls.map(c => String(c[0])).find(p => p.includes('search=animal-1'))!;
  expect(requested).toContain('actor=Operador'); expect(requested).toContain('farmId=farm-1'); expect(requested).toContain('from=2026-10-01'); expect(requested).toContain('page=1');
  await user.click(screen.getByRole('button', { name: 'Limpiar' })); expect(screen.getByLabelText('Buscar registro')).toHaveValue(''); expect(screen.getByLabelText('Desde (UTC)')).toHaveValue('');
});
it('rejects reversed dates locally and does not send them to the API', async () => {
  const user = userEvent.setup(); render(<AuditLogsPage/>); await screen.findByRole('table', { name: 'Eventos de auditoría' }); const count = auth.request.mock.calls.length;
  await user.type(screen.getByLabelText('Desde (UTC)'), '2026-10-08'); await user.type(screen.getByLabelText('Hasta (UTC)'), '2026-10-01'); await user.click(screen.getByRole('button', { name: 'Filtrar' }));
  expect(screen.getByRole('alert')).toHaveTextContent('fecha inicial'); expect(auth.request).toHaveBeenCalledTimes(count);
});
it('requests the next bounded server page', async () => {
  const user = userEvent.setup(); render(<AuditLogsPage/>); await screen.findByRole('table', { name: 'Eventos de auditoría' }); await user.click(screen.getByRole('button', { name: 'Siguiente' }));
  await waitFor(() => expect(auth.request).toHaveBeenCalledWith(expect.stringContaining('page=2'), expect.anything())); expect(await screen.findByText('26 registros · página 2 de 2')).toBeInTheDocument();
});
it('offers recovery when the audit query fails', async () => {
  auth.request.mockImplementation(async (path: string) => { if (!path.endsWith('/options')) throw Error('Servicio no disponible'); return { actions: [], entities: [], farms: [] }; });
  const user = userEvent.setup(); render(<AuditLogsPage/>); expect(await screen.findByRole('alert')).toHaveTextContent('Servicio no disponible'); const count = auth.request.mock.calls.length;
  await user.click(screen.getByRole('button', { name: 'Reintentar' })); await waitFor(() => expect(auth.request.mock.calls.length).toBeGreaterThan(count));
});
it('keeps credential-free access events meaningful without fabricating changed values', async () => {
  auth.request.mockImplementation(async (path: string) => path.endsWith('/options') ? { actions: [], entities: [], farms: [] } : path.endsWith('/event-1') ? { ...detail, entry: { ...entry, entityName: 'Authentication', action: 'LoginSucceeded' }, oldValues: null, newValues: null, ipAddress: null } : { items: [entry], total: 1, page: 1, pageSize: 25 });
  const user = userEvent.setup(); render(<AuditLogsPage/>); await user.click(await screen.findByRole('button', { name: /Ver evento de Animal/ }));
  expect(await screen.findByText(/sin guardar credenciales/)).toBeInTheDocument(); expect(screen.getByText('No registrada en este evento')).toBeInTheDocument(); expect(screen.queryByRole('table', { name: 'Cambios del evento' })).not.toBeInTheDocument();
});
it('does not request data or render audit controls without both administrator access and permission', () => {
  auth.isAdmin = false; const view = render(<AuditLogsPage/>); expect(screen.getByRole('alert')).toHaveTextContent('administrador'); expect(auth.request).not.toHaveBeenCalled(); view.unmount();
  auth.isAdmin = true; auth.can.mockReturnValue(false); render(<AuditLogsPage/>); expect(screen.queryByRole('button')).not.toBeInTheDocument(); expect(auth.request).not.toHaveBeenCalled();
});
