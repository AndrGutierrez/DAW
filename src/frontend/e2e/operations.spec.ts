import { test, expect } from '@playwright/test';
import type { Page } from '@playwright/test';
import { randomUUID } from 'node:crypto';
import { mkdir, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
let headers: Record<string, string>, farmId: string, categoryId: string, productId: string, inventoryId: string, animalId: string, prefix: string;
const username = process.env.E2E_ADMIN_USERNAME!, password = process.env.E2E_ADMIN_PASSWORD!;
const today = () => new Date().toISOString().slice(0, 10);
async function login(page: Page, employee = false) {
  await page.goto('/inventory'); await page.getByLabel('Usuario o correo').fill(employee ? process.env.E2E_EMPLOYEE_USERNAME! : username); await page.getByLabel('Contraseña', { exact: true }).fill(employee ? process.env.E2E_EMPLOYEE_PASSWORD! : password); await page.getByRole('button', { name: 'Entrar a mi finca' }).click(); await expect(page.getByRole('heading', { name: 'Inventario', exact: true })).toBeVisible();
}
test.beforeEach(async ({ request }, info) => {
  prefix = ('P4-OPS-' + info.project.name[0] + '-' + randomUUID().slice(0, 8)).toUpperCase(); animalId = '';
  const auth = await request.post('/api/auth/login', { data: { username, password } }); expect(auth.status()).toBe(200); headers = { Authorization: 'Bearer ' + (await auth.json()).accessToken };
  farmId = (await (await request.get('/api/farms', { headers })).json()).find((x: { data: { code: string } }) => x.data.code === 'DEMO').id;
  const category = await request.post('/api/categories', { headers, data: { name: prefix } }); expect(category.status()).toBe(201); categoryId = (await category.json()).id;
  const product = await request.post('/api/products', { headers, data: { sku: prefix, name: prefix, categoryId, price: 3, costPrice: 2, unit: 'Dose', withdrawalDays: 0 } }); expect(product.status()).toBe(201); productId = (await product.json()).id;
  const inventory = await request.post('/api/inventory', { headers, data: { farmId, productId, stock: 20.1234, minStock: 5, maxStock: 100, location: 'Test storage' } }); expect(inventory.status()).toBe(201); inventoryId = (await inventory.json()).id;
});
test.afterEach(async () => {
  // Traced movements are intentionally retained by the API. The local test run removes only manifest-owned fixtures through guarded SQL.
  const dir = join(tmpdir(), 'daw-phase4-operations'); await mkdir(dir, { recursive: true }); await writeFile(join(dir, prefix + '.json'), JSON.stringify({ prefix, categoryId, productId, inventoryId, animalId }));
});
test('inventory withdrawal preserves precision and uncertain retries cannot duplicate consumption', async ({ page, request }) => {
  await login(page); await page.getByLabel('Buscar por nombre o código').fill(prefix); await page.getByRole('button', { name: 'Movimientos', exact: true }).click();
  const dialog = page.getByRole('dialog', { name: prefix, exact: true }); await dialog.getByLabel('Cantidad (dosis)', { exact: true }).fill('2.1234'); await dialog.getByLabel('Motivo', { exact: true }).fill('Precise withdrawal');
  let intercepted = false;
  await page.route('**/api/inventory/' + inventoryId + '/movements', async route => { if (route.request().method() === 'POST' && !intercepted) { intercepted = true; await route.fetch(); await route.abort('failed'); } else await route.continue(); });
  await dialog.getByRole('button', { name: 'Registrar movimiento', exact: true }).click(); await expect(dialog.getByText('Conservamos este envío.', { exact: false })).toBeVisible(); await dialog.getByRole('button', { name: 'Reintentar y confirmar registro' }).click();
  await expect(page.getByRole('status').filter({ hasText: 'Registro confirmado; ya estaba guardado.' })).toBeVisible();
  const balance = await (await request.get('/api/inventory/' + inventoryId, { headers })).json(); expect(balance.data.stock).toBe(18);
  const history = await (await request.get('/api/inventory/' + inventoryId + '/movements', { headers })).json(); expect(history.total).toBe(2); expect(history.items.filter((m: { openingBalance: boolean }) => !m.openingBalance)).toHaveLength(1);
});
test('stale balance keeps form values until the user refreshes and confirms', async ({ page, request }) => {
  await login(page); await page.getByLabel('Buscar por nombre o código').fill(prefix); await page.getByRole('button', { name: 'Movimientos', exact: true }).click(); const dialog = page.getByRole('dialog', { name: prefix });
  await dialog.getByLabel('Cantidad (dosis)', { exact: true }).fill('2'); await dialog.getByLabel('Motivo', { exact: true }).fill('Keep this reason');
  expect((await request.post('/api/inventory/' + inventoryId + '/movements', { headers, data: { submissionId: randomUUID(), type: 'Out', quantity: 1, expectedStock: 20.1234, reason: 'Concurrent operator' } })).status()).toBe(201);
  await dialog.getByRole('button', { name: 'Registrar movimiento', exact: true }).click(); await expect(dialog.getByRole('alert')).toContainText('Otro usuario cambió el saldo'); await dialog.getByRole('button', { name: 'Actualizar saldo', exact: true }).click(); await expect(dialog.getByLabel('Motivo', { exact: true })).toHaveValue('Keep this reason'); await expect(dialog.getByLabel('Cantidad (dosis)', { exact: true })).toBeEnabled(); await dialog.getByRole('button', { name: 'Registrar movimiento', exact: true }).click(); await expect(page.getByRole('status').filter({ hasText: 'Registro guardado.' })).toBeVisible(); expect((await (await request.get('/api/inventory/' + inventoryId, { headers })).json()).data.stock).toBe(17.1234);
});
test('simultaneous withdrawals cannot spend the same stock twice', async ({ request }) => {
  const replies = await Promise.all([1, 2].map(() => request.post('/api/inventory/' + inventoryId + '/movements', { headers, data: { submissionId: randomUUID(), type: 'Out', quantity: 15, expectedStock: 20.1234, reason: 'Last available stock' } })));
  expect(replies.map(r => r.status()).sort()).toEqual([201, 409]); expect((await (await request.get('/api/inventory/' + inventoryId, { headers })).json()).data.stock).toBe(5.1234);
});
test('employee can record supplies while dashboard and product deletion remain forbidden', async ({ page, request }) => {
  await login(page, true); await expect(page.getByRole('link', { name: 'Dashboard', exact: true })).toHaveCount(0); await page.goto('/dashboard'); await expect(page.getByRole('alert')).toContainText('administrador');
  const auth = await request.post('/api/auth/login', { data: { username: process.env.E2E_EMPLOYEE_USERNAME!, password: process.env.E2E_EMPLOYEE_PASSWORD! } }); const employee = { Authorization: 'Bearer ' + (await auth.json()).accessToken };
  expect((await request.get('/api/analytics/overview?from=' + today() + '&to=' + today(), { headers: employee })).status()).toBe(403); expect((await request.delete('/api/products/' + productId, { headers: employee })).status()).toBe(403);
  await page.goto('/inventory'); await page.getByRole('button', { name: 'Productos', exact: true }).click(); await page.getByLabel('Buscar por nombre o código').fill(prefix); await expect(page.getByRole('button', { name: 'Archivar', exact: true })).toHaveCount(0); await expect(page.getByRole('button', { name: 'Editar producto', exact: true })).toHaveCount(0);
  expect((await request.post('/api/inventory/' + inventoryId + '/movements', { headers: employee, data: { submissionId: randomUUID(), type: 'In', quantity: 1, expectedStock: 20.1234, reason: 'Employee receipt' } })).status()).toBe(201);
});
test('administrator edits product category and stock thresholds and sees matching valuation', async ({ page, request }) => {
  await login(page); await page.getByRole('button', { name: 'Productos', exact: true }).click(); await page.getByLabel('Buscar por nombre o código').fill(prefix); await page.getByRole('button', { name: 'Editar producto', exact: true }).click(); await page.getByLabel('Costo unitario (USD)', { exact: true }).fill('4'); await page.getByRole('button', { name: 'Guardar cambios', exact: true }).click(); await expect(page.getByRole('dialog')).toHaveCount(0);
  await page.getByRole('button', { name: 'Categorías', exact: true }).click(); await page.getByLabel('Buscar por nombre o código').fill(prefix); await page.getByRole('button', { name: 'Editar categoría', exact: true }).click(); await page.getByLabel('Descripción', { exact: true }).fill('Sanitary test category'); await page.getByRole('button', { name: 'Guardar cambios', exact: true }).click(); await expect(page.getByRole('dialog')).toHaveCount(0);
  await page.getByRole('button', { name: 'Existencias', exact: true }).click(); await page.getByLabel('Buscar por nombre o código').fill(prefix); await page.getByRole('button', { name: 'Editar límites', exact: true }).click(); await page.getByLabel('Stock mínimo', { exact: true }).fill('25'); await page.getByRole('button', { name: 'Guardar cambios', exact: true }).click(); await expect(page.getByRole('dialog')).toHaveCount(0); await expect(page.getByText('Reponer existencias', { exact: true })).toBeVisible();
  const response = await request.get('/api/analytics/overview?from=' + today() + '&to=' + today() + '&farmId=' + farmId, { headers }); expect(response.status()).toBe(200); const dashboard = await response.json(); const category = dashboard.categories.find((c: { category: string }) => c.category === prefix); expect(category.cost).toBeCloseTo(20.1234 * 4, 4);
  await page.goto('/dashboard'); await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toBeVisible(); await expect(page.getByRole('table').filter({ hasText: prefix }).first()).toBeVisible(); expect(await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth)).toBe(false); await page.getByRole('button', { name: /Activar tema oscuro/ }).click(); await expect(page.locator('html')).toHaveClass(/dark/);
});
test('clinical and production reports export the filtered data as Excel and PDF', async ({ page, request }, info) => {
  const speciesId = (await (await request.get('/api/species', { headers })).json()).find((x: { data: { code: string } }) => x.data.code === 'BO').id;
  const animal = await request.post('/api/animals', { headers, data: { farmId, speciesId, internalTag: prefix, sex: 'Female', purpose: 'Milk', birthDate: '2022-01-01' } }); expect(animal.status()).toBe(201); animalId = (await animal.json()).id;
  const produced = await request.post('/api/animals/' + animalId + '/production', { headers, data: { submissionId: randomUUID(), date: today(), productType: 'Milk', method: 'Milking', quantity: 12.345, unit: 'Liter', notes: '=SUM(A1:A2) Observación ñ á é' } }); expect(produced.status()).toBe(201);
  const clinical = await request.post('/api/animals/' + animalId + '/clinical', { headers, data: { submissionId: randomUUID(), kind: 'DiseaseCase', date: today(), severity: 'Mild', notes: 'Observación clínica ñ á é', isContagious: false } }); expect(clinical.status()).toBe(201);
  await login(page); await page.goto('/reports'); await page.getByRole('button', { name: 'Producción', exact: true }).click(); await page.getByLabel('Desde', { exact: true }).fill(today()); await page.getByLabel('Hasta', { exact: true }).fill(today()); await page.getByRole('button', { name: 'Aplicar filtros', exact: true }).click(); await expect(page.getByRole('table')).toContainText(prefix);
  const dir = join(tmpdir(), 'daw-phase4-operations'); await mkdir(dir, { recursive: true });
  for (const [button, extension] of [['Exportar Excel', 'xlsx'], ['Exportar PDF', 'pdf']]) { const pending = page.waitForEvent('download'); await page.getByRole('button', { name: button, exact: true }).click(); const download = await pending; await download.saveAs(join(dir, 'production-' + info.project.name + '.' + extension)); }
  await page.getByRole('button', { name: 'Historial clínico', exact: true }).click(); await expect(page.getByRole('table')).toContainText(prefix);
  for (const [button, extension] of [['Exportar Excel', 'xlsx'], ['Exportar PDF', 'pdf']]) { const pending = page.waitForEvent('download'); await page.getByRole('button', { name: button, exact: true }).click(); const download = await pending; await download.saveAs(join(dir, 'clinical-' + info.project.name + '.' + extension)); }
  expect(await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth)).toBe(false);
});

test('dashboard refreshes visible inventory indicators after one minute', async ({ page, request }) => {
  await page.clock.install(); await login(page); await page.goto('/dashboard');
  const row = page.getByRole('article').filter({ has: page.getByRole('heading', { name: 'Valoración por categoría', exact: true }) }).getByRole('row').filter({ hasText: prefix }); await expect(row).toBeVisible(); const initial = await row.innerText();
  expect((await request.post('/api/inventory/' + inventoryId + '/movements', { headers, data: { submissionId: randomUUID(), type: 'In', quantity: 1, expectedStock: 20.1234, reason: 'Dashboard refresh evidence' } })).status()).toBe(201);
  await page.clock.runFor(60100); await expect.poll(async () => row.innerText()).not.toBe(initial); await expect(row).toContainText('42,25');
});
