import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { randomUUID } from 'node:crypto';
import { mkdir } from 'node:fs/promises';
import { join } from 'node:path';
import type { Page } from '@playwright/test';
test.skip(process.env.E2E_ISOLATED_DATABASE !== '1', 'Catalog mutations require the disposable Compose database.');
async function login(page: Page, employee = false) {
  await page.goto('/login'); await page.getByLabel('Usuario o correo').fill(employee ? process.env.E2E_EMPLOYEE_USERNAME! : process.env.E2E_ADMIN_USERNAME!);
  await page.getByLabel('Contraseña', { exact: true }).fill(employee ? process.env.E2E_EMPLOYEE_PASSWORD! : process.env.E2E_ADMIN_PASSWORD!);
  await page.getByRole('button', { name: 'Entrar a mi finca' }).click(); await expect(page.getByRole('heading', { name: employee ? 'Animales' : 'Dashboard', exact: true })).toBeVisible();
}
async function settled(page: Page) { await page.evaluate(async () => { await document.fonts.ready; await Promise.all(document.getAnimations().filter(a => a.effect?.getComputedTiming().iterations !== Infinity).map(a => a.finished.catch(() => {}))); }); }
async function accessible(page: Page) { await settled(page); const result = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa']).analyze(); expect(result.violations).toEqual([]); }
test('dashboard filters restore focus, discard drafts and keep mobile indicators in two columns', async ({ page }, info) => {
  await login(page); const metrics = page.locator('.dashboard-summary > article'); await expect(metrics).toHaveCount(4); await settled(page);
  if (process.env.QUALITY_EVIDENCE_DIR) { await mkdir(process.env.QUALITY_EVIDENCE_DIR, { recursive: true }); await page.screenshot({ path: join(process.env.QUALITY_EVIDENCE_DIR, 'dashboard-' + info.project.name + '.png') }); await page.locator('.reproduction-summary').screenshot({ path: join(process.env.QUALITY_EVIDENCE_DIR, 'reproduction-' + info.project.name + '.png') }); }
  await expect(page.getByLabel('Desde', { exact: true })).toHaveCount(0);
  const trigger = page.getByRole('button', { name: 'Filtros', exact: true }); await trigger.click(); const dialog = page.getByRole('dialog', { name: 'Filtros del dashboard' });
  const original = await dialog.getByLabel('Desde', { exact: true }).inputValue(); await dialog.getByLabel('Desde', { exact: true }).fill('2000-01-01'); await accessible(page);
  await page.keyboard.press('Escape'); await expect(dialog).toHaveCount(0); await expect(trigger).toBeFocused();
  await trigger.click(); await expect(dialog.getByLabel('Desde', { exact: true })).toHaveValue(original);
  await dialog.getByLabel('Desde', { exact: true }).fill('2000-01-01'); await dialog.getByLabel('Hasta', { exact: true }).fill('2000-01-02');
  await dialog.getByRole('button', { name: 'Aplicar filtros', exact: true }).click(); await expect(dialog).toHaveCount(0);
  await expect(page.getByRole('article', { name: 'Preñez en hembras evaluadas', exact: true })).toContainText('Sin evaluación concluyente');
  await expect(page.getByRole('region', { name: 'Fertilidad de servicios evaluados', exact: true })).toContainText('Sin servicios evaluados');
  if (info.project.name === 'mobile') { const boxes = await metrics.evaluateAll(items => items.map(item => { const b = item.getBoundingClientRect(); return { x: b.x, y: b.y, width: b.width }; })); expect(boxes[1].x).toBeGreaterThan(boxes[0].x); expect(boxes[0].y).toBe(boxes[1].y); expect(boxes[0].width).toBeGreaterThan(120); }
  expect(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth)).toBe(false);
  await page.emulateMedia({ reducedMotion: 'reduce' }); expect(await metrics.first().evaluate(item => getComputedStyle(item).animationName)).toBe('none');
  await accessible(page);
  if (process.env.QUALITY_EVIDENCE_DIR) { await mkdir(process.env.QUALITY_EVIDENCE_DIR, { recursive: true }); await page.screenshot({ path: join(process.env.QUALITY_EVIDENCE_DIR, 'dashboard-empty-' + info.project.name + '.png') }); await page.locator('.reproduction-summary').screenshot({ path: join(process.env.QUALITY_EVIDENCE_DIR, 'reproduction-empty-' + info.project.name + '.png') }); }
});
test('administrator manages farms, species, breeds, lots and paddock dependencies from the SPA', async ({ page, request }, info) => {
  expect(info.project.use.baseURL).toBe('http://localhost:18089'); const prefix = 'P4-MGT-' + randomUUID().slice(0, 8).toUpperCase();
  await login(page); if (info.project.name === 'mobile') await page.getByRole('button', { name: 'Más secciones', exact: true }).click();
  await page.getByRole('link', { name: 'Fincas y catálogos', exact: true }).click(); await expect(page.getByRole('heading', { name: 'Fincas y catálogos', exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Nueva finca', exact: true }).click(); let dialog = page.getByRole('dialog', { name: 'Nueva finca', exact: true });
  await dialog.getByLabel('Nombre', { exact: true }).fill(prefix + ' Finca'); await dialog.getByLabel('Código', { exact: true }).fill(prefix); await dialog.getByLabel('Dirección', { exact: true }).fill('Sector de prueba'); await dialog.getByLabel('Correo', { exact: true }).fill('farm@example.org'); await accessible(page);
  await dialog.getByRole('button', { name: 'Guardar cambios', exact: true }).click(); await expect(dialog).toHaveCount(0); await expect(page.getByRole('article', { name: prefix + ' Finca' })).toContainText('farm@example.org');
  await page.getByRole('link', { name: 'Especies', exact: true }).click(); await page.getByRole('button', { name: 'Nueva especie', exact: true }).click(); dialog = page.getByRole('dialog', { name: 'Nueva especie', exact: true });
  await dialog.getByLabel('Nombre', { exact: true }).fill(prefix + ' Bovino'); await dialog.getByLabel('Código', { exact: true }).fill(prefix + 'S'); await dialog.getByLabel('Gestación de referencia (días)', { exact: true }).fill('283'); await dialog.getByRole('button', { name: 'Guardar cambios', exact: true }).click(); await expect(dialog).toHaveCount(0);
  await page.getByRole('link', { name: 'Razas', exact: true }).click(); await page.getByRole('button', { name: 'Nueva raza', exact: true }).click(); dialog = page.getByRole('dialog', { name: 'Nueva raza', exact: true });
  await dialog.getByLabel('Nombre', { exact: true }).fill(prefix + ' Raza'); await dialog.getByLabel('Especie', { exact: true }).selectOption({ label: prefix + ' Bovino' }); await dialog.getByLabel('Origen', { exact: true }).fill('Venezuela'); await dialog.getByRole('button', { name: 'Guardar cambios', exact: true }).click(); await expect(dialog).toHaveCount(0);
  let card = page.getByRole('article', { name: prefix + ' Raza' }); await expect(card).toContainText(prefix + ' Bovino'); await card.getByRole('button', { name: 'Editar raza', exact: true }).click(); dialog = page.getByRole('dialog', { name: 'Editar raza' }); await dialog.getByLabel('Origen', { exact: true }).fill('Origen actualizado'); await dialog.getByRole('button', { name: 'Guardar cambios', exact: true }).click(); await expect(dialog).toHaveCount(0); await expect(card).toContainText('Origen actualizado');
  await page.getByRole('link', { name: 'Gestionar potreros', exact: true }).click(); await page.getByRole('button', { name: 'Registrar potrero', exact: true }).click(); await page.getByLabel('Finca del potrero *', { exact: true }).selectOption({ label: prefix + ' Finca' }); await page.getByLabel('Nombre del potrero *', { exact: true }).fill(prefix + ' Potrero'); await page.getByRole('button', { name: 'Guardar potrero', exact: true }).click(); await expect(page.getByRole('heading', { name: 'Registrar potrero', exact: true })).toHaveCount(0);
  await page.goto('/management?tab=lots'); await page.getByRole('button', { name: 'Nuevo lote', exact: true }).click(); dialog = page.getByRole('dialog', { name: 'Nuevo lote' });
  await dialog.getByLabel('Nombre', { exact: true }).fill(prefix + ' Lote'); await dialog.getByLabel('Finca', { exact: true }).selectOption({ label: prefix + ' Finca' }); await dialog.getByLabel('Especie', { exact: true }).selectOption({ label: prefix + ' Bovino' }); await dialog.getByLabel('Potrero de referencia', { exact: true }).selectOption({ label: prefix + ' Potrero' }); await accessible(page); await dialog.getByRole('button', { name: 'Guardar cambios', exact: true }).click(); await expect(dialog).toHaveCount(0);
  await expect(page.getByRole('article', { name: prefix + ' Lote' })).toContainText(prefix + ' Potrero');
  await page.goto('/paddocks'); await page.getByRole('button', { name: 'Archivar ' + prefix + ' Potrero', exact: true }).click(); await page.getByRole('dialog', { name: 'Confirmar acción' }).getByRole('button', { name: 'Archivar potrero' }).click(); await expect(page.getByRole('alert').filter({ hasText: 'animales o lotes vinculados' })).toBeVisible();
  for (const [kind, name] of [['lots', prefix + ' Lote'], ['paddocks', prefix + ' Potrero'], ['breeds', prefix + ' Raza'], ['species', prefix + ' Bovino'], ['farms', prefix + ' Finca']]) {
    await page.goto(kind === 'paddocks' ? '/paddocks' : '/management?tab=' + kind);
    const archive = kind === 'paddocks' ? page.getByRole('button', { name: 'Archivar ' + name, exact: true }) : page.getByRole('article', { name, exact: true }).getByRole('button', { name: 'Archivar', exact: true });
    await archive.click(); const deletion = page.waitForResponse(response => response.request().method() === 'DELETE' && new URL(response.url()).pathname.startsWith('/api/' + kind + '/'));
    await page.getByRole('dialog', { name: 'Confirmar acción' }).getByRole('button', { name: kind === 'paddocks' ? 'Archivar potrero' : 'Archivar', exact: true }).click(); expect((await deletion).status()).toBe(204); await expect(archive).toHaveCount(0);
  }
  const loginResponse = await request.post('/api/auth/login', { data: { username: process.env.E2E_ADMIN_USERNAME, password: process.env.E2E_ADMIN_PASSWORD } }); const headers = { Authorization: 'Bearer ' + (await loginResponse.json()).accessToken };
  const archived = await (await request.get('/api/admin/archive?search=' + prefix + '&pageSize=100', { headers })).json(); expect(archived.total).toBe(5);
});
test('employee cannot open the catalog manager or see its navigation link', async ({ page }) => {
  await login(page, true); if (page.viewportSize()!.width < 760) await page.getByRole('button', { name: 'Más secciones', exact: true }).click(); await expect(page.getByRole('link', { name: 'Fincas y catálogos', exact: true })).toHaveCount(0);
  await page.goto('/management?tab=breeds'); await expect(page.getByRole('alert')).toContainText('administrador'); await expect(page.getByRole('button', { name: 'Nueva raza' })).toHaveCount(0);
});
