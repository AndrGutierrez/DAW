import { test, expect } from '@playwright/test';
import type { Page } from '@playwright/test';
import { randomUUID } from 'node:crypto';

const username = process.env.E2E_ADMIN_USERNAME!, password = process.env.E2E_ADMIN_PASSWORD!;
let headers: Record<string, string>, farmId: string, speciesId: string, prefix: string;
let animals: string[];
async function login(page: Page, path = '/animals') {
  await page.goto(path);
  await page.getByLabel('Usuario o correo').fill(username);
  await page.getByLabel('Contraseña', { exact: true }).fill(password);
  await page.getByRole('button', { name: 'Entrar a mi finca' }).click();
  await expect(page.getByRole('link', { name: 'Animales', exact: true })).toBeVisible();
}
test.beforeEach(async ({ request }, info) => {
  animals = []; prefix = ('P4-UX-' + info.project.name[0] + '-' + randomUUID().slice(0, 8)).toUpperCase();
  const auth = await request.post('/api/auth/login', { data: { username, password } }); expect(auth.status()).toBe(200);
  headers = { Authorization: 'Bearer ' + (await auth.json()).accessToken };
  farmId = (await (await request.get('/api/farms', { headers })).json()).find((f: { data: { code: string } }) => f.data.code === 'DEMO').id;
  speciesId = (await (await request.get('/api/species', { headers })).json()).find((s: { data: { code: string } }) => s.data.code === 'BO').id;
});
test.afterEach(async ({ request }) => {
  for (const id of animals.reverse()) expect((await request.delete('/api/animals/' + id, { headers })).status()).toBe(204);
});

test('password visibility preserves value and focus and Enter sends only once while pending', async ({ page }) => {
  await page.goto('/login');
  await page.getByLabel('Usuario o correo').fill(username);
  const input = page.getByLabel('Contraseña', { exact: true }); await input.fill(password);
  await input.evaluate((element: HTMLInputElement) => { element.focus(); element.setSelectionRange(2, 5); });
  await page.getByRole('button', { name: 'Mostrar contraseña' }).click();
  await expect(input).toHaveAttribute('type', 'text'); await expect(input).toHaveValue(password); await expect(input).toBeFocused();
  expect(await input.evaluate((element: HTMLInputElement) => [element.selectionStart, element.selectionEnd])).toEqual([2, 5]);
  await page.getByRole('button', { name: 'Ocultar contraseña' }).focus(); await page.keyboard.press('Space');
  await expect(input).toHaveAttribute('type', 'password'); await expect(input).toHaveAttribute('autocomplete', 'current-password');
  let calls = 0; let release!: () => void; const gate = new Promise<void>(resolve => { release = resolve; });
  await page.route('**/api/auth/session/login', async route => { calls++; await gate; await route.continue(); });
  await input.focus(); await page.keyboard.press('Enter'); await expect(page.getByRole('button', { name: 'Iniciando sesión…' })).toBeDisabled();
  await page.keyboard.press('Enter'); await expect.poll(() => calls).toBe(1); await expect(input).toHaveValue(password); release();
  await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toBeVisible();
});

test('login distinguishes missing fields, invalid credentials and connection failure without losing values', async ({ page }) => {
  await page.goto('/login'); await page.getByRole('button', { name: 'Entrar a mi finca' }).click();
  await expect(page.getByLabel('Usuario o correo')).toBeFocused(); await expect(page.getByLabel('Usuario o correo')).toHaveAttribute('aria-invalid', 'true');
  await expect(page.getByText('Escribe tu contraseña.', { exact: true })).toBeVisible();
  await page.getByLabel('Usuario o correo').fill('unknown-user'); await page.getByLabel('Contraseña', { exact: true }).fill('wrong-password');
  await page.getByRole('button', { name: 'Entrar a mi finca' }).click();
  const notice = page.getByRole('alert', { name: 'Revisa tus credenciales' }); await expect(notice).toBeVisible(); await expect(notice).toBeFocused(); await expect(notice).toContainText('administrador');
  await page.route('**/api/auth/session/login', route => route.abort('failed'));
  await page.getByLabel('Contraseña', { exact: true }).press('Enter'); await expect(page.getByRole('alert', { name: 'No pudimos conectar' })).toContainText('Conservamos los datos');
  await expect(page.getByLabel('Usuario o correo')).toHaveValue('unknown-user'); await expect(page.getByLabel('Contraseña', { exact: true })).toHaveValue('wrong-password');
  expect(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth)).toBe(false);
});

