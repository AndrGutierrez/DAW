// @vitest-environment jsdom
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, expect, it, vi } from 'vitest';
import { AnimalEditorPage } from './AnimalEditorPage';
import { ApiError } from '../auth/session';
const auth = vi.hoisted(() => ({ can: vi.fn(), request: vi.fn() }));
vi.mock('../auth/AuthContext', () => ({ useAuth: () => auth }));
vi.mock('../components/NavigationProtection', () => ({ useUnsavedChanges: () => vi.fn() }));
vi.mock('../components/Feedback', () => ({ useFeedback: () => ({ notify: vi.fn(), confirm: vi.fn() }) }));
const catalog: Record<string, unknown> = {
 '/api/farms': [{ id: 'farm-a', data: { name: 'Farm A', isActive: true } }, { id: 'farm-b', data: { name: 'Farm B', isActive: true } }],
 '/api/species': [{ id: 'cattle', data: { name: 'Bovino', isActive: true } }, { id: 'sheep', data: { name: 'Ovino', isActive: true } }],
 '/api/breeds': [{ id: 'breed-a', data: { name: 'Raza A', speciesId: 'cattle', isActive: true } }],
 '/api/lots': [{ id: 'lot-a', data: { name: 'Lote A', farmId: 'farm-a', speciesId: 'cattle', isActive: true } }],
 '/api/paddocks': [{ id: 'paddock-a', data: { name: 'Potrero A', farmId: 'farm-a', isActive: true } }]
};
beforeEach(() => { auth.can.mockImplementation((permission: string) => permission !== 'animals.list'); auth.request.mockReset(); auth.request.mockImplementation((path: string) => Promise.resolve(catalog[path])); });
function show() { render(<MemoryRouter><AnimalEditorPage /></MemoryRouter>); }
it('announces missing identification fields without submitting the form', async () => {
 const user = userEvent.setup(); show(); await waitFor(() => expect(screen.getByRole('button', { name: 'Guardar animal' })).toBeEnabled());
 await user.click(screen.getByRole('button', { name: 'Guardar animal' }));
 expect(screen.getByRole('alert')).toHaveTextContent('Revisa los campos');
 expect(auth.request.mock.calls.some(([, options]) => options?.method === 'POST')).toBe(false);
 await waitFor(() => expect(screen.getByLabelText('Finca *')).toHaveFocus());
});
it('clears incompatible breed and lot selections when the species changes', async () => {
 const user = userEvent.setup(); show(); await screen.findByRole('option', { name: 'Farm A' });
 await user.selectOptions(screen.getByLabelText('Finca *'), 'farm-a'); await user.selectOptions(screen.getByLabelText('Especie *'), 'cattle');
 await user.selectOptions(screen.getByLabelText('Raza'), 'breed-a'); await user.selectOptions(screen.getByLabelText('Lote'), 'lot-a');
 await user.selectOptions(screen.getByLabelText('Especie *'), 'sheep');
 expect(screen.getByLabelText('Raza')).toHaveValue(''); expect(screen.getByLabelText('Lote')).toHaveValue('');
 expect(screen.queryByRole('option', { name: 'Raza A' })).not.toBeInTheDocument();
});
it('maps server field errors to the control while retaining the draft', async () => {
 auth.request.mockImplementation((path: string, options?: { method: string }) => options?.method === 'POST' ? Promise.reject(new ApiError(400, { errors: { InternalTag: ['already exists'] } })) : Promise.resolve(catalog[path]));
 const user = userEvent.setup(); show(); await screen.findByRole('option', { name: 'Farm A' });
 await user.selectOptions(screen.getByLabelText('Finca *'), 'farm-a'); await user.selectOptions(screen.getByLabelText('Especie *'), 'cattle');
 await user.type(screen.getByLabelText('Arete interno *'), 'COW-001'); await user.click(screen.getByRole('button', { name: 'Guardar animal' }));
 await waitFor(() => expect(screen.getByLabelText('Arete interno *')).toHaveAttribute('aria-invalid', 'true'));
 expect(screen.getByLabelText('Arete interno *')).toHaveValue('COW-001');
 expect(screen.getByLabelText('Arete interno *')).toHaveAccessibleDescription('already exists');
});
it('does not load catalogs for an account without creation permission', () => {
 auth.can.mockReturnValue(false); show(); expect(screen.getByRole('heading', { name: 'Sin acceso a esta acción' })).toBeInTheDocument();
 expect(auth.request).not.toHaveBeenCalled();
});
