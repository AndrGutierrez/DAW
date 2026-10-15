import { test, expect } from '@playwright/test';
import type { APIRequestContext, Page } from '@playwright/test';
import { randomUUID } from 'node:crypto';
let headers: Record<string, string>, farmId: string, speciesId: string, prefix: string;
let animals: string[], weights: string[], paddocks: string[];
async function signIn(page: Page, path: string) {
  await page.goto(path); await page.getByLabel('Usuario o correo').fill(process.env.E2E_ADMIN_USERNAME!);
  await page.getByLabel('Contraseña', { exact: true }).fill(process.env.E2E_ADMIN_PASSWORD!);
  await page.getByRole('button', { name: 'Entrar a mi finca' }).click();
}
async function createAnimal(request: APIRequestContext, paddockId: string | null = null) {
  const response = await request.post('/api/animals', { headers, data: { farmId, speciesId, internalTag: prefix, sex: 'Male', purpose: 'Meat', paddockId } });
  expect(response.status()).toBe(201); const id = (await response.json()).id; animals.push(id); return id;
}
test.beforeEach(async ({ request }) => {
  test.skip(process.env.E2E_ISOLATED_DATABASE !== '1', 'Mutation fixtures require the named disposable database.');
  animals = []; weights = []; paddocks = []; prefix = ('P4-MON-' + randomUUID().slice(0, 8)).toUpperCase();
  const response = await request.post('/api/auth/login', { data: { username: process.env.E2E_ADMIN_USERNAME, password: process.env.E2E_ADMIN_PASSWORD } });
  expect(response.status()).toBe(200); headers = { Authorization: 'Bearer ' + (await response.json()).accessToken };
  farmId = (await (await request.get('/api/farms', { headers })).json()).find((f: { data: { code: string } }) => f.data.code === 'DEMO').id;
  speciesId = (await (await request.get('/api/species', { headers })).json()).find((s: { data: { code: string } }) => s.data.code === 'BO').id;
});
test.afterEach(async ({ request }) => {
  if (process.env.E2E_ISOLATED_DATABASE !== '1') return;
  for (const id of weights) expect((await request.delete('/api/weights/' + id, { headers })).status()).toBe(204);
  for (const id of animals) expect((await request.delete('/api/animals/' + id, { headers })).status()).toBe(204);
  for (const id of paddocks) expect((await request.delete('/api/paddocks/' + id, { headers })).status()).toBe(204);
});
test('farm and animal GDP goals persist, recalculate alerts and preserve automatic weight-loss warnings', async ({ page, request }) => {
  const oldGoal = (await (await request.get('/api/farms/' + farmId + '/growth-policy', { headers })).json()).dailyGainKg;
  try {
    const id = await createAnimal(request);
    for (const [date, weightKg] of [['2026-01-01', 100], ['2026-01-11', 105]] as const) {
      const response = await request.post('/api/weights', { headers, data: { farmId, animalId: id, date, weightKg } });
      expect(response.status()).toBe(201); weights.push((await response.json()).id);
    }
    await signIn(page, '/monitoring?farm=' + farmId);
    await page.getByLabel('GDP mínima de la finca (kg/día)', { exact: true }).fill('1');
    await page.getByRole('button', { name: 'Guardar objetivo de finca', exact: true }).click();
    await expect(page.getByRole('table', { name: 'Alertas de crecimiento' })).toContainText(prefix);
    await page.goto('/animals/' + id + '?tab=growth');
    await expect(page.getByText(/Objetivo guardado:.*heredado de la finca/)).toBeVisible();
    await page.getByLabel('Objetivo de GDP (kg/día)', { exact: true }).fill('0.4');
    await page.getByRole('button', { name: 'Guardar objetivo individual', exact: true }).click();
    await expect(page.getByRole('status').filter({ hasText: 'Objetivo del animal guardado.' })).toBeVisible();
    await page.reload(); await expect(page.getByText(/Objetivo guardado:.*individual/)).toBeVisible();
    const alerts = await (await request.get('/api/alerts/growth?farmId=' + farmId + '&pageSize=100', { headers })).json();
    expect(alerts.items.some((a: { animalId: string }) => a.animalId === id)).toBe(false);
    const changed = await request.put('/api/weights/' + weights[1], { headers, data: { farmId, animalId: id, date: '2026-01-11', weightKg: 95 } });
    expect(changed.status()).toBe(200);
    await page.reload(); await expect(page.getByText('Descenso de peso detectado', { exact: true })).toBeVisible();
    const losses = await (await request.get('/api/alerts/growth?farmId=' + farmId + '&pageSize=100', { headers })).json();
    expect(losses.items.find((a: { animalId: string }) => a.animalId === id).reason).toBe('weight-loss');
    expect(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth)).toBe(false);
  } finally { expect((await request.put('/api/farms/' + farmId + '/growth-policy', { headers, data: { dailyGainKg: oldGoal } })).status()).toBe(200); }
});
test('animated paddock plan exposes capacity and opens the resident side panel', async ({ page, request }) => {
  const invalid = await request.post('/api/paddocks', { headers, data: { farmId, name: prefix, mapX: 95, mapY: 0, mapWidth: 10, mapHeight: 40 } });
  expect(invalid.status()).toBe(400);
  const response = await request.post('/api/paddocks', { headers, data: { farmId, name: prefix, capacity: 1, maxStayDays: 10, mapX: 5, mapY: 5, mapWidth: 40, mapHeight: 50 } });
  expect(response.status()).toBe(201); const paddock = (await response.json()).id; paddocks.push(paddock);
  await createAnimal(request, paddock);
  await signIn(page, '/paddocks?farm=' + farmId + '&search=not-a-card-match');
  const plan = page.getByRole('group', { name: 'Plano interactivo de potreros' });
  const item = plan.getByRole('button', { name: new RegExp(prefix) });
  await expect(page.locator('.farm-viewer')).toHaveAttribute('data-scene-state', 'ready', { timeout: 15_000 });
  await expect(page.locator('.farm-canvas-host canvas')).toBeVisible();
  await expect(item).toHaveAccessibleName(new RegExp('^' + prefix + ': 1 animales; Capacidad completa;'));
  await expect(item).toHaveClass(/\bcritical\b/);
  await expect(item).toContainText('Dentro del período previsto');
  await expect(item).toHaveAttribute('aria-pressed', 'false');
  await item.focus(); await page.keyboard.press('Enter');
  await expect(item).toHaveAttribute('aria-pressed', 'true');
  const residents = page.getByRole('dialog', { name: prefix });
  await expect(residents).toBeVisible();
  await expect(residents.getByRole('table', { name: 'Animales presentes en el potrero' })).toContainText(prefix);
  expect(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth)).toBe(false);
});


