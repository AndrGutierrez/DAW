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
vi.mock('../components/Feedback', () => ({ useFeedback: () => ({ notify: vi.fn() }) }));
const members = ['A', 'B'].map(id => ({ id, internalTag: id, speciesId: 'species', farmId: 'farm', paddockId: 'source', lotId: 'lot', lot: 'Cría', status: 'Active' }));
let capacity = 10;
beforeEach(() => {
  capacity = 10; auth.can.mockReturnValue(true); auth.request.mockReset();
  auth.request.mockImplementation((path: string, options?: RequestInit) => {
    if (options?.method === 'POST') return Promise.resolve({ replayed: false });
    if (path === '/api/farms') return Promise.resolve([{ id: 'farm', data: { name: 'Finca', isActive: true } }]);
    if (path === '/api/lots') return Promise.resolve([{ id: 'lot', data: { name: 'Cría', farmId: 'farm', speciesId: 'species', isActive: true } }]);
    if (path === '/api/paddocks') return Promise.resolve([{ id: 'source', data: { farmId: 'farm', name: 'Norte', capacity: 10, isActive: true } }, { id: 'target', data: { farmId: 'farm', name: 'Sur', capacity, isActive: true } }, { id: 'inactive', data: { farmId: 'farm', name: 'Anterior', capacity: 10, isActive: false } }]);
    if (path.startsWith('/api/paddocks/destinations')) return Promise.resolve([{ id: 'target', occupancy: 2, data: { farmId: 'farm', name: 'Sur', capacity, isActive: true } }, { id: 'source', occupancy: 2, data: { farmId: 'farm', name: 'Norte', capacity: 10, isActive: true } }]);
    return Promise.resolve({ items: members, total: 2, page: 1, pageSize: 12 });
  });
});
function show() { render(<MemoryRouter><TransfersPage /></MemoryRouter>); }
async function selectGroup() {
  const user = userEvent.setup(); show();
  await screen.findByRole('button', { name: /^Norte:/ });
  await user.selectOptions(screen.getByLabelText('Potrero de origen'), 'source');
  await user.click(screen.getByRole('button', { name: 'Elegir animales' }));
  await user.selectOptions(screen.getByLabelText('Lote de origen'), 'lot');
  await user.click(await screen.findByRole('button', { name: 'Seleccionar todos los resultados (2)' }));
  await user.click(screen.getByRole('button', { name: 'Elegir destino' }));
  return user;
}
async function prepare(review = true) {
  const user = await selectGroup();
  await user.selectOptions(screen.getByLabelText('Potrero de destino *'), 'target');
  await user.type(screen.getByLabelText('Motivo del traslado *'), 'Rotación');
  if (review) await user.click(screen.getByRole('button', { name: 'Revisar traslado' }));
  return user;
}
it('moves the reviewed lot with expected origins and keeps lot membership by default', async () => {
  const user = await prepare();
  expect(auth.request.mock.calls.some(([, options]) => options?.method === 'POST')).toBe(false);
  await user.click(screen.getByRole('button', { name: 'Confirmar traslado' }));
  await screen.findByRole('heading', { name: '2 animales trasladados' });
  const call = auth.request.mock.calls.find(([, options]) => options?.method === 'POST')!;
  expect(call[0]).toBe('/api/animal-movements/batch');
  const payload = JSON.parse(call[1].body);
  expect(payload).toMatchObject({ farmId: 'farm', toPaddockId: 'target', changeLot: false, toLotId: null, reason: 'Rotación' });
  expect(payload.animals).toEqual(members.map(member => ({ animalId: member.id, submissionId: expect.any(String), expectedFromPaddockId: 'source', expectedFromLotId: 'lot' })));
  expect(payload.animals[0].submissionId).not.toBe(payload.animals[1].submissionId);
  expect(screen.queryByRole('button', { name: 'Confirmar traslado' })).not.toBeInTheDocument();
});
it('blocks a destination without capacity before the review stage', async () => {
  capacity = 3; await prepare(false);
  expect(screen.getByRole('alert')).toHaveTextContent('no tiene capacidad para todo el grupo');
  expect(screen.getByRole('button', { name: 'Revisar traslado' })).toBeDisabled();
  expect(auth.request.mock.calls.some(([, options]) => options?.method === 'POST')).toBe(false);
});
it('keeps the group at review after a concurrent-location conflict', async () => {
  const original = auth.request.getMockImplementation()!;
  auth.request.mockImplementation((path: string, options?: RequestInit) => options?.method === 'POST' ? Promise.reject(new ApiError(409, { detail: 'The animal location changed. Refresh before moving it.' })) : original(path, options));
  const user = await prepare(); await user.click(screen.getByRole('button', { name: 'Confirmar traslado' }));
  await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('Otro usuario cambió la ubicación'));
  expect(screen.getByRole('heading', { name: 'Trasladar 2 animales' })).toBeVisible();
  expect(screen.queryByRole('heading', { name: '2 animales trasladados' })).not.toBeInTheDocument();
});
it('locks editing and retries an uncertain submission with identical per-animal identifiers', async () => {
  const original = auth.request.getMockImplementation()!; let attempts = 0;
  auth.request.mockImplementation((path: string, options?: RequestInit) => options?.method === 'POST' ? (++attempts === 1 ? Promise.reject(new TypeError('Network failure')) : Promise.resolve({ replayed: true })) : original(path, options));
  const user = await prepare(); await user.click(screen.getByRole('button', { name: 'Confirmar traslado' }));
  const retry = await screen.findByRole('button', { name: 'Reintentar y confirmar registro' });
  expect(screen.getByRole('button', { name: 'Atrás' })).toBeDisabled();
  expect(screen.getByRole('button', { name: 'Origen' })).toBeDisabled();
  await user.click(retry); await screen.findByRole('heading', { name: '2 animales trasladados' });
  const writes = auth.request.mock.calls.filter(([, options]) => options?.method === 'POST');
  expect(writes).toHaveLength(2); expect(writes[0][1].body).toBe(writes[1][1].body);
});
it('denies direct access without loading protected catalogs', () => {
  auth.can.mockReturnValue(false); show();
  expect(screen.getByRole('heading', { name: 'Sin acceso a los traslados' })).toBeInTheDocument();
  expect(auth.request).not.toHaveBeenCalled();
});
it('keeps animal selection on its own step and filters the source on the server', async () => {
  const user = userEvent.setup(); show();
  await user.click(await screen.findByRole('button', { name: /^Norte: 2 animales;/ }));
  await waitFor(() => expect(auth.request.mock.calls.some(([path]) => path.includes('/api/animals/page?') && path.includes('paddockId=source'))).toBe(true));
  await user.click(screen.getByRole('button', { name: 'Elegir animales' }));
  await user.click(await screen.findByRole('checkbox', { name: 'Seleccionar A' }));
  expect(screen.getByRole('heading', { name: 'Reúne los animales' })).toBeVisible();
  expect(screen.queryByLabelText('Potrero de destino *')).not.toBeInTheDocument();
  await user.click(screen.getByRole('button', { name: 'Elegir destino' }));
  await user.click(screen.getByRole('button', { name: /^Sur: 2 animales;/ }));
  expect(screen.getByLabelText('Potrero de destino *')).toHaveValue('target');
});
it('preselects only an individually linked animal and lets the operator revise its group', async () => {
  const original = auth.request.getMockImplementation()!;
  auth.request.mockImplementation((path: string, options?: RequestInit) => path === '/api/animals/A' ? Promise.resolve(members[0]) : original(path, options));
  const user = userEvent.setup();
  render(<MemoryRouter initialEntries={['/transfers?farm=farm&source=source&animal=A']}><TransfersPage /></MemoryRouter>);
  await screen.findByRole('heading', { name: 'Elige el destino' });
  await user.click(screen.getByRole('button', { name: 'Cambiar grupo' }));
  expect(screen.getByRole('checkbox', { name: 'Seleccionar A' })).toBeChecked();
  expect(screen.getByRole('checkbox', { name: 'Seleccionar B' })).not.toBeChecked();
});
it('preserves the draft when moving backward from review and between animal and destination steps', async () => {
  const user = await prepare();
  await user.click(screen.getByRole('button', { name: 'Atrás' }));
  expect(screen.getByLabelText('Motivo del traslado *')).toHaveValue('Rotación');
  expect(screen.getByLabelText('Potrero de destino *')).toHaveValue('target');
  await user.click(screen.getByRole('button', { name: 'Atrás' }));
  expect(screen.getByRole('checkbox', { name: 'Seleccionar A' })).toBeChecked();
  await user.click(screen.getByRole('button', { name: 'Elegir destino' }));
  expect(screen.getByLabelText('Motivo del traslado *')).toHaveValue('Rotación');
  expect(auth.request.mock.calls.some(([, options]) => options?.method === 'POST')).toBe(false);
});
it('requires an explicit origin and clears the group when the origin changes', async () => {
  const user = userEvent.setup(); show();
  await screen.findByRole('button', { name: /^Norte:/ });
  expect(screen.getByRole('button', { name: 'Elegir animales' })).toBeDisabled();
  await user.selectOptions(screen.getByLabelText('Potrero de origen'), 'source');
  await user.click(screen.getByRole('button', { name: 'Elegir animales' }));
  await user.click(screen.getByRole('checkbox', { name: 'Seleccionar A' }));
  await user.click(screen.getByRole('button', { name: 'Cambiar origen' }));
  await user.selectOptions(screen.getByLabelText('Potrero de origen'), 'target');
  await user.click(screen.getByRole('button', { name: 'Elegir animales' }));
  expect(screen.getByRole('checkbox', { name: 'Seleccionar A' })).not.toBeChecked();
  expect(screen.getByRole('button', { name: 'Elegir destino' })).toBeDisabled();
});
it('starts a fresh operation only after the successful operation is complete', async () => {
  const user = await prepare(); await user.click(screen.getByRole('button', { name: 'Confirmar traslado' }));
  await user.click(await screen.findByRole('button', { name: 'Nuevo traslado' }));
  expect(screen.getByRole('heading', { name: 'Elige el origen' })).toBeVisible();
  expect(screen.getByRole('button', { name: 'Elegir animales' })).toBeDisabled();
  expect(screen.getByLabelText('Finca')).toHaveValue('farm');
});

it('uses the full geometry catalog while keeping inactive paddocks out of transfer choices', async () => {
  show();
  await screen.findByRole('button', { name: /^Norte:/ });
  expect(auth.request.mock.calls.filter(([path]) => path === '/api/paddocks')).toHaveLength(1);
  expect(screen.queryByRole('button', { name: /^Anterior:/ })).not.toBeInTheDocument();
  expect(screen.queryByRole('option', { name: /^Anterior/ })).not.toBeInTheDocument();
});