test('expired session explains the return to login and preserves the intended destination', async ({ page }) => {
  await login(page); await expect(page.getByRole('heading', { name: 'Animales', exact: true })).toBeVisible(); await page.waitForLoadState('networkidle');
  await page.route('**/api/farms', route => route.fulfill({ status: 401, contentType: 'application/problem+json', body: JSON.stringify({ detail: 'Expired bearer' }) }));
  await page.route('**/api/auth/session/refresh', route => route.fulfill({ status: 401, contentType: 'application/problem+json', body: JSON.stringify({ detail: 'Expired session' }) }));
  await page.getByRole('link', { name: 'Pesaje', exact: true }).click();
  await expect(page.getByRole('status', { name: 'Tu sesión terminó' })).toBeVisible();
  await page.unroute('**/api/farms'); await page.unroute('**/api/auth/session/refresh');
  await page.getByLabel('Usuario o correo').fill(username); await page.getByLabel('Contraseña', { exact: true }).fill(password); await page.getByRole('button', { name: 'Entrar a mi finca' }).click();
  await expect(page).toHaveURL(new RegExp('/weighing$'));
});

test('animal selector searches on the server, paginates and restores focus after keyboard selection', async ({ page, request }) => {
  test.setTimeout(60000);
  for (let i = 1; i <= 21; i++) {
    const result = await request.post('/api/animals', { headers, data: { farmId, speciesId, internalTag: prefix + '-' + String(i).padStart(2, '0'), sex: 'Female', purpose: 'Meat', birthDate: '2020-01-01' } }); expect(result.status()).toBe(201); animals.push((await result.json()).id);
  }
  await login(page, '/animals/new'); await page.getByLabel('Finca *', { exact: true }).selectOption(farmId); await page.getByLabel('Especie *', { exact: true }).selectOption(speciesId);
  const trigger = page.getByRole('button', { name: 'Madre', exact: true }); await trigger.click();
  const dialog = page.getByRole('dialog', { name: 'Seleccionar madre' }); const search = page.getByLabel('Buscar madre'); await expect(search).toBeFocused();
  const response = page.waitForResponse(r => r.url().includes('/api/animals/page?') && new URL(r.url()).searchParams.get('search') === prefix);
  await search.fill(prefix); await response; await expect(dialog.getByText('21 candidatos · página 1')).toBeVisible();
  await dialog.getByRole('button', { name: 'Siguiente', exact: true }).click(); await expect(dialog.getByText('21 candidatos · página 2')).toBeVisible();
  await expect(dialog.getByRole('button', { name: prefix + '-21', exact: true })).toBeVisible();
  await search.focus(); await search.press('ArrowDown'); await page.keyboard.press('End'); await expect(dialog.getByRole('button', { name: prefix + '-21', exact: true })).toBeFocused(); await page.keyboard.press('Enter');
  await expect(dialog).not.toBeVisible(); await expect(trigger).toBeFocused(); await expect(trigger).toContainText(prefix + '-21');
  await trigger.click(); await search.press('Escape'); await expect(dialog).not.toBeVisible(); await expect(trigger).toBeFocused();
  expect(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth)).toBe(false);
});

test('search picker stays inside the inventory dialog and Escape closes only the picker', async ({ page, request }, info) => {
  const created = await request.post('/api/animals', { headers, data: { farmId, speciesId, internalTag: prefix, sex: 'Female', purpose: 'Meat' } }); expect(created.status()).toBe(201); animals.push((await created.json()).id);
  await login(page, '/inventory'); await page.getByRole('button', { name: 'Movimientos', exact: true }).first().click();
  const panel = page.locator('dialog.side-panel'); const trigger = panel.getByRole('button', { name: 'Animal (opcional, solo salidas)' }); await trigger.click();
  const popup = panel.getByRole('dialog', { name: 'Seleccionar animal (opcional, solo salidas)' }); await expect(popup).toBeVisible();
  const search = popup.getByRole('searchbox'); await search.fill(prefix); await popup.getByRole('button', { name: prefix, exact: true }).click();
  await expect(trigger).toContainText(prefix); await trigger.click(); await popup.getByRole('searchbox').press('Escape');
  await expect(popup).not.toBeVisible(); await expect(panel).toBeVisible(); await expect(trigger).toBeFocused();
  await trigger.click();
  if (info.project.name === 'desktop') { const amount = panel.getByLabel('Cantidad', { exact: false }); await amount.click(); await expect(popup).not.toBeVisible(); await expect(amount).toBeFocused(); }
  else { await panel.getByRole('heading', { level: 2 }).click(); await expect(popup).not.toBeVisible(); }
  await expect(trigger).toContainText(prefix);
  expect(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth)).toBe(false);
});
