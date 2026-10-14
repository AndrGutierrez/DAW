import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import type { Page } from '@playwright/test';
import { mkdir } from 'node:fs/promises';
import { join } from 'node:path';

// These scenarios only query reports and analytics; no business records are created.
test.skip(!process.env.E2E_ADMIN_USERNAME || !process.env.E2E_ADMIN_PASSWORD, 'An administrator account is required.');
async function login(page: Page) {
  await page.goto('/login');
  await page.getByLabel('Usuario o correo').fill(process.env.E2E_ADMIN_USERNAME!);
  await page.getByLabel('Contraseña', { exact: true }).fill(process.env.E2E_ADMIN_PASSWORD!);
  await page.getByRole('button', { name: 'Entrar a mi finca' }).click();
  await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toBeVisible();
}
async function apply(page: Page, path: string, from: string, to: string) {
  const response = page.waitForResponse(response => {
    const url = new URL(response.url());
    return url.pathname === path && url.searchParams.get('from') === from && url.searchParams.get('to') === to;
  });
  await page.getByRole('button', { name: 'Aplicar filtros', exact: true }).click();
  expect((await response).status()).toBe(200);
}
function recentRange(days: number) {
  const to = new Date().toISOString().slice(0, 10);
  const from = new Date(Date.parse(to + 'T00:00:00Z') - (days - 1) * 86400000).toISOString().slice(0, 10);
  return { from, to };
}
async function accessible(page: Page) {
  await page.evaluate(async () => { await document.fonts.ready; await Promise.all(document.getAnimations().filter(animation => animation.effect?.getComputedTiming().iterations !== Infinity).map(animation => animation.finished.catch(() => {}))); });
  expect((await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa']).analyze()).violations).toEqual([]);
  expect(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth)).toBe(false);
}

test('dashboard applies a month, a year, recent days and an editable derived range', async ({ page }, info) => {
  await login(page);
  const trigger = page.getByRole('button', { name: 'Filtros', exact: true });
  await trigger.click();
  await page.getByLabel('Período', { exact: true }).selectOption('month');
  await page.getByLabel('Mes', { exact: true }).fill('2024-02');
  await accessible(page);
  await apply(page, '/api/analytics/overview', '2024-02-01', '2024-02-29');
  await expect(trigger).toBeFocused();
  await trigger.click();
  await page.getByLabel('Período', { exact: true }).selectOption('year');
  await page.getByLabel('Año', { exact: true }).fill('2025');
  await apply(page, '/api/analytics/overview', '2025-01-01', '2025-12-31');
  await trigger.click();
  await page.getByLabel('Período', { exact: true }).selectOption('recent');
  await page.getByLabel('Últimos días', { exact: true }).selectOption('7');
  const seven = recentRange(7);
  await apply(page, '/api/analytics/overview', seven.from, seven.to);
  await trigger.click();
  await page.getByLabel('Período', { exact: true }).selectOption('recent');
  await page.getByLabel('Últimos días', { exact: true }).selectOption('custom');
  await page.getByLabel('Cantidad de días', { exact: true }).fill('15');
  await accessible(page);
  if (process.env.QUALITY_EVIDENCE_DIR) {
    await mkdir(process.env.QUALITY_EVIDENCE_DIR, { recursive: true });
    await page.screenshot({ path: join(process.env.QUALITY_EVIDENCE_DIR, 'period-filters-' + info.project.name + '.png') });
  }
  const fifteen = recentRange(15);
  await page.getByLabel('Período', { exact: true }).selectOption('custom');
  await expect(page.getByLabel('Desde', { exact: true })).toHaveValue(fifteen.from);
  await expect(page.getByLabel('Hasta', { exact: true })).toHaveValue(fifteen.to);
  await apply(page, '/api/analytics/overview', fifteen.from, fifteen.to);
});

test('reports use the shared month and recent-day filters', async ({ page }) => {
  await login(page);
  await page.goto('/reports?kind=production');
  await expect(page.getByRole('heading', { name: 'Reportes', exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Aplicar filtros', exact: true })).toBeEnabled();
  await page.getByLabel('Período', { exact: true }).selectOption('month');
  await page.getByLabel('Mes', { exact: true }).fill('2024-02');
  await apply(page, '/api/reports/production', '2024-02-01', '2024-02-29');
  await expect(page.getByRole('button', { name: 'Aplicar filtros', exact: true })).toBeEnabled();
  await page.getByLabel('Período', { exact: true }).selectOption('recent');
  await page.getByLabel('Últimos días', { exact: true }).selectOption('90');
  const range = recentRange(90);
  await apply(page, '/api/reports/production', range.from, range.to);
  await expect(page.getByRole('button', { name: 'Aplicar filtros', exact: true })).toBeEnabled();
  await accessible(page);
});
