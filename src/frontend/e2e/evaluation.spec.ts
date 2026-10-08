import { test, expect } from '@playwright/test';
import type { APIRequestContext, Page } from '@playwright/test';
import { randomUUID } from 'node:crypto';
// These scenarios intentionally retain clinical history and synthetic rates until the disposable Compose volume is removed.
test.skip(process.env.E2E_ISOLATED_DATABASE !== '1', 'Run only against the disposable phase4 evaluation database.');
let headers: Record<string, string>, farmId: string, speciesId: string, prefix: string;
async function signIn(page: Page) {
  await page.goto('/dashboard'); await page.getByLabel('Usuario o correo').fill(process.env.E2E_ADMIN_USERNAME!);
  await page.getByLabel('Contraseña', { exact: true }).fill(process.env.E2E_ADMIN_PASSWORD!);
  await page.getByRole('button', { name: 'Entrar a mi finca' }).click();
  await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toBeVisible();
}
async function animal(request: APIRequestContext, suffix: string) {
  const response = await request.post('/api/animals', { headers, data: { farmId, speciesId, internalTag: prefix + suffix, sex: 'Female', purpose: 'Milk', birthDate: '2020-01-01' } });
  expect(response.status()).toBe(201); return (await response.json()).id as string;
}
async function period(page: Page, from: string, to: string) {
  await page.getByLabel('Desde', { exact: true }).fill(from); await page.getByLabel('Hasta', { exact: true }).fill(to);
  await page.getByRole('button', { name: 'Aplicar filtros', exact: true }).click();
}
test.beforeEach(async ({ request }, info) => {
  expect(info.project.use.baseURL).toBe('http://localhost:18089');
  prefix = 'P4-EVAL-' + randomUUID().slice(0, 8).toUpperCase();
  const auth = await request.post('/api/auth/login', { data: { username: process.env.E2E_ADMIN_USERNAME, password: process.env.E2E_ADMIN_PASSWORD } });
  expect(auth.status()).toBe(200); headers = { Authorization: 'Bearer ' + (await auth.json()).accessToken };
  farmId = (await (await request.get('/api/farms', { headers })).json()).find((f: { data: { code: string } }) => f.data.code === 'DEMO').id;
  speciesId = (await (await request.get('/api/species', { headers })).json()).find((s: { data: { code: string } }) => s.data.code === 'BO').id;
});
test('an employee production write updates another session dashboard without navigation or waiting for polling', async ({ page, request }) => {
  const id = await animal(request, '-LIVE'); const day = new Date().toISOString().slice(0, 10);
  await signIn(page); await period(page, day, day);
  await expect(page.getByText('Actualización en vivo conectada.', { exact: false })).toBeVisible();
  const baseline = await (await request.get('/api/analytics/overview?from=' + day + '&to=' + day, { headers })).json();
  const total = baseline.milkByDay.reduce((sum: number, p: { value: number }) => sum + p.value, 0);
  const auth = await request.post('/api/auth/login', { data: { username: process.env.E2E_EMPLOYEE_USERNAME, password: process.env.E2E_EMPLOYEE_PASSWORD } });
  expect(auth.status()).toBe(200); const operator = { Authorization: 'Bearer ' + (await auth.json()).accessToken };
  const write = await request.post('/api/animals/' + id + '/production', { headers: operator, data: { submissionId: randomUUID(), date: day, quantity: 6.789, productType: 'Milk', method: 'Milking', unit: 'Liter' } });
  expect(write.status()).toBe(201);
  const expected = new Intl.NumberFormat('es-VE', { maximumFractionDigits: 3 }).format(total + 6.789) + ' L';
  await expect(page.getByRole('article', { name: 'Leche registrada', exact: true }).locator('strong')).toHaveText(expected, { timeout: 8000 });
  await expect(page).toHaveURL(/dashboard/); expect(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth)).toBe(false);
});
test('pregnancy and fertility deduplicate females and disclose pending diagnoses', async ({ page, request }) => {
  const ids: string[] = []; for (const suffix of ['A', 'B', 'C']) ids.push(await animal(request, suffix));
  for (let i = 0; i < ids.length; i++) {
    for (const record of [{ kind: 'Mating', date: '2026-02-01' }, { kind: 'PregnancyCheck', date: '2026-02-10', result: ['Positive', 'Negative', 'Uncertain'][i] }]) {
      const response = await request.post('/api/animals/' + ids[i] + '/reproduction', { headers, data: { submissionId: randomUUID(), ...record } }); expect(response.status()).toBe(201);
    }
  }
  const response = await request.get('/api/analytics/overview?from=2026-02-01&to=2026-02-10', { headers }); const metrics = (await response.json()).reproduction;
  expect(metrics.fertilityPercent).toBe(50); expect(metrics.pregnancyPercent).toBe(50); expect(metrics.pendingServices).toBeGreaterThanOrEqual(1);
  await signIn(page); await period(page, '2026-02-01', '2026-02-10');
  await expect(page.getByRole('article', { name: 'Preñez en hembras evaluadas', exact: true }).locator('strong')).toHaveText('50%');
  await expect(page.getByText('Fertilidad de servicios evaluados', { exact: true })).toBeVisible();
  await expect(page.getByText(/pendientes de.*hembras servidas/)).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth)).toBe(false);
});
test('dated BCV reference persists and changes valuation currency without modifying USD catalog prices', async ({ page, request }) => {
  await signIn(page);
  await page.getByRole('button', { name: 'Registrar tasa BCV', exact: true }).click();
  const drawer = page.getByRole('dialog', { name: 'Registrar referencia BCV', exact: true }); await expect(drawer).toBeVisible();
  await drawer.getByLabel('Bolívares por dólar', { exact: true }).fill('41.123456');
  await drawer.getByRole('button', { name: 'Guardar referencia BCV', exact: true }).click(); await expect(drawer).not.toBeVisible();
  const quote = await (await request.get('/api/exchange-rates/usd-ves', { headers })).json(); expect(quote.bolivarsPerDollar).toBe(41.123456); expect(quote.entryMethod).toBe('manual');
  await page.reload(); await page.getByRole('button', { name: 'Bs', exact: true }).click(); await expect(page.getByRole('button', { name: 'Bs', exact: true })).toHaveAttribute('aria-pressed', 'true');
  await expect(page.getByRole('columnheader', { name: 'Costo (Bs)', exact: true })).toBeVisible();
  const overview = await (await request.get('/api/analytics/overview?from=' + new Date().toISOString().slice(0, 10) + '&to=' + new Date().toISOString().slice(0, 10), { headers })).json();
  const converted = new Intl.NumberFormat('es-VE', { style: 'currency', currency: 'VES', minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(overview.cost * quote.bolivarsPerDollar);
  await expect(page.locator('article').filter({ has: page.getByText('Costo del inventario actual', { exact: true }) }).locator('strong')).toHaveText(converted);
  await page.getByRole('button', { name: 'USD', exact: true }).click(); await expect(page.getByRole('columnheader', { name: 'Costo (USD)', exact: true })).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth)).toBe(false);
});
