import { test, expect } from '@playwright/test';
import { randomUUID } from 'node:crypto';
import QRCode from 'qrcode';
import { mkdir } from 'node:fs/promises';
import { join } from 'node:path';
import { tmpdir } from 'node:os';
let headers: Record<string, string>, farmId: string, animalId: string, tag: string;
let paddocksCreated: string[] = [];
const username = process.env.E2E_ADMIN_USERNAME!, password = process.env.E2E_ADMIN_PASSWORD!;
test.beforeEach(async ({ request }) => {
  animalId = ""; paddocksCreated = [];
  test.skip(process.env.E2E_ISOLATED_DATABASE !== '1', 'Requires an explicitly disposable database.');
  const login = await request.post('/api/auth/login', { data: { username, password } }); expect(login.status()).toBe(200);
  headers = { Authorization: 'Bearer ' + (await login.json()).accessToken };
  farmId = (await (await request.get('/api/farms', { headers })).json()).find((f: { data: { code: string } }) => f.data.code === 'DEMO').id;
  const speciesId = (await (await request.get('/api/species', { headers })).json()).find((s: { data: { code: string } }) => s.data.code === 'BO').id;
  tag = 'P5-' + randomUUID().slice(0, 8).toUpperCase();
  const animal = await request.post('/api/animals', { headers, data: { farmId, speciesId, internalTag: tag, rfid: tag + '-RFID', sex: 'Female', purpose: 'Milk', healthStatus: 'Quarantine' } }); expect(animal.status()).toBe(201); animalId = (await animal.json()).id;
});
test.afterEach(async ({ request }) => { if (animalId) expect((await request.delete('/api/animals/' + animalId, { headers })).status()).toBe(204); for (const id of paddocksCreated) expect((await request.delete('/api/paddocks/' + id, { headers })).status()).toBe(204); });
async function login(page: import('@playwright/test').Page) {
  await page.goto('/animals'); await page.getByLabel('Usuario o correo').fill(username); await page.getByLabel('Contraseña', { exact: true }).fill(password); await page.getByRole('button', { name: 'Entrar a mi finca' }).click(); await expect(page.getByRole('heading', { name: 'Animales', exact: true })).toBeVisible();
}
test('real QR image decoder opens the authorized animal and downloads its label', async ({ page }, info) => {
  await login(page); await page.getByRole('button', { name: 'Leer QR / NFC' }).click(); const dialog = page.getByRole('dialog', { name: 'Identificar animal' });
  // Delay scripts loaded by the upload without depending on the bundler's chunk names.
  let decoderRequested = false;
  const slowDecoder = async (route: import('@playwright/test').Route) => { decoderRequested = true; await new Promise(resolve => setTimeout(resolve, 6000)); await route.continue(); };
  await page.route('**/assets/*.js', slowDecoder);
  const buffer = await QRCode.toBuffer('daw:animal:' + animalId, { width: 500, errorCorrectionLevel: 'M' });
  await dialog.getByLabel('Cargar imagen QR').setInputFiles({ name: 'animal.png', mimeType: 'image/png', buffer });
  const identifier = dialog.getByLabel('Identificación leída');
  await expect(identifier).toBeDisabled();
  await expect(identifier).toBeEnabled({ timeout: 15_000 });
  expect(decoderRequested).toBe(true);
  await page.unroute('**/assets/*.js', slowDecoder);
  await expect(identifier).toHaveValue('daw:animal:' + animalId); await dialog.getByRole('button', { name: 'Buscar animal identificado' }).click(); await expect(page).toHaveURL(new RegExp('/animals/' + animalId + '$'));
  await page.getByRole('button', { name: 'Etiqueta QR / NFC' }).click(); const label = page.getByRole('dialog', { name: 'Etiqueta de ' + tag }); await expect(label.getByRole('img', { name: 'Código QR del animal ' + tag })).toBeVisible();
  const download = page.waitForEvent('download'); await label.getByRole('link', { name: 'Descargar QR' }).click(); expect((await download).suggestedFilename()).toBe('animal-' + tag + '.png');
  const dir = join(tmpdir(), 'daw-phase5-evidence'); await mkdir(dir, { recursive: true }); await page.screenshot({ path: join(dir, 'qr-' + info.project.name + '.png') });
});
test('header notifications expose details and navigate to the correct animal', async ({ page }, info) => {
  await login(page); await page.getByRole('button', { name: /^Notificaciones:/ }).click(); const dialog = page.getByRole('dialog', { name: 'Notificaciones', exact: true });
  await dialog.getByRole('button', { name: 'Sanidad', exact: true }).click(); const item = dialog.locator('li').filter({ hasText: tag }); await expect(item).toContainText('En cuarentena');
  const dir = join(tmpdir(), 'daw-phase5-evidence'); await mkdir(dir, { recursive: true }); await page.screenshot({ path: join(dir, 'notifications-' + info.project.name + '.png') });
  await item.getByRole('link', { name: 'Revisar sanidad' }).click(); await expect(page).toHaveURL(new RegExp('/animals/' + animalId + '\\?tab=clinical$')); await expect(page.getByRole('dialog')).toHaveCount(0);
});
test('inventory version blocks an ABA balance and a retry cannot create a duplicate', async ({ request }) => {
  const categories = await (await request.get('/api/categories', { headers })).json();
  const productReply = await request.post('/api/products', { headers, data: { sku: tag, name: tag, categoryId: categories[0].id, price: 10, costPrice: 5, unit: 'Dose' } }); expect(productReply.status()).toBe(201); const product = await productReply.json();
  const stockReply = await request.post('/api/inventory', { headers, data: { farmId, productId: product.id, stock: 20, minStock: 5, maxStock: 100 } }); expect(stockReply.status()).toBe(201); const stock = await stockReply.json(); const url = '/api/inventory/' + stock.id + '/movements';
  const first = { submissionId: randomUUID(), type: 'Out', quantity: 1, expectedStock: 20, expectedVersion: stock.data.version, reason: 'First operation' };
  expect((await request.post(url, { headers, data: first })).status()).toBe(201);
  const updated = await (await request.get('/api/inventory/' + stock.id, { headers })).json();
  expect((await request.post(url, { headers, data: { submissionId: randomUUID(), type: 'In', quantity: 1, expectedStock: 19, expectedVersion: updated.data.version, reason: 'Return balance' } })).status()).toBe(201);
  expect((await request.post(url, { headers, data: { ...first, submissionId: randomUUID() } })).status()).toBe(409);
  expect((await request.post(url, { headers, data: first })).status()).toBe(200);
  const current = await (await request.get('/api/inventory/' + stock.id, { headers })).json(); expect(current.data.stock).toBe(20); expect(current.data.version).toBe(stock.data.version + 2);
  const responses = await Promise.all([1, 2].map(() => request.post(url, { headers, data: { ...first, submissionId: randomUUID(), expectedVersion: current.data.version, quantity: 15, reason: 'Concurrent withdrawal' } })));
  expect(responses.map(r => r.status()).sort()).toEqual([201, 409]); expect((await (await request.get('/api/inventory/' + stock.id, { headers })).json()).data.stock).toBe(5);
});

