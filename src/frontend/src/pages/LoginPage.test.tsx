// @vitest-environment jsdom
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '../auth/session';
import { LoginPage } from './LoginPage';
const auth = vi.hoisted(() => ({ status: 'anonymous', reason: null, login: vi.fn(), isAdmin: false, can: vi.fn() }));
vi.mock('../auth/AuthContext', () => ({ useAuth: () => auth }));
vi.mock('../theme/ThemeContext', () => ({ useTheme: () => ({ theme: 'light', toggle: vi.fn() }) }));
function show() { return render(<MemoryRouter><LoginPage /></MemoryRouter>); }
beforeEach(() => { auth.login.mockReset(); });
describe('login interaction', () => {
 it('labels both fields and focuses the first missing credential without requesting access', async () => {
  const user = userEvent.setup(); show();
  await user.click(screen.getByRole('button', { name: 'Entrar a mi finca' }));
  expect(auth.login).not.toHaveBeenCalled();
  const username = screen.getByLabelText('Usuario o correo');
  await waitFor(() => expect(username).toHaveFocus());
  expect(username).toHaveAccessibleDescription('Escribe tu usuario o correo.');
  expect(screen.getByLabelText('Contraseña')).toHaveAttribute('aria-invalid', 'true');
 });
 it('reveals the password without clearing the value', async () => {
  const user = userEvent.setup(); show();
  const password = screen.getByLabelText('Contraseña');
  await user.type(password, 'a-private-value');
  await user.click(screen.getByRole('button', { name: 'Mostrar contraseña' }));
  expect(password).toHaveAttribute('type', 'text');
  expect(password).toHaveValue('a-private-value');
  await user.click(screen.getByRole('button', { name: 'Ocultar contraseña' }));
  expect(password).toHaveAttribute('type', 'password');
 });
 it('announces rejected credentials, keeps values and allows correction', async () => {
  auth.login.mockRejectedValue(new ApiError(401, { title: 'Rejected' }));
  const user = userEvent.setup(); show();
  await user.type(screen.getByLabelText('Usuario o correo'), 'farmer');
  await user.type(screen.getByLabelText('Contraseña'), 'incorrect');
  await user.click(screen.getByRole('button', { name: 'Entrar a mi finca' }));
  const alert = await screen.findByRole('alert');
  expect(alert).toHaveTextContent('Revisa tus credenciales');
  expect(alert).toHaveFocus();
  expect(screen.getByLabelText('Usuario o correo')).toHaveValue('farmer');
  expect(screen.getByLabelText('Contraseña')).toHaveValue('incorrect');
  expect(screen.getByRole('button', { name: 'Entrar a mi finca' })).toBeEnabled();
 });
 it('prevents a second submit while access is pending', async () => {
  let finish!: () => void; auth.login.mockImplementation(() => new Promise<void>(resolve => { finish = resolve; }));
  const user = userEvent.setup(); show();
  await user.type(screen.getByLabelText('Usuario o correo'), 'farmer');
  await user.type(screen.getByLabelText('Contraseña'), 'private');
  await user.dblClick(screen.getByRole('button', { name: 'Entrar a mi finca' }));
  expect(auth.login).toHaveBeenCalledTimes(1);
  expect(screen.getByRole('button', { name: 'Iniciando sesión…' })).toBeDisabled();
  finish();
  await waitFor(() => expect(screen.getByRole('button', { name: 'Entrar a mi finca' })).toBeEnabled());
 });
 it('distinguishes a network failure from invalid credentials', async () => {
  auth.login.mockRejectedValue(new TypeError('Offline'));
  const user = userEvent.setup(); show();
  await user.type(screen.getByLabelText('Usuario o correo'), 'farmer');
  await user.type(screen.getByLabelText('Contraseña'), 'private');
  await user.click(screen.getByRole('button', { name: 'Entrar a mi finca' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('No pudimos conectar');
 });
});
