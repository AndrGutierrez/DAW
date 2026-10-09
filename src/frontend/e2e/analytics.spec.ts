import { test, expect } from '@playwright/test';
import { randomUUID } from 'node:crypto';
import { mkdir } from 'node:fs/promises';
import { join } from 'node:path';

let headers: Record<string, string>;
let animals: string[], lots: string[], productions: string[];
const username = process.env.E2E_ADMIN_USERNAME!, password = process.env.E2E_ADMIN_PASSWORD!;
test.beforeEach(async ({ request }) => {
  test.skip(process.env.E2E_ISOLATED_DATABASE !== '1', 'Mutation fixtures require the named disposable database.');
  animals = []; lots = []; productions = [];
  const auth = await request.post('/api/auth/login', { data: { username, password } });
  expect(auth.status()).toBe(200);
  headers = { Authorization: 'Bearer ' + (await auth.json()).accessToken };
});
test.afterEach(async ({ request }) => {
  if (process.env.E2E_ISOLATED_DATABASE !== '1') return;
  for (const id of productions) expect((await request.delete('/api/production/' + id, { headers })).status()).toBe(204);
  for (const id of animals) expect((await request.delete('/api/animals/' + id, { headers })).status()).toBe(204);
  for (const id of lots) expect((await request.delete('/api/lots/' + id, { headers })).status()).toBe(204);
});
test('daily milk chart changes lot and preserves recorded totals and gaps', async ({ page, request }, info) => {
  const prefix = 'P4-MILK-' + randomUUID().slice(0, 8).toUpperCase();
  const farmId = (await (await request.get('/api/farms', { headers })).json()).find((f: { data: { code: string } }) => f.data.code === 'DEMO').id;
  const speciesId = (await (await request.get('/api/species', { headers })).json()).find((s: { data: { code: string } }) => s.data.code === 'BO').id;
  for (const suffix of ['A', 'B']) {
    const lot = await request.post('/api/lots', { headers, data: { farmId, speciesId, name: prefix + '-' + suffix, purpose: 'Milk' } });
    expect(lot.status()).toBe(201); lots.push((await lot.json()).id);
    const animal = await request.post('/api/animals', { headers, data: { farmId, speciesId, lotId: lots.at(-1), internalTag: prefix + '-' + suffix, sex: 'Female', purpose: 'Milk', birthDate: '2020-01-01' } });
    expect(animal.status()).toBe(201); animals.push((await animal.json()).id);
  }
  for (const [animalId, date, quantity] of [[animals[0], '2026-01-10', 1.234], [animals[0], '2026-01-12', 2.345], [animals[1], '2026-01-10', 7]] as const) {
    const production = await request.post('/api/animals/' + animalId + '/production', { headers, data: { submissionId: randomUUID(), date, quantity, productType: 'Milk', method: 'Milking', unit: 'Liter' } });
    expect(production.status()).toBe(201); productions.push((await production.json()).id);
  }
  await page.goto('/login'); await page.getByLabel('Usuario o correo').fill(username); await page.getByLabel('Contraseña', { exact: true }).fill(password); await page.getByRole('button', { name: 'Entrar a mi finca' }).click(); await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toBeVisible();
  await expect(page.getByRole('article', { name: 'Leche registrada', exact: true })).toBeVisible();
  await page.getByLabel('Desde', { exact: true }).fill('2026-01-10'); await page.getByLabel('Hasta', { exact: true }).fill('2026-01-12'); await page.getByRole('button', { name: 'Aplicar filtros', exact: true }).click();
  await expect(page.getByRole('article', { name: 'Leche registrada', exact: true }).locator('strong')).toHaveText('10,579 L');
  await page.getByLabel('Gráfico', { exact: true }).selectOption('lot-daily');
  await page.getByLabel('Lote de la curva diaria', { exact: true }).selectOption(farmId + ':' + lots[0]);
  const table = page.getByRole('table', { name: new RegExp('Leche diaria.*' + prefix + '-A') });
  await expect(table.getByRole('row')).toHaveCount(3); await expect(table).toContainText('1,234'); await expect(table).toContainText('2,345'); await expect(table).not.toContainText('11/1/2026');
  await expect(page.getByText('Las fechas sin registros aparecen como huecos', { exact: false })).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth)).toBe(false);
  if (process.env.E2E_EVIDENCE_DIR) { await mkdir(process.env.E2E_EVIDENCE_DIR, { recursive: true }); await page.locator('.analytics-chart').screenshot({ path: join(process.env.E2E_EVIDENCE_DIR, 'daily-lot-milk-' + info.project.name + '.png'), animations: 'disabled' }); }
  await page.getByLabel('Lote de la curva diaria', { exact: true }).selectOption(farmId + ':' + lots[1]);
  const other = page.getByRole('table', { name: new RegExp('Leche diaria.*' + prefix + '-B') });
  await expect(other.getByRole('row')).toHaveCount(2); await expect(other).toContainText('7'); await expect(other).not.toContainText('2,345');
  await page.getByRole('link', { name: 'Consultar producción', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Producción', exact: true })).toHaveAttribute('aria-pressed', 'true');
  await expect(page.getByLabel('Desde', { exact: true })).toHaveValue('2026-01-10'); await expect(page.getByLabel('Hasta', { exact: true })).toHaveValue('2026-01-12');
  await expect(page.getByRole('table')).toContainText(prefix + '-A'); await expect(page.getByRole('table')).toContainText(prefix + '-B');
});
