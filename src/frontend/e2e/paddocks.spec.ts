import { test, expect } from '@playwright/test';
import type { Page, APIRequestContext } from '@playwright/test';
import { randomUUID } from 'node:crypto';

let headers: Record<string, string>, farmId: string, speciesId: string, prefix: string;
let animals: string[], paddocks: string[], lots: string[];
const username = process.env.E2E_ADMIN_USERNAME!, password = process.env.E2E_ADMIN_PASSWORD!;
async function login(page: Page, employee = false) {
  await page.goto('/paddocks');
  await page.getByLabel('Usuario o correo').fill(employee ? process.env.E2E_EMPLOYEE_USERNAME! : username);
  await page.getByLabel('Contraseña', { exact: true }).fill(employee ? process.env.E2E_EMPLOYEE_PASSWORD! : password);
  await page.getByRole('button', { name: 'Entrar a mi finca' }).click();
  await expect(page.getByRole('heading', { name: 'Potreros', exact: true })).toBeVisible();
}
async function createPaddock(request: APIRequestContext, name: string, capacity: number) {
  const response = await request.post('/api/paddocks', { headers, data: { farmId, name: prefix + '-' + name, areaHectares: 2, capacity } });
  expect(response.status()).toBe(201); const id = (await response.json()).id; paddocks.push(id); return id;
}
async function createAnimal(request: APIRequestContext, name: string, paddockId: string | null = null) {
  const response = await request.post('/api/animals', { headers, data: { farmId, speciesId, internalTag: prefix + '-' + name, sex: 'Female', purpose: 'Milk', paddockId } });
  expect(response.status()).toBe(201); const id = (await response.json()).id; animals.push(id); return id;
}
test.beforeEach(async ({ request }, info) => {
  test.skip(process.env.E2E_ISOLATED_DATABASE !== '1', 'Mutation fixtures require the named disposable database.');
  animals = []; paddocks = []; lots = []; prefix = ('P4-PAD-' + info.project.name[0] + '-' + randomUUID().slice(0, 8)).toUpperCase();
  const response = await request.post('/api/auth/login', { data: { username, password } });
  expect(response.status()).toBe(200); headers = { Authorization: 'Bearer ' + (await response.json()).accessToken };
  farmId = (await (await request.get('/api/farms', { headers })).json()).find((x: { data: { code: string } }) => x.data.code === 'DEMO').id;
  speciesId = (await (await request.get('/api/species', { headers })).json()).find((x: { data: { code: string } }) => x.data.code === 'BO').id;
});
test.afterEach(async ({ request }) => {
  if (process.env.E2E_ISOLATED_DATABASE !== '1') return;
  for (const id of animals) expect((await request.delete('/api/animals/' + id, { headers })).status()).toBe(204);
  for (const id of lots) expect((await request.delete('/api/lots/' + id, { headers })).status()).toBe(204);
  for (const id of paddocks) expect((await request.delete('/api/paddocks/' + id, { headers })).status()).toBe(204);
});
test('map reads actual occupancy and moves an animal with recorded arrival', async ({ page, request }) => {
  const source = await createPaddock(request, 'SOURCE', 2), target = await createPaddock(request, 'TARGET', 2);
  const animal = await createAnimal(request, 'MOVING', source);
  await login(page);
  await page.getByLabel('Buscar potrero').fill(prefix);
  await page.getByRole('button', { name: 'Buscar', exact: true }).click();
  const map = page.getByLabel('Mapa de ocupación de potreros');
  const sourceCard = map.getByRole('button').filter({ hasText: prefix + '-SOURCE' }).first();
  await expect(sourceCard).toContainText('0,5 animales/ha');
  await expect(sourceCard).toContainText('Mayor permanencia registrada: 0 días');
  await sourceCard.click();
  const table = page.getByRole('table', { name: 'Animales presentes en el potrero' });
  await expect(table).toContainText(prefix + '-MOVING');
  await table.getByRole('button', { name: 'Trasladar ' + prefix + '-MOVING' }).click();
  await page.getByLabel('Potrero de destino', { exact: true }).selectOption(target);
  await page.getByLabel('Motivo del traslado *').fill('Browser paddock rotation');
  await page.getByRole('button', { name: 'Confirmar traslado', exact: true }).click();
  await expect(page.getByRole('status').filter({ hasText: 'Registro guardado.' })).toBeVisible();
  const detail = await (await request.get('/api/animals/' + animal, { headers })).json();
  expect(detail.paddockId).toBe(target);
  await page.goto('/animals/' + animal + '?tab=location');
  await expect(page.getByRole('region', { name: 'Potrero y lote' })).toContainText('Browser paddock rotation');
  expect(await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth)).toBe(false);
});
test('full paddock blocks transfer without losing form values', async ({ page, request }) => {
  const target = await createPaddock(request, 'FULL', 1);
  await createAnimal(request, 'RESIDENT', target);
  const animal = await createAnimal(request, 'WAITING');
  await login(page); await page.goto('/animals/' + animal + '?tab=location');
  await page.getByRole('button', { name: 'Registrar traslado', exact: true }).click();
  await page.getByLabel('Potrero de destino', { exact: true }).selectOption(target);
  await page.getByLabel('Motivo del traslado *').fill('Keep my reason');
  await page.getByRole('button', { name: 'Confirmar traslado', exact: true }).click();
  const region = page.getByRole('region', { name: 'Potrero y lote' });
  await expect(region.getByRole('alert')).toContainText('capacidad máxima');
  await expect(page.getByLabel('Motivo del traslado *')).toHaveValue('Keep my reason');
  expect((await (await request.get('/api/animals/' + animal, { headers })).json()).paddockId).toBeNull();
  expect((await (await request.get('/api/animals/' + animal + '/movements', { headers })).json()).total).toBe(0);
});
test('concurrent arrivals cannot occupy the same last slot', async ({ request }) => {
  const target = await createPaddock(request, 'LAST', 1);
  const first = await createAnimal(request, 'FIRST'), second = await createAnimal(request, 'SECOND');
  const replies = await Promise.all([first, second].map(id => request.post('/api/animals/' + id + '/movements', { headers,
    data: { submissionId: randomUUID(), toPaddockId: target, toLotId: null, expectedFromPaddockId: null, expectedFromLotId: null, reason: 'Concurrent arrival' } })));
  expect(replies.map(r => r.status()).sort()).toEqual([201, 409]);
  const residents = await (await request.get('/api/paddocks/' + target + '/residents', { headers })).json();
  expect(residents.total).toBe(1);
});
test('admin and employee maintain assigned paddocks while deletion remains restricted', async ({ page, request }) => {
  const id = await createPaddock(request, 'EDIT', 2);
  await createAnimal(request, 'PRESENT', id);
  await login(page);
  await page.getByLabel('Buscar potrero').fill(prefix);
  await page.getByRole('button', { name: 'Buscar', exact: true }).click();
  await page.getByRole('button', { name: 'Editar ' + prefix + '-EDIT', exact: true }).click();
  await page.getByLabel('Capacidad máxima (animales)').fill('4');
  await page.getByRole('button', { name: 'Guardar potrero', exact: true }).click();
  await expect(page.getByRole('status').filter({ hasText: 'Potrero guardado.' })).toBeVisible();
  expect((await (await request.get('/api/paddocks/' + id, { headers })).json()).data.capacity).toBe(4);
  await page.getByRole('button', { name: 'Salir', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Inicia sesión' })).toBeVisible();
  await login(page, true);
  await expect(page.getByRole('button', { name: 'Registrar potrero', exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Editar ' + prefix + '-EDIT', exact: true }).click();
  await page.getByLabel('Capacidad máxima (animales)').fill('5');
  await page.getByRole('button', { name: 'Guardar potrero', exact: true }).click();
  await expect(page.getByRole('status').filter({ hasText: 'Potrero guardado.' })).toBeVisible();
  expect((await (await request.get('/api/paddocks/' + id, { headers })).json()).data.capacity).toBe(5);
  const employeeSession = await request.post('/api/auth/login', { data: { username: process.env.E2E_EMPLOYEE_USERNAME!, password: process.env.E2E_EMPLOYEE_PASSWORD! } });
  expect(employeeSession.status()).toBe(200);
  const employeeHeaders = { Authorization: 'Bearer ' + (await employeeSession.json()).accessToken };
  expect((await request.delete('/api/paddocks/' + id, { headers: employeeHeaders })).status()).toBe(403);
  expect(await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth)).toBe(false);
});
test('lost movement response replays one submission and preserves its source', async ({ page, request }) => {
  const target = await createPaddock(request, 'REPLAY', 2), animal = await createAnimal(request, 'RETRY');
  await login(page); await page.goto('/animals/' + animal + '?tab=location');
  await page.getByRole('button', { name: 'Registrar traslado', exact: true }).click();
  await page.getByLabel('Potrero de destino', { exact: true }).selectOption(target);
  await page.getByLabel('Motivo del traslado *').fill('One movement');
  let dropped = false;
  await page.route('**/api/animals/' + animal + '/movements', async route => {
    if (route.request().method() === 'POST' && !dropped) {
      dropped = true;
      const response = await route.fetch();
      expect(response.status()).toBe(201);
      await route.abort('failed');
    } else await route.continue();
  });
  await page.getByRole('button', { name: 'Confirmar traslado', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Reintentar y confirmar registro' })).toBeVisible();
  await expect(page.getByLabel('Potrero de destino', { exact: true })).toBeDisabled();
  await page.getByRole('button', { name: 'Reintentar y confirmar registro' }).click();
  await expect(page.getByRole('region', { name: 'Potrero y lote' })).toContainText('One movement');
  const history = await (await request.get('/api/animals/' + animal + '/movements', { headers })).json();
  expect(history.total).toBe(1); expect(history.items[0].fromPaddockId).toBeNull();
});

test('stale movement source can be refreshed from the form', async ({ page, request }) => {
  const source = await createPaddock(request, 'ORIGINAL', 2), changed = await createPaddock(request, 'CHANGED', 2), target = await createPaddock(request, 'FINAL', 2);
  const animal = await createAnimal(request, 'STALE', source);
  await login(page); await page.goto('/animals/' + animal + '?tab=location');
  await page.getByRole('button', { name: 'Registrar traslado', exact: true }).click();
  await page.getByLabel('Potrero de destino', { exact: true }).selectOption(target);
  await page.getByLabel('Motivo del traslado *').fill('Stale browser draft');
  const external = await request.post('/api/animals/' + animal + '/movements', { headers, data: { submissionId: randomUUID(), toPaddockId: changed, toLotId: null, expectedFromPaddockId: source, expectedFromLotId: null, reason: 'Another operator' } });
  expect(external.status()).toBe(201);
  await page.getByRole('button', { name: 'Confirmar traslado', exact: true }).click();
  await expect(page.getByRole('region', { name: 'Potrero y lote' }).getByRole('alert')).toContainText('Otro usuario cambió la ubicación');
  await page.getByRole('button', { name: 'Actualizar ubicación', exact: true }).click();
  await expect(page.getByRole('region', { name: 'Potrero y lote' })).toContainText(prefix + '-CHANGED');
  await page.getByRole('button', { name: 'Registrar traslado', exact: true }).click();
  await page.getByLabel('Potrero de destino', { exact: true }).selectOption(target);
  await page.getByLabel('Motivo del traslado *').fill('Reviewed location');
  await page.getByRole('button', { name: 'Confirmar traslado', exact: true }).click();
  await expect(page.getByRole('region', { name: 'Potrero y lote' })).toContainText('Reviewed location');
  expect((await (await request.get('/api/animals/' + animal, { headers })).json()).paddockId).toBe(target);
});

test('lot selection filters residents on the server within the chosen paddock', async ({ page, request }) => {
  const source = await createPaddock(request, 'LOTS', 4);
  const lotResponse = await request.post('/api/lots', { headers, data: { farmId, speciesId, name: prefix + '-GROUP', purpose: 'Milk', paddockId: source } });
  expect(lotResponse.status()).toBe(201); const lot = (await lotResponse.json()).id; lots.push(lot);
  const grouped = await createAnimal(request, 'GROUPED', source);
  const ungrouped = await createAnimal(request, 'UNGROUPED', source);
  const update = await request.put('/api/animals/' + grouped, { headers, data: { farmId, speciesId, internalTag: prefix + '-GROUPED', sex: 'Female', purpose: 'Milk', paddockId: source, lotId: lot } });
  expect(update.status()).toBe(200);
  await login(page); await page.getByLabel('Buscar potrero').fill(prefix);
  await page.getByRole('button', { name: 'Buscar', exact: true }).click();
  await page.getByLabel('Mapa de ocupación de potreros').getByRole('button').filter({ hasText: prefix + '-LOTS' }).first().click();
  const table = page.getByRole('table', { name: 'Animales presentes en el potrero' });
  await expect(table).toContainText(prefix + '-GROUPED'); await expect(table).toContainText(prefix + '-UNGROUPED');
  const response = page.waitForResponse(r => r.url().includes('/residents?') && r.url().includes('lotId=' + lot));
  await page.getByRole('button', { name: prefix + '-GROUP · 1', exact: true }).click();
  expect((await response).status()).toBe(200);
  await expect(table).toContainText(prefix + '-GROUPED'); await expect(table).not.toContainText(prefix + '-UNGROUPED');
  await page.getByRole('button', { name: 'Sin lote · 1', exact: true }).click();
  await expect(table).toContainText(prefix + '-UNGROUPED'); await expect(table).not.toContainText(prefix + '-GROUPED');
  expect((await (await request.get('/api/paddocks/' + source + '/residents?lotId=' + lot, { headers })).json()).total).toBe(1);
  expect((await (await request.get('/api/paddocks/' + source + '/residents?ungrouped=true', { headers })).json()).items[0].id).toBe(ungrouped);
});