test('static paddock plan projects persisted geometry after graphics failure and reload', async ({ page, request }) => {
  const response = await request.post('/api/paddocks', { headers, data: { farmId, name: prefix, capacity: 1, maxStayDays: 10, mapX: 5, mapY: 5, mapWidth: 40, mapHeight: 50 } });
  expect(response.status()).toBe(201); const paddock = (await response.json()).id; paddocks.push(paddock);
  const persisted = await request.get('/api/paddocks/' + paddock, { headers });
  expect(persisted.status()).toBe(200);
  expect((await persisted.json()).data).toMatchObject({ mapX: 5, mapY: 5, mapWidth: 40, mapHeight: 50, maxStayDays: 10 });

  await page.route(/(renderer-[\w-]+\.js|paddock-scene\/renderer\.ts)/, route => route.abort('failed'));
  await signIn(page, '/paddocks?farm=' + farmId + '&search=not-a-card-match');
  const viewer = page.locator('.farm-viewer');
  const shape = page.locator('.farm-static-plan g').filter({ hasText: prefix }).locator('polygon');
  // Independent projection of the stored (5, 5, 40, 50) rectangle, without the production model as an oracle.
  const projectedCorners = '0,35 280,175 -70,350 -350,210';
  await expect(viewer).toHaveAttribute('data-scene-state', 'fallback', { timeout: 15_000 });
  await expect(shape).toHaveAttribute('points', projectedCorners);
  await page.reload();
  await expect(viewer).toHaveAttribute('data-scene-state', 'fallback', { timeout: 15_000 });
  await expect(shape).toHaveAttribute('points', projectedCorners);
  expect(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth)).toBe(false);
});
