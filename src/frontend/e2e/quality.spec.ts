import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { mkdir, writeFile } from 'node:fs/promises';
import { join } from 'node:path';
const routes = ['/dashboard', '/animals', '/weighing', '/paddocks', '/inventory', '/reports', '/monitoring', '/users', '/account', '/diagnostics'];
test('essential screens have no detected WCAG A/AA violations in both themes', async ({ page }, info) => {
 test.setTimeout(240000);
 await page.goto('/login');
 const findings: unknown[] = [];
 for (const theme of ['light', 'dark']) {
  if (theme === 'dark') await page.getByRole('button', { name: 'Activar tema oscuro' }).click();
  await page.evaluate(async () => { await document.fonts.ready; await Promise.all(document.getAnimations().filter(a => a.effect?.getComputedTiming().iterations !== Infinity).map(a => a.finished.catch(() => {}))); });
  const result = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa']).analyze();
  findings.push({ route: '/login', theme, violations: result.violations });
  expect.soft(result.violations, '/login ' + theme).toEqual([]);
 }
 await page.getByLabel('Usuario o correo').fill(process.env.E2E_ADMIN_USERNAME!);
 await page.getByLabel('Contraseña', { exact: true }).fill(process.env.E2E_ADMIN_PASSWORD!);
 await page.getByRole('button', { name: 'Entrar a mi finca' }).click();
 await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toBeVisible();
 for (const theme of ['dark', 'light']) {
  if (theme === 'light') await page.getByRole('button', { name: 'Activar tema claro' }).click();
  for (const route of routes) {
   await page.goto(route); await expect(page.locator('main h1')).toBeVisible();
   await expect(page.locator('.route-loading')).toHaveCount(0);
   await expect(page.locator('main [aria-label="Cargando indicadores"]')).toHaveCount(0);
   const result = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa']).analyze();
   findings.push({ route, theme, violations: result.violations });
   expect.soft(result.violations, route + ' ' + theme).toEqual([]);
   expect.soft(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), route + ' width').toBe(false);
  }
 }
 const folder = process.env.QUALITY_EVIDENCE_DIR;
 if (folder) { await mkdir(folder, { recursive: true }); await writeFile(join(folder, 'accessibility-' + info.project.name + '.json'), JSON.stringify(findings, null, 2)); }
});
test('mobile More navigation closes with Escape and restores focus', async ({ page }, info) => {
 test.skip(info.project.name !== 'mobile');
 await page.goto('/login');
 await page.getByLabel('Usuario o correo').fill(process.env.E2E_ADMIN_USERNAME!);
 await page.getByLabel('Contraseña', { exact: true }).fill(process.env.E2E_ADMIN_PASSWORD!);
 await page.getByRole('button', { name: 'Entrar a mi finca' }).click();
 const more = page.getByRole('button', { name: 'Más secciones', exact: true }); await more.click();
 await expect(page.getByRole('link', { name: 'Inventario', exact: true })).toBeVisible();
 await page.keyboard.press('Escape'); await expect(more).toBeFocused();
 await more.click(); await page.getByRole('link', { name: 'Mi cuenta', exact: true }).click();
 await expect(page.getByRole('heading', { name: 'Mi cuenta', exact: true })).toBeVisible();
 await expect(page.locator('.mobile-menu')).toHaveCount(0);
});
test('reduced motion disables decorative transitions and diagnostic export has no credentials', async ({ page }, info) => {
 await page.emulateMedia({ reducedMotion: 'reduce' }); await page.goto('/login');
 await page.getByLabel('Usuario o correo').fill(process.env.E2E_ADMIN_USERNAME!);
 await page.getByLabel('Contraseña', { exact: true }).fill(process.env.E2E_ADMIN_PASSWORD!);
 await page.getByRole('button', { name: 'Entrar a mi finca' }).click();
 await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toBeVisible();
 expect(await page.locator('.button').first().evaluate(element => getComputedStyle(element).transitionDuration)).toBe('0s');
 await page.goto('/dashboard'); await expect(page.getByRole('article', { name: 'Leche registrada', exact: true })).toBeVisible();
 if (info.project.name === 'mobile') await page.getByRole('button', { name: 'Más secciones', exact: true }).click();
 await page.getByRole('link', { name: 'Mi cuenta', exact: true }).click(); await page.getByRole('link', { name: 'Ver rendimiento de la aplicación' }).click(); await expect(page.getByRole('heading', { name: 'Rendimiento de la aplicación' })).toBeVisible();
 const downloaded = page.waitForEvent('download'); await page.getByRole('button', { name: 'Exportar mediciones' }).click();
 const download = await downloaded; const stream = await download.createReadStream(); const chunks = []; for await (const chunk of stream!) chunks.push(chunk); const report = JSON.parse(Buffer.concat(chunks).toString());
 expect(report.timings.length).toBeGreaterThan(0);
 const serialized = JSON.stringify(report); expect(serialized).not.toMatch(new RegExp('Bearer|accessToken|csrf|password|Authorization|/api/', 'i'));
 expect(serialized).not.toContain(process.env.E2E_ADMIN_PASSWORD!);
 expect(report.vitals.some((v: { name: string }) => v.name === 'LCP')).toBe(true);
 const folder = process.env.QUALITY_EVIDENCE_DIR;
 if (folder) { await mkdir(folder, { recursive: true }); await writeFile(join(folder, 'performance-' + info.project.name + '.json'), JSON.stringify(report, null, 2)); await page.screenshot({ path: join(folder, 'diagnostics-' + info.project.name + '.png'), fullPage: true }); }
});

test('local cross-field validation retains data without sending invalid requests', async ({ page }) => {
 await page.goto('/inventory');
 await page.getByLabel('Usuario o correo').fill(process.env.E2E_ADMIN_USERNAME!);
 await page.getByLabel('Contraseña', { exact: true }).fill(process.env.E2E_ADMIN_PASSWORD!);
 await page.getByRole('button', { name: 'Entrar a mi finca' }).click();
 await page.getByRole('button', { name: 'Nuevo inventario', exact: true }).click();
 const panel = page.getByRole('dialog');
 await panel.getByLabel('Finca', { exact: true }).selectOption({ label: 'Finca El Paraíso' });
 await expect(panel.getByLabel('Producto', { exact: true }).locator('option')).not.toHaveCount(1);
 const first = await panel.getByLabel('Producto', { exact: true }).locator('option').nth(1).getAttribute('value');
 await panel.getByLabel('Producto', { exact: true }).selectOption(first!);
 await panel.getByLabel('Stock mínimo', { exact: true }).fill('10'); await panel.getByLabel('Stock máximo', { exact: true }).fill('5');
 let submissions = 0; page.on('request', request => { if (request.method() === 'POST' && new URL(request.url()).pathname === '/api/inventory') submissions++; });
 await panel.getByRole('button', { name: 'Guardar cambios', exact: true }).click();
 await expect(panel.getByLabel('Stock máximo', { exact: true })).toHaveAttribute('aria-invalid', 'true');
 await expect(panel.getByLabel('Stock máximo', { exact: true })).toHaveValue('5');
 expect(submissions).toBe(0);
});
