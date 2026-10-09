import { test, expect } from '@playwright/test';
import type { Page } from '@playwright/test';
import { randomUUID } from 'node:crypto';

let headers: Record<string, string>, animalId: string, paddockId: string, prefix: string;
const username = process.env.E2E_ADMIN_USERNAME!, password = process.env.E2E_ADMIN_PASSWORD!;
async function login(page: Page) {
  await page.goto('/animals');
  await page.getByLabel('Usuario o correo').fill(username);
  await page.getByLabel('Contraseña', { exact: true }).fill(password);
  await page.getByRole('button', { name: 'Entrar a mi finca' }).click();
  await expect(page.getByRole('heading', { name: 'Animales', exact: true })).toBeVisible();
}
test.beforeEach(async ({ request }, info) => {
  test.skip(process.env.E2E_ISOLATED_DATABASE !== '1', 'Mutation fixtures require the named disposable database.');
  animalId = ''; paddockId = '';
  prefix = ('P4-UI-' + info.project.name[0] + '-' + randomUUID().slice(0, 8)).toUpperCase();
  const login = await request.post('/api/auth/login', { data: { username, password } });
  expect(login.status()).toBe(200); headers = { Authorization: 'Bearer ' + (await login.json()).accessToken };
  const farmId = (await (await request.get('/api/farms', { headers })).json()).find((x: { data: { code: string } }) => x.data.code === 'DEMO').id;
  const speciesId = (await (await request.get('/api/species', { headers })).json()).find((x: { data: { code: string } }) => x.data.code === 'BO').id;
  const paddock = await request.post('/api/paddocks', { headers, data: { farmId, name: prefix, areaHectares: 2, capacity: 5 } });
  expect(paddock.status()).toBe(201); paddockId = (await paddock.json()).id;
  const animal = await request.post('/api/animals', { headers, data: { farmId, speciesId, internalTag: prefix, name: prefix, paddockId, sex: 'Female', purpose: 'Milk', healthStatus: 'Critical' } });
  expect(animal.status()).toBe(201); animalId = (await animal.json()).id;
});
test.afterEach(async ({ request }) => {
  if (process.env.E2E_ISOLATED_DATABASE !== '1') return;
  if (animalId) expect((await request.delete('/api/animals/' + animalId, { headers })).status()).toBe(204);
  if (paddockId) expect((await request.delete('/api/paddocks/' + paddockId, { headers })).status()).toBe(204);
});

