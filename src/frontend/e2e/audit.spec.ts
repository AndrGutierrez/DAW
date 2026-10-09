import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { randomUUID } from 'node:crypto';

async function login(page: import('@playwright/test').Page, employee = false) {
  await page.goto('/login');
  await page.getByLabel('Usuario o correo').fill(process.env[employee ? 'E2E_EMPLOYEE_USERNAME' : 'E2E_ADMIN_USERNAME']!);
  await page.getByLabel('Contraseña', { exact: true }).fill(process.env[employee ? 'E2E_EMPLOYEE_PASSWORD' : 'E2E_ADMIN_PASSWORD']!);
  await page.getByRole('button', { name: 'Entrar a mi finca' }).click();
  await expect(page.locator('.app-header')).toBeVisible();
}

test('audit filters and on-demand before/after detail use persisted PostgreSQL data', async ({ page, request }) => {
  test.skip(process.env.E2E_ISOLATED_DATABASE !== '1', 'Mutating audit fixtures require a disposable database.');
  const session = await request.post('/api/auth/login', { data: { username: process.env.E2E_ADMIN_USERNAME, password: process.env.E2E_ADMIN_PASSWORD } }); expect(session.status()).toBe(200);
  const headers = { Authorization: 'Bearer ' + (await session.json()).accessToken };
  const farms = await (await request.get('/api/farms', { headers })).json(); const farm = farms.find((x: { data: { code: string } }) => x.data.code === 'DEMO');
  const species = (await (await request.get('/api/species', { headers })).json())[0];
  const tag = 'AUDIT-' + randomUUID().slice(0, 8);
  const draft = { farmId: farm.id, speciesId: species.id, internalTag: tag, name: 'Animal para auditoría', sex: 'Male', purpose: 'Meat' };
  const created = await request.post('/api/animals', { headers, data: draft }); expect(created.status()).toBe(201); const animal = await created.json();
  expect((await request.put('/api/animals/' + animal.id, { headers, data: { ...draft, name: 'Nombre actualizado' } })).status()).toBe(200);
  await login(page); await page.goto('/auditlogs'); await expect(page.getByRole('heading', { name: 'Auditoría', exact: true })).toBeVisible();
  await expect(page.getByLabel('Acción', { exact: true }).getByRole('option', { name: 'Modificación', exact: true })).toHaveCount(1);
  await page.getByLabel('Buscar registro').fill(animal.id); await page.getByLabel('Acción', { exact: true }).selectOption('Modified'); await page.getByLabel('Tipo de registro').selectOption('Animal'); await page.getByLabel('Finca', { exact: true }).selectOption(farm.id);
  await page.getByRole('button', { name: 'Filtrar', exact: true }).click();
  const table = page.getByRole('table', { name: 'Eventos de auditoría' }); await expect(table.getByRole('row')).toHaveCount(2);
  const trigger = table.getByRole('button', { name: /Ver evento de Animal/ }); await trigger.click();
  const panel = page.getByRole('dialog', { name: 'Detalle del evento' }); await expect(panel).toBeVisible();
  const changes = panel.getByRole('table', { name: 'Cambios del evento' }); await expect(changes).toContainText('Animal para auditoría'); await expect(changes).toContainText('Nombre actualizado');
  await expect(changes).not.toContainText('Identificación interna'); await panel.getByLabel('Mostrar campos sin cambios').check(); await expect(changes).toContainText('Identificación interna');
  await expect(panel.getByText('No registrada en este evento', { exact: true })).toHaveCount(0);
  expect((await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa']).analyze()).violations).toEqual([]);
  expect(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth)).toBe(false);
  await page.keyboard.press('Escape'); await expect(panel).not.toBeVisible(); await expect(trigger).toBeFocused();
  await page.getByRole('button', { name: 'Limpiar', exact: true }).click(); await expect(page.getByLabel('Buscar registro')).toHaveValue('');
  const query = await request.get('/api/admin/auditlogs?entityName=Animal&search=' + animal.id, { headers }); expect(query.status()).toBe(200); const history = (await query.json()).items;
  expect(history.some((x: { action: string }) => x.action === 'Added')).toBe(true); expect(history.some((x: { action: string }) => x.action === 'Modified')).toBe(true);
  expect(history[0]).not.toHaveProperty('newValues');
});

test('operator cannot consult audit data or access administrative controls', async ({ page, request }) => {
  const response = await request.post('/api/auth/login', { data: { username: process.env.E2E_EMPLOYEE_USERNAME, password: process.env.E2E_EMPLOYEE_PASSWORD } }); expect(response.status()).toBe(200);
  const headers = { Authorization: 'Bearer ' + (await response.json()).accessToken };
  expect((await request.get('/api/admin/auditlogs', { headers })).status()).toBe(403); expect((await request.get('/api/admin/auditlogs/options', { headers })).status()).toBe(403);
  await login(page, true); await page.goto('/auditlogs'); await expect(page.getByRole('alert')).toContainText('administrador'); await expect(page.getByRole('navigation').getByRole('link', { name: 'Auditoría', exact: true })).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Filtrar', exact: true })).toHaveCount(0);
});

test('browser login, rejected login and logout have credential-free security events', async ({ page, request }) => {
  await page.goto('/login'); await page.getByLabel('Usuario o correo').fill(process.env.E2E_ADMIN_USERNAME!); await page.getByLabel('Contraseña', { exact: true }).fill('Rejected123!Login'); await page.getByRole('button', { name: 'Entrar a mi finca' }).click(); await expect(page.getByRole('alert')).toBeVisible();
  await page.getByLabel('Contraseña', { exact: true }).fill(process.env.E2E_ADMIN_PASSWORD!); await page.getByRole('button', { name: 'Entrar a mi finca' }).click(); await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Salir', exact: true }).click(); await expect(page.getByRole('heading', { name: 'Inicia sesión', exact: true })).toBeVisible();
  const response = await request.post('/api/auth/login', { data: { username: process.env.E2E_ADMIN_USERNAME, password: process.env.E2E_ADMIN_PASSWORD } }); const headers = { Authorization: 'Bearer ' + (await response.json()).accessToken };
  const events = (await (await request.get('/api/admin/auditlogs?entityName=Authentication&pageSize=100', { headers })).json()).items;
  for (const action of ['LoginSucceeded', 'LoginRejected', 'Logout']) expect(events.some((x: { action: string }) => x.action === action)).toBe(true);
  const rejected = events.find((x: { action: string }) => x.action === 'LoginRejected'); expect(rejected.userId).toBeNull();
  const detail = await (await request.get('/api/admin/auditlogs/' + rejected.id, { headers })).json(); expect(JSON.stringify(detail)).not.toContain('Rejected123!Login'); expect(detail.entry.entityId).toBeNull();
});
