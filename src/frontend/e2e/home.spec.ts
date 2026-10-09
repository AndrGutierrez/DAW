import { test, expect } from '@playwright/test';
import type { Page } from '@playwright/test';

async function signIn(page: Page, employee = false) {
  await page.getByLabel('Usuario o correo').fill((employee ? process.env.E2E_EMPLOYEE_USERNAME : process.env.E2E_ADMIN_USERNAME)!);
  await page.getByLabel('Contraseña', { exact: true }).fill((employee ? process.env.E2E_EMPLOYEE_PASSWORD : process.env.E2E_ADMIN_PASSWORD)!);
  await page.getByRole('button', { name: 'Entrar a mi finca' }).click();
}
for (const employee of [false, true]) {
  test((employee ? 'employee' : 'admin') + ' home respects role on root, restored session and login', async ({ page }) => {
    await page.goto('/'); await signIn(page, employee);
    const path = employee ? '/animals' : '/dashboard', heading = employee ? 'Animales' : 'Dashboard';
    await expect(page).toHaveURL(new RegExp(path + '$')); await expect(page.getByRole('heading', { name: heading, exact: true })).toBeVisible();
    await page.reload(); await expect(page.getByRole('heading', { name: heading, exact: true })).toBeVisible();
    await page.goto('/login'); await expect(page).toHaveURL(new RegExp(path + '$'));
    await page.getByRole('link', { name: 'Animales', exact: true }).click(); await page.locator('a.brand').click(); await expect(page).toHaveURL(new RegExp(path + '$'));
    if (employee) { await expect(page.getByRole('link', { name: 'Dashboard', exact: true })).toHaveCount(0); await page.goto('/dashboard'); await expect(page.getByRole('alert')).toContainText('administrador'); }
    expect(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth)).toBe(false);
  });
}
test('admin direct login opens live cattle indicators', async ({ page }) => {
  await page.goto('/login');
  const response = page.waitForResponse(r => r.url().includes('/api/analytics/overview?') && r.status() === 200);
  await signIn(page); await expect(page).toHaveURL(/\/dashboard$/);
  const data = await (await response).json();
  const number = (value: number, digits: number) => new Intl.NumberFormat('es-VE', { maximumFractionDigits: digits }).format(value);
  const milk = data.milkByDay.reduce((sum: number, day: { value: number }) => sum + day.value, 0);
  const cattle = data.weightByAge.reduce((sum: number, group: { count: number }) => sum + group.count, 0);
  await expect(page.getByRole('article', { name: 'Leche registrada', exact: true }).locator('strong')).toHaveText(data.milkByDay.length ? number(milk, 3) + ' L' : 'Sin registros');
  await expect(page.getByRole('article', { name: 'Bovinos con pesaje comparable', exact: true }).locator('strong')).toHaveText(number(cattle, 0));
  await expect(page.getByRole('article', { name: 'Preñez en hembras evaluadas', exact: true }).locator('strong')).toHaveText(data.reproduction.pregnancyPercent === null ? 'Sin evaluación concluyente' : number(data.reproduction.pregnancyPercent, 2) + '%');
  await expect(page.getByRole('article', { name: 'Existencias críticas', exact: true }).locator('strong')).toHaveText(number(data.critical, 0));
  await expect(page.getByLabel('Gráfico', { exact: true })).toHaveValue('milk');
  expect(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth)).toBe(false);
});
test('login returns to the requested animal tab and fragment', async ({ page, request }) => {
  const auth = await request.post('/api/auth/login', { data: { username: process.env.E2E_ADMIN_USERNAME, password: process.env.E2E_ADMIN_PASSWORD } }); expect(auth.status()).toBe(200);
  const headers = { Authorization: 'Bearer ' + (await auth.json()).accessToken };
  const animals = await (await request.get('/api/animals/page?search=DEMO-001', { headers })).json();
  const path = '/animals/' + animals.items[0].id + '?tab=location#history';
  await page.goto(path); await signIn(page); await expect(page).toHaveURL(new URL(path, process.env.E2E_BASE_URL || 'http://localhost:18086').href);
  await expect(page.getByRole('tab', { name: 'Ubicación', exact: true })).toHaveAttribute('aria-selected', 'true');
});

test('weight distribution matches the protected API and filters the histogram by age', async ({ page }) => {
 await page.goto('/login');
 const response = page.waitForResponse(r => r.url().includes('/api/analytics/overview?') && r.status() === 200);
 await signIn(page); const data = await (await response).json();
 expect(data.weightDistributionByAge.reduce((sum: number, p: { count: number }) => sum + p.count, 0)).toBe(data.weightByAge.reduce((sum: number, p: { count: number }) => sum + p.count, 0));
 const table = page.getByRole('heading', { name: 'Distribución de peso por edad', exact: true }).locator('..').getByRole('table');
 await expect(table.locator('tbody tr')).toHaveCount(data.weightDistributionByAge.length);
 await page.getByLabel('Gráfico', { exact: true }).selectOption('distribution');
 const groups = [...new Set<string>(data.weightDistributionByAge.map((p: { ageGroup: string }) => p.ageGroup))];
 if (groups.length) {
  await expect(page.getByLabel('Edad al pesaje', { exact: true })).toHaveValue(groups[0]);
  if (groups.length > 1) { await page.getByLabel('Edad al pesaje', { exact: true }).selectOption(groups[groups.length - 1]); await expect(page.getByLabel('Edad al pesaje', { exact: true })).toHaveValue(groups[groups.length - 1]); }
 }
 expect(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth)).toBe(false);
});