test('text and semantic states remain readable in both themes', async ({ page }) => {
  await login(page);
  await page.getByLabel('Buscar animal').fill(prefix);
  await page.getByRole('button', { name: 'Buscar', exact: true }).click();
  await expect(page.getByRole('article')).toHaveCount(1);
  await expect(page.getByRole('article').getByText('Crítico', { exact: true })).toHaveClass(/critical/);
  for (const dark of [false, true]) {
    if (dark) await page.getByRole('button', { name: 'Activar tema oscuro' }).click();
    await page.getByRole('button', { name: 'Buscar', exact: true }).hover();
    const samples = await page.evaluate(() => {
      function rgb(value: string) { return value.match(/[\d.]+/g)!.slice(0, 3).map(Number); }
      function luminance(values: number[]) { const channels = values.map(v => { const n = v / 255; return n <= .04045 ? n / 12.92 : ((n + .055) / 1.055) ** 2.4; }); return channels[0] * .2126 + channels[1] * .7152 + channels[2] * .0722; }
      return [...document.querySelectorAll<HTMLElement>('.animal-card .status-pill, .animal-card .tag, .animal-card-content > .muted, .page-heading p, .button.primary, .button.secondary:not(:disabled), input, select')].map(element => {
        const foreground = getComputedStyle(element).color; let current: HTMLElement | null = element; let background = '';
        while (current) { background = getComputedStyle(current).backgroundColor; if (background !== 'rgba(0, 0, 0, 0)' && background !== 'transparent') break; current = current.parentElement; }
        const a = luminance(rgb(foreground)), b = luminance(rgb(background));
        return { text: element.textContent?.trim() || element.tagName, contrast: (Math.max(a, b) + .05) / (Math.min(a, b) + .05) };
      });
    });
    expect(samples.length).toBeGreaterThan(6);
    for (const sample of samples) expect(sample.contrast, sample.text + ' in ' + (dark ? 'dark' : 'light')).toBeGreaterThanOrEqual(4.5);
    const fields = await page.locator('input, select').evaluateAll(elements => {
      const luminance = (color: string) => { const c = color.match(/[\d.]+/g)!.slice(0, 3).map(Number).map(v => { const n = v / 255; return n <= .04045 ? n / 12.92 : ((n + .055) / 1.055) ** 2.4; }); return c[0] * .2126 + c[1] * .7152 + c[2] * .0722; };
      const contrast = (a: string, b: string) => { const x = luminance(a), y = luminance(b); return (Math.max(x, y) + .05) / (Math.min(x, y) + .05); };
      return elements.map(element => {
        const style = getComputedStyle(element); const placeholder = getComputedStyle(element, '::placeholder');
        const fg = placeholder.color.match(/[\d.]+/g)!.slice(0, 3).map(Number), bg = style.backgroundColor.match(/[\d.]+/g)!.slice(0, 3).map(Number), opacity = Number(placeholder.opacity);
        const painted = 'rgb(' + fg.map((n, i) => n * opacity + bg[i] * (1 - opacity)).join(',') + ')';
        return { border: contrast(style.borderColor, style.backgroundColor), placeholder: element.tagName === 'INPUT' ? contrast(painted, style.backgroundColor) : null };
      });
    });
    for (const field of fields) { expect(field.border).toBeGreaterThanOrEqual(3); if (field.placeholder !== null) expect(field.placeholder).toBeGreaterThanOrEqual(4.5); }
    expect(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth)).toBe(false);
  }
});

test('dirty animal survives menu, browser back and logout until leaving is confirmed', async ({ page, request }) => {
  const errors: string[] = []; page.on('pageerror', error => errors.push(error.message));
  await login(page); await page.goto('/animals/' + animalId);
  await page.getByRole('link', { name: 'Editar ficha' }).click();
  await page.getByLabel('Nombre', { exact: true }).fill(prefix + '-DRAFT');
  await page.getByRole('navigation', { name: 'Principal', exact: true }).getByRole('link', { name: 'Potreros', exact: true }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Cancelar', exact: true }).click();
  await expect(page.getByLabel('Nombre', { exact: true })).toHaveValue(prefix + '-DRAFT');
  await page.goBack();
  await page.getByRole('dialog').getByRole('button', { name: 'Cancelar', exact: true }).click();
  await expect(page).toHaveURL(new RegExp('/animals/' + animalId + '/edit$'));
  await page.getByRole('button', { name: 'Salir', exact: true }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Cancelar', exact: true }).click();
  await expect(page.getByLabel('Nombre', { exact: true })).toHaveValue(prefix + '-DRAFT');
  await page.getByRole('navigation', { name: 'Principal', exact: true }).getByRole('link', { name: 'Potreros', exact: true }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Salir sin guardar', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Potreros', exact: true })).toBeVisible();
  expect((await (await request.get('/api/animals/' + animalId, { headers })).json()).name).toBe(prefix);
  expect(errors).toEqual([]);
});

