// @vitest-environment jsdom
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { expect, it, vi } from 'vitest';
import { WeighingPage } from './WeighingPage';
const auth = vi.hoisted(() => ({ can: vi.fn().mockReturnValue(true), request: vi.fn() }));
vi.mock('../auth/AuthContext', () => ({ useAuth: () => auth }));
vi.mock('../components/Feedback', () => ({ useFeedback: () => ({ confirm: vi.fn() }) }));
vi.mock('../components/WeighingForm', () => ({ WeighingForm: () => <div>Weight editor</div> }));
it('selects every search result beyond the visible page and the former fifty-animal cap', async () => {
  const animals = Array.from({ length: 103 }, (_, i) => ({ id: String(i), internalTag: 'COW-' + i, currentWeightKg: 100, farmId: 'farm' }));
  auth.request.mockImplementation((path: string) => {
    if (path === '/api/farms') return Promise.resolve([{ id: 'farm', data: { name: 'Finca', isActive: true } }]);
    if (path === '/api/lots') return Promise.resolve([]);
    const params = new URLSearchParams(path.split('?')[1]); const page = Number(params.get('page')), size = Number(params.get('pageSize'));
    return Promise.resolve({ items: animals.slice((page - 1) * size, page * size), total: 103, page, pageSize: size });
  });
  const user = userEvent.setup(); render(<MemoryRouter><WeighingPage /></MemoryRouter>);
  await user.click(await screen.findByRole('button', { name: 'Seleccionar todos los resultados (103)' }));
  expect(await screen.findByText('103 animales seleccionados')).toBeVisible();
  await user.click(screen.getByRole('button', { name: 'Comenzar pesaje' }));
  expect(await screen.findByText('ANIMAL 1 DE 103')).toBeVisible();
});
