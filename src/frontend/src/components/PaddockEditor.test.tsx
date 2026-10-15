// @vitest-environment jsdom
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, expect, it, vi } from 'vitest';
import { PaddockEditor } from './PaddockEditor';
import { paddock } from '../test/paddocks';
const auth = vi.hoisted(() => ({ request: vi.fn(), can: () => true }));
vi.mock('../auth/AuthContext', () => ({ useAuth: () => auth }));
vi.mock('./NavigationProtection', () => ({ useUnsavedChanges: () => vi.fn() }));
vi.mock('./Feedback', () => ({ useFeedback: () => ({ notify: vi.fn(), confirm: vi.fn().mockResolvedValue(true) }) }));
const farms = [{ id: 'farm', data: { name: 'Finca', isActive: true } }];
beforeEach(() => { auth.request.mockReset().mockImplementation((_path: string, options?: RequestInit) => Promise.resolve(options?.method ? { id: 'new' } : [])); });
it('saves visual placement and the existing management parameters in the same request', async () => {
  const user = userEvent.setup(), saved = vi.fn();
  render(<PaddockEditor farms={farms} initialFarmId="farm" onSaved={saved} onCancel={vi.fn()} />);
  await user.type(screen.getByLabelText('Nombre del potrero *'), 'Este');
  await user.type(screen.getByLabelText('Permanencia máxima (días)'), '10');
  await user.click(screen.getByRole('button', { name: 'Mover a la derecha' }));
  await user.click(screen.getByRole('button', { name: 'Guardar potrero' }));
  await waitFor(() => expect(saved).toHaveBeenCalledOnce());
  const write = auth.request.mock.calls.find(([, options]) => options?.method === 'POST')!;
  expect(JSON.parse(write[1].body)).toMatchObject({ farmId: 'farm', name: 'Este', maxStayDays: 10, mapX: 7, mapY: 5, mapWidth: 28, mapHeight: 30 });
});
it('preserves automatic coordinates when editing only the capacity of an existing paddock', async () => {
  const user = userEvent.setup(), saved = vi.fn();
  render(<PaddockEditor item={paddock('source', 2, false)} farms={farms} initialFarmId="farm" onSaved={saved} onCancel={vi.fn()} />);
  await user.clear(screen.getByLabelText('Capacidad máxima (animales)')); await user.type(screen.getByLabelText('Capacidad máxima (animales)'), '8');
  await user.click(screen.getByRole('button', { name: 'Guardar potrero' }));
  await waitFor(() => expect(saved).toHaveBeenCalled());
  const write = auth.request.mock.calls.find(([, options]) => options?.method === 'PUT')!;
  expect(JSON.parse(write[1].body)).toMatchObject({ capacity: 8, mapX: null, mapY: null, mapWidth: null, mapHeight: null });
});
it('rejects an out-of-bounds advanced position before submitting and keeps the draft', async () => {
  const user = userEvent.setup();
  render(<PaddockEditor farms={farms} initialFarmId="farm" onSaved={vi.fn()} onCancel={vi.fn()} />);
  await user.type(screen.getByLabelText('Nombre del potrero *'), 'Este');
  await user.click(screen.getByText('Coordenadas avanzadas (opcional)'));
  await user.clear(screen.getByLabelText('Posición X (%)')); await user.type(screen.getByLabelText('Posición X (%)'), '95');
  await user.click(screen.getByRole('button', { name: 'Guardar potrero' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('Revisa la ubicación');
  expect(screen.getByLabelText('Nombre del potrero *')).toHaveValue('Este');
  expect(auth.request.mock.calls.some(([, options]) => options?.method === 'POST')).toBe(false);
});

it('includes inactive occupied geometry and excludes records from another farm', async () => {
  const inactive = paddock('source'); inactive.data.isActive = false;
  const elsewhere = paddock('target'); elsewhere.data.farmId = 'other';
  auth.request.mockImplementation((path: string) => Promise.resolve(path === '/api/paddocks' ? [inactive, elsewhere].map(({ id, data }) => ({ id, data })) : []));
  render(<PaddockEditor farms={farms} initialFarmId="farm" onSaved={vi.fn()} onCancel={vi.fn()} />);
  expect(await screen.findByRole('img', { name: 'Norte: espacio ocupado · inactivo' })).toBeVisible();
  expect(screen.queryByRole('img', { name: /Sur: espacio ocupado/ })).not.toBeInTheDocument();
});

it('reuses a supplied geometry resource instead of loading the same catalog again', async () => {
  const source = paddock('source', 2, false);
  render(<PaddockEditor farms={farms} initialFarmId="farm" geometryResource={{ data: [source], loading: false, error: '', reload: vi.fn() }} onSaved={vi.fn()} onCancel={vi.fn()} />);
  expect(await screen.findByRole('img', { name: /Norte: espacio ocupado/ })).toBeVisible();
  expect(auth.request).not.toHaveBeenCalled();
});