test('paddock edits keep values after cancellation and checkbox supports keyboard', async ({ page, request }) => {
  await login(page); await page.goto('/paddocks?search=' + prefix);
  await page.getByRole('button', { name: 'Editar ' + prefix, exact: true }).click();
  await page.getByLabel('Capacidad máxima (animales)', { exact: true }).fill('7');
  const active = page.getByLabel('Potrero activo', { exact: true });
  await active.focus(); await active.press('Space'); await expect(active).not.toBeChecked();
  await active.press('Space'); await expect(active).toBeChecked();
  await page.getByRole('navigation', { name: 'Principal', exact: true }).getByRole('link', { name: 'Animales', exact: true }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Cancelar', exact: true }).click();
  await expect(page.getByLabel('Capacidad máxima (animales)', { exact: true })).toHaveValue('7');
  await page.getByRole('button', { name: 'Cancelar', exact: true }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Confirmar', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Editar potrero', exact: true })).not.toBeVisible();
  expect((await (await request.get('/api/paddocks/' + paddockId, { headers })).json()).data.capacity).toBe(5);
});

test('record tabs show one panel, support keyboard and preserve entered values', async ({ page }) => {
  await login(page); await page.goto('/animals/' + animalId);
  const tabs = page.getByRole('tablist', { name: 'Secciones de la ficha', exact: true });
  const summary = tabs.getByRole('tab', { name: 'Resumen', exact: true });
  await expect(summary).toHaveAttribute('aria-selected', 'true');
  await expect(page.getByRole('tabpanel')).toHaveCount(1);
  await summary.focus(); await summary.press('End');
  const photos = tabs.getByRole('tab', { name: 'Fotografías', exact: true });
  await expect(photos).toBeFocused(); await expect(summary).toHaveAttribute('aria-selected', 'true');
  await photos.press('Enter');
  await expect(page).toHaveURL(/tab=photos$/);
  await expect(page.getByRole('tabpanel', { name: 'Fotografías', exact: true })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Identificación y ubicación', exact: true })).not.toBeVisible();
  await photos.press('ArrowLeft');
  const growth = tabs.getByRole('tab', { name: 'Crecimiento', exact: true });
  await expect(growth).toBeFocused(); await growth.press('Space');
  await expect(page.getByRole('tabpanel', { name: 'Crecimiento', exact: true })).toBeVisible();
  await page.getByLabel('Peso vivo (kg) *', { exact: true }).fill('145');
  await summary.click(); await growth.click();
  await expect(page.getByLabel('Peso vivo (kg) *', { exact: true })).toHaveValue('145');
  await page.reload(); await expect(growth).toHaveAttribute('aria-selected', 'true');
  expect(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth)).toBe(false);
});

test('paddock occupancy opens in a modal side panel and restores focus on close', async ({ page }) => {
  await login(page); await page.goto('/paddocks?search=' + prefix);
  const trigger = page.locator('.paddock-select');
  await trigger.click();
  const drawer = page.getByRole('dialog', { name: prefix, exact: true });
  await expect(drawer).toBeVisible();
  await expect(drawer.getByRole('table', { name: 'Animales presentes en el potrero' })).toContainText(prefix);
  const width = page.viewportSize()!.width;
  await expect.poll(async () => { const bounds = await drawer.boundingBox(); return bounds!.x + bounds!.width; }).toBeCloseTo(width, 0);
  await page.getByRole('button', { name: 'Cerrar detalle del potrero' }).press('Tab');
  expect(await page.evaluate(() => !!document.activeElement?.closest('dialog.side-panel'))).toBe(true);
  await page.keyboard.press('Escape'); await expect(drawer).not.toBeVisible();
  await expect(trigger).toBeFocused();
  await trigger.click();
  await drawer.getByRole('button', { name: 'Trasladar ' + prefix, exact: true }).click();
  await page.getByLabel('Motivo del traslado *').fill('Keep this draft');
  await page.keyboard.press('Escape');
  const confirm = page.getByRole('dialog', { name: 'Confirmar acción', exact: true });
  await confirm.getByRole('button', { name: 'Cancelar', exact: true }).click();
  await expect(page.getByLabel('Motivo del traslado *')).toHaveValue('Keep this draft');
  await page.getByRole('button', { name: 'Cerrar detalle del potrero' }).click();
  await confirm.getByRole('button', { name: 'Descartar y cerrar', exact: true }).click();
  await expect(drawer).not.toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth)).toBe(false);
});
