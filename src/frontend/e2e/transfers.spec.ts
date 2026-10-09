import { test, expect } from '@playwright/test';
import type { APIRequestContext } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { randomUUID } from 'node:crypto';
import { mkdir } from 'node:fs/promises';
import { join } from 'node:path';

test.skip(process.env.E2E_ISOLATED_DATABASE !== '1', 'Movement fixtures require the disposable database.');
async function fixture(request: APIRequestContext, count: number, capacity: number) {
  const login = await request.post('/api/auth/login', { data: { username: process.env.E2E_ADMIN_USERNAME, password: process.env.E2E_ADMIN_PASSWORD } });
  expect(login.status()).toBe(200);
  const headers = { Authorization: 'Bearer ' + (await login.json()).accessToken };
  const farmId = (await (await request.get('/api/farms', { headers })).json()).find((item: { data: { code: string } }) => item.data.code === 'DEMO').id;
  const speciesId = (await (await request.get('/api/species', { headers })).json()).find((item: { data: { code: string } }) => item.data.code === 'BO').id;
  const prefix = 'MOVE-' + randomUUID().slice(0, 8);
  async function create(path: string, data: unknown) { const response = await request.post(path, { headers, data }); expect(response.status()).toBe(201); return (await response.json()).id as string; }
  const lot = await create('/api/lots', { farmId, speciesId, name: prefix, purpose: 'Meat' });
  const source = await create('/api/paddocks', { farmId, name: prefix + ' Origen', capacity: 150 });
  const target = await create('/api/paddocks', { farmId, name: prefix + ' Destino', capacity });
  const animals: string[] = [];
  for (let i = 0; i < count; i++) animals.push(await create('/api/animals', { farmId, speciesId, internalTag: prefix + '-' + String(i).padStart(3, '0'), sex: 'Female', purpose: 'Meat', lotId: lot, paddockId: source }));
  const payload = { farmId, toPaddockId: target, reason: 'Rotación de pastoreo', changeLot: false, toLotId: null,
    animals: animals.map(animalId => ({ animalId, submissionId: randomUUID(), expectedFromPaddockId: source, expectedFromLotId: lot })) };
  return { headers, farmId, prefix, lot, source, target, animals, payload };
}
test('group capacity failure rolls back locations, history and audit; replay does not duplicate moves', async ({ request }) => {
  const f = await fixture(request, 3, 2);
  const before = await Promise.all(f.animals.map(async id => (await (await request.get('/api/animals/' + id + '/movements', { headers: f.headers })).json()).total));
  const refused = await request.post('/api/animal-movements/batch', { headers: f.headers, data: f.payload }); expect(refused.status()).toBe(409);
  for (let i = 0; i < f.animals.length; i++) {
    const id = f.animals[i]; const detail = await (await request.get('/api/animals/' + id, { headers: f.headers })).json();
    expect(detail.paddockId).toBe(f.source); expect(detail.lotId).toBe(f.lot);
    const history = await (await request.get('/api/animals/' + id + '/movements', { headers: f.headers })).json(); expect(history.total).toBe(before[i]);
    const audit = await (await request.get('/api/admin/auditlogs?search=' + f.payload.animals[i].submissionId, { headers: f.headers })).json(); expect(audit.total).toBe(0);
  }
  const subset = { ...f.payload, animals: f.payload.animals.slice(0, 2) };
  expect((await request.post('/api/animal-movements/batch', { headers: f.headers, data: subset })).status()).toBe(201);
  const replay = await request.post('/api/animal-movements/batch', { headers: f.headers, data: subset }); expect(replay.status()).toBe(200); expect((await replay.json()).replayed).toBe(true);
  for (let i = 0; i < 2; i++) {
    const detail = await (await request.get('/api/animals/' + f.animals[i], { headers: f.headers })).json(); expect(detail.paddockId).toBe(f.target); expect(detail.lotId).toBe(f.lot);
    const savedAudit = await (await request.get('/api/admin/auditlogs?search=' + subset.animals[i].submissionId, { headers: f.headers })).json(); expect(savedAudit.total).toBe(1);
    expect((await (await request.get('/api/animals/' + f.animals[i] + '/movements', { headers: f.headers })).json()).total).toBe(before[i] + 1);
  }
  const stale = { ...f.payload, toPaddockId: f.source, animals: [
    { ...f.payload.animals[0], submissionId: randomUUID(), expectedFromPaddockId: f.target },
    { ...f.payload.animals[1], submissionId: randomUUID(), expectedFromPaddockId: f.source }] };
  expect((await request.post('/api/animal-movements/batch', { headers: f.headers, data: stale })).status()).toBe(409);
  expect((await (await request.get('/api/animals/' + f.animals[0], { headers: f.headers })).json()).paddockId).toBe(f.target);
});
test('weighing selects a whole search and the transfer module moves a complete lot across pages', async ({ page, request }, info) => {
  const f = await fixture(request, 14, 40);
  await page.goto('/login'); await page.getByLabel('Usuario o correo').fill(process.env.E2E_ADMIN_USERNAME!); await page.getByLabel('Contraseña', { exact: true }).fill(process.env.E2E_ADMIN_PASSWORD!);
  await page.getByRole('button', { name: 'Entrar a mi finca' }).click(); await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toBeVisible();
  await page.goto('/weighing'); await page.getByLabel('Lote', { exact: true }).selectOption(f.lot);
  await expect(page.getByRole('checkbox')).toHaveCount(12);
  await page.getByRole('button', { name: 'Seleccionar todos los resultados (14)' }).click(); await expect(page.getByText('14 animales seleccionados')).toBeVisible();
  await page.getByRole('button', { name: 'Limpiar selección' }).click(); await expect(page.getByRole('button', { name: 'Comenzar pesaje' })).toBeDisabled();
  await page.goto('/transfers'); await page.getByLabel('Lote de origen').selectOption(f.lot);
  await expect(page.getByRole('checkbox')).toHaveCount(12); await page.getByRole('button', { name: 'Seleccionar todos los resultados (14)' }).click();
  await expect(page.getByRole('heading', { name: 'Trasladar 14 animales', exact: true })).toBeVisible(); await page.getByLabel('Potrero de destino *').selectOption(f.target);
  await page.getByLabel('Motivo del traslado *').fill('Rotación de pastoreo'); await expect(page.getByText('0 presentes + 14 entradas = 14 animales / 40 de capacidad')).toBeVisible();
  await page.evaluate(async () => { await document.fonts.ready; });
  const axe = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa']).analyze(); expect(axe.violations).toEqual([]);
  await page.getByRole('button', { name: 'Activar tema oscuro' }).click();
  const darkAxe = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa']).analyze(); expect(darkAxe.violations).toEqual([]);
  await page.getByRole('button', { name: 'Activar tema claro' }).click();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth + 1)).toBe(true);
  if (process.env.QUALITY_EVIDENCE_DIR) { await mkdir(process.env.QUALITY_EVIDENCE_DIR, { recursive: true }); await page.screenshot({ path: join(process.env.QUALITY_EVIDENCE_DIR, 'transfers-' + info.project.name + '.png'), fullPage: true }); }
  await page.getByRole('button', { name: 'Trasladar 14 animales', exact: true }).click(); const saved = page.waitForResponse(response => response.url().endsWith('/api/animal-movements/batch') && response.request().method() === 'POST');
  await page.getByRole('dialog', { name: 'Confirmar acción' }).getByRole('button', { name: 'Confirmar traslado' }).click(); expect((await saved).status()).toBe(201);
  await expect(page.getByRole('heading', { name: '14 animales trasladados', exact: true })).toBeVisible();
  for (const id of f.animals) { const detail = await (await request.get('/api/animals/' + id, { headers: f.headers })).json(); expect(detail.paddockId).toBe(f.target); expect(detail.lotId).toBe(f.lot); }
});
