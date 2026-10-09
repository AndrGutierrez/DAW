import { test, expect } from '@playwright/test';
import type { Page } from '@playwright/test';
import { randomUUID } from 'node:crypto';
import { appendFile } from 'node:fs/promises';

let headers: Record<string, string>, farmId: string, speciesId: string, productId: string;
let prefix: string;
let animals: string[];
const username = process.env.E2E_ADMIN_USERNAME!, password = process.env.E2E_ADMIN_PASSWORD!;
async function login(page: Page) {
  await page.goto('/animals');
  await page.getByLabel('Usuario o correo').fill(username);
  await page.getByLabel('Contraseña', { exact: true }).fill(password);
  await page.getByRole('button', { name: 'Entrar a mi finca' }).click();
  await expect(page.getByRole('heading', { name: 'Animales', exact: true })).toBeVisible();
}
test.beforeEach(async ({ request }, info) => {
  animals = []; prefix = ('P4-CARE-' + info.project.name[0] + '-' + randomUUID().slice(0, 8)).toUpperCase();
  const session = await request.post('/api/auth/login', { data: { username, password } });
  expect(session.status()).toBe(200); headers = { Authorization: 'Bearer ' + (await session.json()).accessToken };
  farmId = (await (await request.get('/api/farms', { headers })).json()).find((x: { data: { code: string } }) => x.data.code === 'DEMO').id;
  speciesId = (await (await request.get('/api/species', { headers })).json()).find((x: { data: { code: string } }) => x.data.code === 'BO').id;
  const products = await (await request.get('/api/products', { headers })).json();
  productId = products.find((x: { data: { sku: string; isActive: boolean } }) => x.data.sku === 'SAN-001' && x.data.isActive).id;
});
test.afterEach(async () => {
  // Append-only clinical records stay in the isolated test database unless the runner
  // supplies this manifest for a scoped, explicit cleanup outside the public API.
  if (process.env.E2E_CARE_FIXTURE_OUTPUT)
    await appendFile(process.env.E2E_CARE_FIXTURE_OUTPUT, JSON.stringify({ prefix, animals }) + '\n');
});
test('clinical form snapshots withdrawal and production refuses milk and slaughter', async ({ page, request }) => {
  const created = await request.post('/api/animals', { headers, data: { farmId, speciesId, internalTag: prefix + '-CLINICAL', sex: 'Female', purpose: 'DualPurpose' } });
  expect(created.status()).toBe(201); const animal = (await created.json()).id; animals.push(animal);
  await login(page); await page.goto('/animals/' + animal + '?tab=clinical');
  await page.getByRole('button', { name: 'Registrar evento sanitario', exact: true }).click();
  await page.getByLabel('Producto *', { exact: true }).selectOption(productId);
  await page.getByLabel('Dosis administrada *', { exact: true }).fill('2.125');
  await page.getByLabel('Días de retiro *', { exact: true }).fill('7');
  await page.getByLabel('Observaciones sanitarias').fill('Clinical browser evidence');
  await page.getByRole('button', { name: 'Guardar evento sanitario' }).click();
  const clinical = page.getByRole('region', { name: 'Sanidad', exact: true });
  await expect(clinical.getByText('Retiro activo: leche y sacrificio bloqueados')).toBeVisible();
  await expect(clinical.getByText('Clinical browser evidence')).toBeVisible();
  const history = await (await request.get('/api/animals/' + animal + '/clinical', { headers })).json();
  expect(history.history.total).toBe(1);
  expect(history.history.items[0].data.withdrawalDays).toBe(7);
  await page.getByRole('tab', { name: 'Producción', exact: true }).click();
  await page.getByRole('button', { name: 'Registrar producción', exact: true }).click();
  await page.getByLabel('Cantidad (L) *', { exact: true }).fill('12.5');
  await page.getByRole('button', { name: 'Guardar producción', exact: true }).click();
  const production = page.getByRole('region', { name: 'Producción', exact: true });
  await expect(production.getByRole('alert')).toContainText('retiro sanitario');
  await page.getByLabel('Tipo de producción *').selectOption('meat');
  await page.getByRole('button', { name: 'Guardar producción', exact: true }).click();
  await expect(page.getByRole('dialog')).toBeVisible();
  await page.getByRole('dialog').getByRole('button', { name: 'Confirmar' }).click();
  await expect(production.getByRole('alert')).toContainText('retiro sanitario');
  expect((await (await request.get('/api/animals/' + animal, { headers })).json()).status).toBe('Active');
  expect((await (await request.get('/api/animals/' + animal + '/production', { headers })).json()).total).toBe(0);
  expect(await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth)).toBe(false);
});
test('reproductive diagnosis and calving update status and genealogy stays interactive', async ({ page, request }) => {
  const parentResponse = await request.post('/api/animals', { headers, data: { farmId, speciesId, internalTag: prefix + '-DAM', sex: 'Female', purpose: 'Milk', birthDate: '2020-01-01' } });
  expect(parentResponse.status()).toBe(201); const parent = (await parentResponse.json()).id; animals.push(parent);
  const childResponse = await request.post('/api/animals', { headers, data: { farmId, speciesId, internalTag: prefix + '-CHILD', sex: 'Female', purpose: 'Milk', birthDate: '2025-01-01', damId: parent } });
  expect(childResponse.status()).toBe(201); const child = (await childResponse.json()).id; animals.push(child);
  await login(page); await page.goto('/animals/' + parent + '?tab=reproduction');
  await page.getByRole('button', { name: 'Registrar evento reproductivo', exact: true }).click();
  await page.getByLabel('Resultado del diagnóstico *').selectOption('Positive');
  await page.getByRole('button', { name: 'Guardar evento reproductivo', exact: true }).click();
  const reproduction = page.getByRole('region', { name: 'Reproducción', exact: true });
  await expect(reproduction.getByText('Gestación confirmada', { exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Registrar evento reproductivo', exact: true }).click();
  await page.getByLabel('Evento reproductivo *', { exact: true }).selectOption('Calving');
  await page.getByLabel('Crías totales *').fill('2');
  await page.getByLabel('Crías nacidas muertas *').fill('1');
  await page.getByRole('button', { name: 'Guardar evento reproductivo', exact: true }).click();
  await expect(reproduction.getByText('Parto registrado', { exact: true })).toBeVisible();
  await page.goto('/animals/' + child + '?tab=genealogy');
  const genealogy = page.getByRole('region', { name: 'Genealogía', exact: true });
  await genealogy.getByRole('link', { name: prefix + '-DAM' }).click();
  await expect(page).toHaveURL('/animals/' + parent);
  const history = await (await request.get('/api/animals/' + parent + '/reproduction', { headers })).json();
  expect(history.history.total).toBe(2); expect(history.status.state).toBe('Calved');
});
test('lost clinical response retries one immutable submission', async ({ page, request }) => {
  const created = await request.post('/api/animals', { headers, data: { farmId, speciesId, internalTag: prefix + '-RETRY', sex: 'Female', purpose: 'Milk' } });
  expect(created.status()).toBe(201); const animal = (await created.json()).id; animals.push(animal);
  await login(page); await page.goto('/animals/' + animal + '?tab=clinical');
  await page.getByRole('button', { name: 'Registrar evento sanitario', exact: true }).click();
  await page.getByLabel('Evento sanitario *', { exact: true }).selectOption('Quarantine');
  await page.getByLabel('Motivo de cuarentena *').fill('Arrival check');
  let intercepted = false;
  await page.route('**/api/animals/' + animal + '/clinical', async route => {
    if (route.request().method() === 'POST' && !intercepted) {
      intercepted = true; const response = await route.fetch(); expect(response.status()).toBe(201); await route.abort('failed');
    } else await route.continue();
  });
  await page.getByRole('button', { name: 'Guardar evento sanitario' }).click();
  await expect(page.getByLabel('Motivo de cuarentena *')).toBeDisabled();
  await page.getByRole('button', { name: 'Reintentar y confirmar registro' }).click();
  const clinical = page.getByRole('region', { name: 'Sanidad', exact: true });
  await expect(clinical.getByText('Arrival check', { exact: true })).toBeVisible();
  expect((await (await request.get('/api/animals/' + animal + '/clinical', { headers })).json()).history.total).toBe(1);
});
test('clinical paging uses the server and employee can record care without destructive controls', async ({ page, request }) => {
  const created = await request.post('/api/animals', { headers, data: { farmId, speciesId, internalTag: prefix + '-PAGING', sex: 'Female', purpose: 'Milk' } });
  expect(created.status()).toBe(201); const animal = (await created.json()).id; animals.push(animal);
  for (let i = 0; i < 11; i++)
    expect((await request.post('/api/animals/' + animal + '/clinical', { headers, data: { submissionId: randomUUID(), kind: 'DiseaseCase', date: '2026-01-01', notes: 'Observation ' + i } })).status()).toBe(201);
  await page.goto('/login');
  await page.getByLabel('Usuario o correo').fill(process.env.E2E_EMPLOYEE_USERNAME!);
  await page.getByLabel('Contraseña', { exact: true }).fill(process.env.E2E_EMPLOYEE_PASSWORD!);
  await page.getByRole('button', { name: 'Entrar a mi finca' }).click();
  await expect(page.getByRole('heading', { name: 'Animales', exact: true })).toBeVisible();
  await page.goto('/animals/' + animal + '?tab=clinical');
  const clinical = page.getByRole('region', { name: 'Sanidad', exact: true });
  await expect(clinical.locator('.care-timeline').first().locator('li')).toHaveCount(10);
  const next = page.waitForResponse(response => response.url().includes('/clinical?page=2'));
  await clinical.getByRole('button', { name: 'Siguiente', exact: true }).click();
  expect((await next).status()).toBe(200);
  await expect(clinical.locator('.care-timeline').first().locator('li')).toHaveCount(1);
  await expect(clinical.getByRole('button', { name: 'Registrar evento sanitario' })).toBeVisible();
  await expect(clinical.getByRole('button', { name: /Eliminar/ })).toHaveCount(0);
  await clinical.getByText('Actualizar estado de salud', { exact: true }).click();
  await page.getByLabel('Nuevo estado de salud').selectOption('InTreatment');
  await page.getByLabel('Motivo del cambio de salud *').fill('Clinical follow-up');
  const before = await (await request.get('/api/animals/' + animal, { headers })).json();
  await page.getByRole('button', { name: 'Guardar estado de salud', exact: true }).click();
  await expect(page.locator('.detail-summary')).toContainText('En tratamiento');
  const after = await (await request.get('/api/animals/' + animal, { headers })).json();
  expect(Date.parse(after.updatedAt)).toBeGreaterThan(Date.parse(before.updatedAt));
  const refreshed = await (await request.get('/api/animals/' + animal + '/clinical', { headers })).json();
  expect(refreshed.statusChanges[0].reason).toBe('Clinical follow-up');

});

test('all care event types persist and offspring search stays scoped to its mother', async ({ request }) => {
  async function create(tag: string, extra: Record<string, unknown> = {}) {
    const response = await request.post('/api/animals', { headers, data: { farmId, speciesId, internalTag: prefix + '-' + tag, sex: 'Female', purpose: 'Milk', birthDate: '2020-01-01', ...extra } });
    expect(response.status()).toBe(201); const id = (await response.json()).id; animals.push(id); return id;
  }
  const dam = await create('MOTHER');
  const sire = await create('SIRE', { sex: 'Male' });
  const child = await create('OFFSPRING', { damId: dam, birthDate: '2025-01-01' });
  for (const data of [
    { kind: 'Treatment', productId, dose: 1, route: 'Oral', endDate: '2026-01-01', withdrawalDays: 7 },
    { kind: 'Vaccination', productId, dose: 1, nextDueDate: '2027-01-01' },
    { kind: 'Deworming', productId, dose: 1 },
    { kind: 'DiseaseCase', severity: 'Observed', isContagious: true },
    { kind: 'Quarantine', reason: 'Arrival', endDate: '2026-01-10' },
  ]) expect((await request.post('/api/animals/' + dam + '/clinical', { headers, data: { submissionId: randomUUID(), date: '2026-01-01', ...data } })).status()).toBe(201);
  const events = [
    { kind: 'Heat', method: 'Observation' }, { kind: 'Mating', sireId: sire }, { kind: 'Insemination', sireId: sire },
    { kind: 'PregnancyCheck', result: 'Positive', expectedCalvingDate: '2026-09-01' },
    { kind: 'Calving', offspringCount: 1, stillbornCount: 0, difficulty: 'Easy' },
    { kind: 'Weaning', offspringId: child, weightKg: 120.5 }, { kind: 'Abortion', reason: 'Recorded assessment' },
  ];
  for (let index = 0; index < events.length; index++)
    expect((await request.post('/api/animals/' + dam + '/reproduction', { headers, data: { submissionId: randomUUID(), date: '2026-01-0' + (index + 1), ...events[index] } })).status()).toBe(201);
  const history = await (await request.get('/api/animals/' + dam + '/reproduction', { headers })).json();
  expect(history.history.total).toBe(7); expect(history.status.state).toBe('Aborted');
  const offspring = await (await request.get('/api/animals/page?farmId=' + farmId + '&damId=' + dam, { headers })).json();
  expect(offspring.total).toBe(1); expect(offspring.items[0].id).toBe(child);
  expect((await (await request.get('/api/animals/' + dam + '/clinical', { headers })).json()).history.total).toBe(5);
});

test('concurrent treatment and milk cannot both commit contradictory records', async ({ request }) => {
  const created = await request.post('/api/animals', { headers, data: { farmId, speciesId, internalTag: prefix + '-RACE', sex: 'Female', purpose: 'Milk' } });
  expect(created.status()).toBe(201); const animal = (await created.json()).id; animals.push(animal);
  const results = await Promise.all([
    request.post('/api/animals/' + animal + '/clinical', { headers, data: { submissionId: randomUUID(), kind: 'Treatment', date: '2026-01-01', productId, dose: 1, endDate: '2026-01-01', withdrawalDays: 7 } }),
    request.post('/api/animals/' + animal + '/production', { headers, data: { submissionId: randomUUID(), date: '2026-01-01', productType: 'Milk', method: 'Milking', quantity: 10, unit: 'Liter' } }),
  ]);
  expect(results.map(result => result.status()).sort()).toEqual([201, 409]);
  const care = await (await request.get('/api/animals/' + animal + '/clinical', { headers })).json();
  const production = await (await request.get('/api/animals/' + animal + '/production', { headers })).json();
  expect(care.history.total + production.total).toBe(1);
});
