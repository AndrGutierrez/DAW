import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { randomUUID } from 'node:crypto';
import { mkdir } from 'node:fs/promises';
import { join } from 'node:path';
async function authorization(request: import('@playwright/test').APIRequestContext, employee = false) {
 const session = await request.post('/api/auth/login', { data: { username: process.env[employee ? 'E2E_EMPLOYEE_USERNAME' : 'E2E_ADMIN_USERNAME'], password: process.env[employee ? 'E2E_EMPLOYEE_PASSWORD' : 'E2E_ADMIN_PASSWORD'] } }); expect(session.status()).toBe(200); return { Authorization: 'Bearer ' + (await session.json()).accessToken };
}
async function fixture(request: import('@playwright/test').APIRequestContext) {
 const headers = await authorization(request); const farm = (await (await request.get('/api/farms', { headers })).json()).find((x: { data: { code: string } }) => x.data.code === 'DEMO');
 const species = (await (await request.get('/api/species', { headers })).json()).find((x: { data: { code: string } }) => x.data.code === 'BO');
 const tag = ('ARCHIVE-' + randomUUID().slice(0,8)).toUpperCase(); const payload = { farmId: farm.id, speciesId: species.id, internalTag: tag, sex: 'Female', purpose: 'Milk', birthDate: '2020-01-01' };
 const animal = await request.post('/api/animals', { headers, data: payload }); expect(animal.status()).toBe(201); return { headers, payload, tag, id: (await animal.json()).id };
}
test.beforeEach(() => { test.skip(process.env.E2E_ISOLATED_DATABASE !== '1', 'Archive mutation tests require a disposable database.'); });
test('archiving and restoring a record preserves its private photograph and audit history', async ({ page, request }, info) => {
 const { headers, id, tag } = await fixture(request);
 const bytes = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+j9S8AAAAASUVORK5CYII=', 'base64');
 const uploaded = await request.post('/api/animals/' + id + '/photo', { headers, multipart: { file: { name: 'retained.png', mimeType: 'image/png', buffer: bytes } } }); expect(uploaded.status()).toBe(200); const photo = await uploaded.json();
 await page.goto('/animals/' + id); await page.getByLabel('Usuario o correo').fill(process.env.E2E_ADMIN_USERNAME!); await page.getByLabel('Contraseña', { exact: true }).fill(process.env.E2E_ADMIN_PASSWORD!); await page.getByRole('button', { name: 'Entrar a mi finca' }).click(); await expect(page.getByRole('heading', { name: 'Ficha del animal', exact: true })).toBeVisible();
 await page.getByRole('button', { name: 'Archivar ficha', exact: true }).click(); await page.getByRole('dialog', { name: 'Confirmar acción' }).getByRole('button', { name: 'Archivar animal', exact: true }).click(); await expect(page.getByRole('heading', { name: 'Animales', exact: true })).toBeVisible();
 expect((await request.get('/api/animals/' + id, { headers })).status()).toBe(404); expect((await request.get(photo.url, { headers })).status()).toBe(404);
 await page.goto('/archive'); await page.getByLabel('Buscar registro archivado').fill(tag); await page.getByLabel('Tipo de registro archivado').selectOption('animals'); await page.getByRole('button', { name: 'Filtrar', exact: true }).click();
 const trigger = page.getByRole('button', { name: new RegExp('^Revisar ' + tag) }); await trigger.click(); const panel = page.getByRole('dialog', { name: 'Registro archivado' }); await expect(panel.getByRole('table', { name: 'Datos conservados' })).toBeVisible();
 await page.evaluate(async () => { await document.fonts.ready; await Promise.all(document.getAnimations().filter(a => a.effect?.getComputedTiming().iterations !== Infinity).map(a => a.finished.catch(() => {}))); });
 expect((await new AxeBuilder({ page }).withTags(['wcag2a','wcag2aa','wcag21aa','wcag22aa']).analyze()).violations).toEqual([]); expect(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth)).toBe(false);
 if (process.env.QUALITY_EVIDENCE_DIR) { await mkdir(process.env.QUALITY_EVIDENCE_DIR, { recursive: true }); await page.screenshot({ path: join(process.env.QUALITY_EVIDENCE_DIR, 'archive-detail-' + info.project.name + '.png'), fullPage: true }); }
 await panel.getByRole('button', { name: 'Restaurar registro' }).click(); await page.getByRole('dialog', { name: 'Confirmar acción' }).getByRole('button', { name: 'Restaurar', exact: true }).click(); await expect(panel).not.toBeVisible(); expect((await request.get('/api/animals/' + id, { headers })).status()).toBe(200);
 expect(Buffer.from(await (await request.get(photo.url, { headers })).body())).toEqual(bytes);
 const history = (await (await request.get('/api/admin/auditlogs?entityName=Animal&search=' + id, { headers })).json()).items; expect(history.some((e: { action: string }) => e.action === 'Archived')).toBe(true); expect(history.some((e: { action: string }) => e.action === 'Restored')).toBe(true);
 expect((await request.delete('/api/animals/' + id, { headers })).status()).toBe(204);
});
test('historical milk remains after archiving its animal and annulled yield is excluded from KPI totals', async ({ request }) => {
 const { headers, id, payload } = await fixture(request); const day = new Date().toISOString().slice(0,10);
 const url = '/api/analytics/overview?from=' + day + '&to=' + day + '&farmId=' + payload.farmId;
 const before = (await (await request.get(url, { headers })).json()).milkByDay.reduce((sum: number, point: { value: number }) => sum + point.value, 0);
 const result = await request.post('/api/production', { headers, data: { farmId: payload.farmId, animalId: id, date: day, productType: 'Milk', method: 'Milking', quantity: 7.1234, unit: 'Liter', operationId: randomUUID() } }); expect(result.status()).toBe(201); const production = (await result.json()).id;
 expect((await (await request.get(url, { headers })).json()).milkByDay.reduce((sum: number, point: { value: number }) => sum + point.value, 0)).toBeCloseTo(before + 7.1234, 4);
 expect((await request.delete('/api/animals/' + id, { headers })).status()).toBe(204); expect((await (await request.get(url, { headers })).json()).milkByDay.reduce((sum: number, point: { value: number }) => sum + point.value, 0)).toBeCloseTo(before + 7.1234, 4);
 expect((await request.delete('/api/production/' + production, { headers })).status()).toBe(204); expect((await (await request.get(url, { headers })).json()).milkByDay.reduce((sum: number, point: { value: number }) => sum + point.value, 0)).toBeCloseTo(before, 4);
 expect((await request.post('/api/admin/archive/animals/' + id + '/restore', { headers })).status()).toBe(204); expect((await request.post('/api/admin/archive/production/' + production + '/restore', { headers })).status()).toBe(204);
 expect((await (await request.get(url, { headers })).json()).milkByDay.reduce((sum: number, point: { value: number }) => sum + point.value, 0)).toBeCloseTo(before + 7.1234, 4);
 expect((await request.delete('/api/production/' + production, { headers })).status()).toBe(204); expect((await request.delete('/api/animals/' + id, { headers })).status()).toBe(204);
});
test('employees cannot access the archive or maintain the currency reference', async ({ request }) => {
 const headers = await authorization(request, true);
 expect((await request.get('/api/admin/archive', { headers })).status()).toBe(403); expect((await request.post('/api/admin/archive/animals/' + randomUUID() + '/restore', { headers })).status()).toBe(403); expect((await request.post('/api/exchange-rates/usd-ves/sync', { headers })).status()).toBe(403);
});
