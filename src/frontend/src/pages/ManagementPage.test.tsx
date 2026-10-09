// @vitest-environment jsdom
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, expect, it, vi } from 'vitest';
import { ManagementPage } from './ManagementPage';
const auth = vi.hoisted(() => ({ isAdmin: true, can: vi.fn(() => true), request: vi.fn() }));
const feedback = vi.hoisted(() => ({ notify: vi.fn(), confirm: vi.fn(async () => true) }));
vi.mock('../auth/AuthContext', () => ({ useAuth: () => auth }));
vi.mock('../components/Feedback', () => ({ useFeedback: () => feedback }));
vi.mock('../components/NavigationProtection', () => ({ useUnsavedChanges: () => () => {} }));
const farm = { id: 'farm-a', data: { name: 'Finca A', code: 'FA', isActive: true } };
const species = { id: 'species-a', data: { name: 'Bovino', code: 'BO', purpose: 'Meat', isActive: true } };
const breed = { id: 'breed-a', data: { name: 'Brahman', speciesId: 'species-a', purpose: 'Meat', isActive: true } };
const records: Record<string, unknown[]> = { farms: [farm], species: [species], breeds: [breed], lots: [], paddocks: [] };
beforeEach(() => {
 auth.isAdmin = true; auth.can.mockReset().mockReturnValue(true); feedback.notify.mockClear(); feedback.confirm.mockReset().mockResolvedValue(true);
 auth.request.mockReset().mockImplementation(async (path: string, options?: RequestInit) => options?.method ? { id: 'created' } : records[path.split('/').at(-1)!] || []);
 HTMLDialogElement.prototype.showModal = vi.fn(function(this: HTMLDialogElement) { this.open = true; }); HTMLDialogElement.prototype.close = vi.fn(function(this: HTMLDialogElement) { this.open = false; });
});
function show(kind = 'farms') { render(<MemoryRouter initialEntries={['/management?tab=' + kind]}><ManagementPage /></MemoryRouter>); }
it.each([{ kind: 'farms', create: 'Nueva finca' }, { kind: 'species', create: 'Nueva especie' }, { kind: 'breeds', create: 'Nueva raza' }, { kind: 'lots', create: 'Nuevo lote' }])('creates a $kind record through its protected endpoint', async ({ kind, create }) => {
 const user = userEvent.setup(); show(kind); const button = await screen.findByRole('button', { name: create }); await waitFor(() => expect(button).toBeEnabled()); await user.click(button);
 const dialog = screen.getByRole('dialog', { name: create }); await user.type(within(dialog).getByLabelText('Nombre'), 'Registro nuevo');
 if (kind === 'farms' || kind === 'species') await user.type(within(dialog).getByLabelText('Código'), 'NEW');
 if (kind === 'breeds' || kind === 'lots') await user.selectOptions(within(dialog).getByLabelText('Especie'), 'species-a');
 if (kind === 'lots') await user.selectOptions(within(dialog).getByLabelText('Finca'), 'farm-a');
 await user.click(within(dialog).getByRole('button', { name: 'Guardar cambios' }));
 await waitFor(() => expect(auth.request).toHaveBeenCalledWith('/api/' + kind, expect.objectContaining({ method: 'POST' })));
 const call = auth.request.mock.calls.find(([, options]) => options?.method === 'POST')!; const data = JSON.parse(call[1].body);
 expect(data.name).toBe('Registro nuevo'); expect(data.isActive).toBe(true);
 if (kind === 'lots') { expect(data.farmId).toBe('farm-a'); expect(data.paddockId).toBeNull(); }
 if (kind === 'species') expect(data.gestationDays).toBeNull();
 expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
});
it('keeps the editor and draft when the backend rejects a species change', async () => {
 const original = auth.request.getMockImplementation()!; auth.request.mockImplementation((path: string, options?: RequestInit) => options?.method === 'PUT' ? Promise.reject(Error('Species cannot change while animals use this breed.')) : original(path, options));
 const user = userEvent.setup(); show('breeds'); await user.click(await screen.findByRole('button', { name: 'Editar raza' }));
 const dialog = screen.getByRole('dialog', { name: 'Editar raza' }); await user.clear(within(dialog).getByLabelText('Nombre')); await user.type(within(dialog).getByLabelText('Nombre'), 'Nombre corregido'); await user.click(within(dialog).getByRole('button', { name: 'Guardar cambios' }));
 expect(await within(dialog).findByRole('alert')).toHaveTextContent('Species cannot change'); expect(within(dialog).getByLabelText('Nombre')).toHaveValue('Nombre corregido');
});
it('archives only after confirmation and preserves the row on a dependency conflict', async () => {
 const original = auth.request.getMockImplementation()!; auth.request.mockImplementation((path: string, options?: RequestInit) => options?.method === 'DELETE' ? Promise.reject(Error('Farm dependencies remain.')) : original(path, options));
 const user = userEvent.setup(); show(); const archive = await screen.findByRole('button', { name: 'Archivar' }); feedback.confirm.mockResolvedValueOnce(false); await user.click(archive);
 expect(auth.request.mock.calls.some(([, options]) => options?.method === 'DELETE')).toBe(false);
 await user.click(archive); expect(await screen.findByRole('alert')).toHaveTextContent('Farm dependencies remain.'); expect(screen.getByRole('article', { name: 'Finca A' })).toBeInTheDocument();
});
it('does not expose management or fetch catalogs for employees', () => { auth.isAdmin = false; show('breeds'); expect(screen.getByRole('alert')).toHaveTextContent('administrador'); expect(auth.request).not.toHaveBeenCalled(); });