test('the transfer map projects occupancy and commits the selected visual destination', async ({ page, request }, info) => {
  async function paddock(name: string, x: number) { const result = await request.post('/api/paddocks', { headers, data: { farmId, name, capacity: 2, mapX: x, mapY: 88, mapWidth: 15, mapHeight: 10 } }); expect(result.status()).toBe(201); const id = (await result.json()).id; paddocksCreated.push(id); return id; }
  const source = await paddock(tag + ' Origen', 3), target = await paddock(tag + ' Destino', 45);
  expect((await request.post('/api/animal-movements/batch', { headers, data: { farmId, toPaddockId: source, reason: 'Initial fixture location', animals: [{ animalId, submissionId: randomUUID(), expectedFromPaddockId: null, expectedFromLotId: null }] } })).status()).toBe(201);
  await login(page); await page.goto('/transfers'); await page.getByLabel('Finca', { exact: true }).selectOption(farmId); await page.getByLabel('Buscar animales para trasladar').fill(tag); await page.getByRole('button', { name: 'Buscar', exact: true }).click(); await page.getByLabel('Seleccionar ' + tag, { exact: true }).check();
  const map = page.getByRole('group', { name: 'Plano interactivo de potreros' }); await map.getByRole('button', { name: new RegExp('^' + tag + ' Destino:') }).click(); await expect(page.getByLabel('Potrero de destino *')).toHaveValue(target); await expect(page.getByLabel('Ruta del traslado')).toContainText(tag + ' Origen → ' + tag + ' Destino'); await expect(map.getByRole('button', { name: new RegExp('^' + tag + ' Destino: 1 animales;') })).toHaveAttribute('aria-pressed', 'true');
  const dir = join(tmpdir(), 'daw-phase5-evidence'); await mkdir(dir, { recursive: true }); await page.screenshot({ path: join(dir, 'transfer-map-' + info.project.name + '.png'), fullPage: true });
  await page.getByLabel('Motivo del traslado *').fill('Visual destination selection'); await page.getByRole('button', { name: 'Trasladar 1 animales', exact: true }).click(); await page.getByRole('button', { name: 'Confirmar traslado', exact: true }).click(); await expect(page.getByRole('heading', { name: '1 animales trasladados' })).toBeVisible(); expect((await (await request.get('/api/animals/' + animalId, { headers })).json()).paddockId).toBe(target);
});

test('an inventory notification replaces stale filters on an already open inventory page', async ({ page, request }) => {
  const categories = await (await request.get('/api/categories', { headers })).json(); const productResponse = await request.post('/api/products', { headers, data: { sku: tag, name: tag, categoryId: categories[0].id, price: 10, costPrice: 5, unit: 'Dose' } }); expect(productResponse.status()).toBe(201); const productId = (await productResponse.json()).id;
  expect((await request.post('/api/inventory', { headers, data: { farmId, productId, stock: 0, minStock: 5, maxStock: 100 } })).status()).toBe(201);
  await login(page); await page.goto('/inventory'); await page.getByLabel('Buscar por nombre o código').fill('DOES-NOT-MATCH'); await page.getByRole('button', { name: /^Notificaciones:/ }).click(); const dialog = page.getByRole('dialog', { name: 'Notificaciones', exact: true }); await dialog.getByRole('button', { name: 'Inventario', exact: true }).click(); await dialog.locator('li').filter({ hasText: tag }).getByRole('link', { name: 'Revisar inventario' }).click();
  await expect(page.getByLabel('Buscar por nombre o código')).toHaveValue(tag); await expect(page.getByLabel('Finca', { exact: true })).toHaveValue(farmId); await expect(page.getByLabel('Estado del stock')).toHaveValue('Low'); await expect(page.getByRole('heading', { name: tag, exact: true })).toBeVisible();
});
