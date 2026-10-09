// @vitest-environment jsdom
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, expect, it, vi } from 'vitest';
import { TransfersPage } from './TransfersPage';
import { ApiError } from '../auth/session';
const auth = vi.hoisted(() => ({ can: vi.fn(), request: vi.fn() }));
vi.mock('../auth/AuthContext', () => ({ useAuth: () => auth }));
vi.mock('../components/NavigationProtection', () => ({ useUnsavedChanges: () => vi.fn() }));
vi.mock('../components/Feedback', () => ({ useFeedback: () => ({ confirm: vi.fn().mockResolvedValue(true), notify: vi.fn() }) }));
const members = ['A', 'B'].map(id => ({ id, internalTag: id, speciesId: 'species', farmId: 'farm', paddockId: 'source', lotId: 'lot', lot: 'Cría', status: 'Active' }));
let capacity = 10;
beforeEach(() => {
  capacity = 10; auth.can.mockReturnValue(true); auth.request.mockReset();
  auth.request.mockImplementation((path: string, options?: RequestInit) => {
    if (options?.method === 'POST') return Promise.resolve({ replayed: false });
    if (path === '/api/farms') return Promise.resolve([{ id: 'farm', data: { name: 'Finca', isActive: true } }]);
    if (path === '/api/lots') return Promise.resolve([{ id: 'lot', data: { name: 'Cría', farmId: 'farm', speciesId: 'species', isActive: true } }]);
    if (path.startsWith('/api/paddocks/destinations')) return Promise.resolve([{ id: 'target', occupancy: 2, data: { name: 'Sur', capacity, isActive: true } }, { id: 'source', occupancy: 2, data: { name: 'Norte', capacity: 10, isActive: true } }]);
    return Promise.resolve({ items: members, total: 2, page: 1, pageSize: 12 });
  });
});
function show() { render(<MemoryRouter><TransfersPage /></MemoryRouter>); }
async function prepare() {
  const user = userEvent.setup(); show();
  await screen.findByRole('button', { name: 'Seleccionar todos los resultados (2)' });
  await user.selectOptions(screen.getByLabelText('Lote de origen'), 'lot');
  await user.click(await screen.findByRole('button', { name: 'Seleccionar todos los resultados (2)' }));
  await screen.findByRole('heading', { name: 'Trasladar 2 animales' });
  await user.selectOptions(screen.getByLabelText('Potrero de destino *'), 'target');
  await user.type(screen.getByLabelText('Motivo del traslado *'), 'Rotación');
  return user;
}
it('moves the full selected lot with expected origins and keeps lot membership by default', async () => {
  const user = await prepare();
  await user.click(screen.getByRole('button', { name: 'Trasladar 2 animales' }));
  await screen.findByRole('heading', { name: '2 animales trasladados' });
  const call = auth.request.mock.calls.find(([, options]) => options?.method === 'POST')!;
  expect(call[0]).toBe('/api/animal-movements/batch');
  const payload = JSON.parse(call[1].body);
  expect(payload).toMatchObject({ farmId: 'farm', toPaddockId: 'target', changeLot: false, toLotId: null, reason: 'Rotación' });
  expect(payload.animals).toEqual(members.map(member => ({ animalId: member.id, submissionId: expect.any(String), expectedFromPaddockId: 'source', expectedFromLotId: 'lot' })));
  expect(payload.animals[0].submissionId).not.toBe(payload.animals[1].submissionId);
  expect(screen.queryByRole('heading', { name: 'Trasladar 2 animales' })).not.toBeInTheDocument();
});
it('blocks a destination without capacity for the entire selected group', async () => {
  capacity = 3; await prepare();
  expect(screen.getByRole('alert')).toHaveTextContent('no tiene capacidad para todo el grupo');
  expect(screen.getByRole('button', { name: 'Trasladar 2 animales' })).toBeDisabled();
  expect(auth.request.mock.calls.some(([, options]) => options?.method === 'POST')).toBe(false);
});
it('keeps the selection after a concurrent-location conflict', async () => {
  const original = auth.request.getMockImplementation()!;
  auth.request.mockImplementation((path: string, options?: RequestInit) => options?.method === 'POST' ? Promise.reject(new ApiError(409, { detail: 'The animal location changed. Refresh before moving it.' })) : original(path, options));
  const user = await prepare(); await user.click(screen.getByRole('button', { name: 'Trasladar 2 animales' }));
  await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('Otro usuario cambió la ubicación'));
  expect(screen.getByRole('heading', { name: 'Trasladar 2 animales' })).toBeVisible();
  expect(screen.queryByRole('heading', { name: '2 animales trasladados' })).not.toBeInTheDocument();
});
it('retries an uncertain request with the same per-animal submission identifiers', async () => {
  const original = auth.request.getMockImplementation()!; let attempts = 0;
  auth.request.mockImplementation((path: string, options?: RequestInit) => options?.method === 'POST' ? (++attempts === 1 ? Promise.reject(new TypeError('Network failure')) : Promise.resolve({ replayed: true })) : original(path, options));
  const user = await prepare(); await user.click(screen.getByRole('button', { name: 'Trasladar 2 animales' }));
  await user.click(await screen.findByRole('button', { name: 'Reintentar y confirmar registro' }));
  await screen.findByRole('heading', { name: '2 animales trasladados' });
  const writes = auth.request.mock.calls.filter(([, options]) => options?.method === 'POST');
  expect(writes).toHaveLength(2); expect(writes[0][1].body).toBe(writes[1][1].body);
});
it('denies direct access without loading protected catalogs', () => {
  auth.can.mockReturnValue(false); show();
  expect(screen.getByRole('heading', { name: 'Sin acceso a los traslados' })).toBeInTheDocument();
  expect(auth.request).not.toHaveBeenCalled();
});
