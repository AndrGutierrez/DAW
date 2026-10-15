// @vitest-environment jsdom
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, expect, it, vi } from 'vitest';
import { PaddocksPage } from './PaddocksPage';
import { paddock } from '../test/paddocks';

const mocks = vi.hoisted(() => ({ request: vi.fn(), plan: vi.fn(), editor: vi.fn() }));
vi.mock('../auth/AuthContext', () => ({ useAuth: () => ({ can: () => true, isAdmin: true, request: mocks.request }) }));
vi.mock('../components/Feedback', () => ({ useFeedback: () => ({ notify: vi.fn(), confirm: vi.fn() }) }));
vi.mock('../components/NavigationProtection', () => ({ useUnsavedChanges: () => vi.fn() }));
vi.mock('../components/PaddockPlan', () => ({ PaddockPlan: (props: unknown) => { mocks.plan(props); return null; } }));
vi.mock('../components/PaddockEditor', () => ({ PaddockEditor: (props: { onSaved: () => void }) => { mocks.editor(props); return <button onClick={props.onSaved}>Save editor fixture</button>; } }));

beforeEach(() => { mocks.request.mockReset(); mocks.plan.mockReset(); mocks.editor.mockReset(); });

it('shares the complete geometry with the editor and refreshes it together with active snapshots', async () => {
  const user = userEvent.setup(), source = paddock('source', 2, false), inactive = paddock('inactive', 0, false);
  inactive.data.isActive = false;
  const catalog = [source, inactive].map(({ id, data }) => ({ id, data }));
  mocks.request.mockImplementation((path: string) => {
    if (path === '/api/farms') return Promise.resolve([{ id: 'farm', data: { name: 'Finca', isActive: true } }]);
    if (path === '/api/paddocks') return Promise.resolve(catalog);
    if (path.startsWith('/api/paddocks/destinations')) return Promise.resolve([source]);
    return Promise.resolve({ items: [], total: 0, page: 1, pageSize: 12 });
  });
  render(<MemoryRouter initialEntries={['/paddocks?farm=farm']}><PaddocksPage /></MemoryRouter>);
  await waitFor(() => expect(mocks.plan).toHaveBeenCalledWith(expect.objectContaining({ items: [source], geometryItems: catalog })));
  expect(mocks.request.mock.calls.filter(([path]) => path === '/api/paddocks')).toHaveLength(1);
  await user.click(screen.getByRole('button', { name: 'Registrar potrero' }));
  expect(mocks.editor).toHaveBeenCalledWith(expect.objectContaining({ geometryResource: expect.objectContaining({ data: catalog }) }));
  expect(mocks.request.mock.calls.filter(([path]) => path === '/api/paddocks')).toHaveLength(1);
  await user.click(screen.getByRole('button', { name: 'Save editor fixture' }));
  await waitFor(() => {
    expect(mocks.request.mock.calls.filter(([path]) => path === '/api/paddocks')).toHaveLength(2);
    expect(mocks.request.mock.calls.filter(([path]) => path.startsWith('/api/paddocks/destinations'))).toHaveLength(2);
  });
});
