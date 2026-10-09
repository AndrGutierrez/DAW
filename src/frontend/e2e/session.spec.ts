import { test, expect } from '@playwright/test';
import type { Page } from '@playwright/test';

const username = process.env.E2E_ADMIN_USERNAME;
const password = process.env.E2E_ADMIN_PASSWORD;
if (!username || !password) throw new Error('Set E2E_ADMIN_USERNAME and E2E_ADMIN_PASSWORD for the isolated test database.');

async function login(page: Page) {
  await page.goto('/animals');
  await expect(page.getByRole('heading', { name: 'Inicia sesión' })).toBeVisible();
  await page.getByLabel('Usuario o correo').fill(username!);
  await page.getByLabel('Contraseña', { exact: true }).fill(password!);
  await page.getByRole('button', { name: 'Entrar a mi finca' }).click();
  await expect(page.getByRole('heading', { name: 'Animales', exact: true })).toBeVisible();
  await expect(page.getByLabel('Cargando animales')).not.toBeVisible();
}

test('persistent session survives reload and a new tab without exposing tokens to storage', async ({ page, context }) => {
  const errors: string[] = [];
  page.on('pageerror', error => errors.push(error.message));
  await login(page);
  const cookies = await context.cookies();
  const refresh = cookies.find(cookie => cookie.name === 'daw.refresh');
  expect(refresh?.httpOnly).toBe(true);
  expect(refresh?.sameSite).toBe('Strict');
  expect(refresh?.expires).toBeGreaterThan(Date.now() / 1000);
  expect(refresh?.path).toBe('/api/auth/session');
  const storage = await page.evaluate(() => ({
    local: { ...localStorage }, session: { ...sessionStorage }, cookies: document.cookie,
  }));
  expect(Object.keys(storage.local)).toEqual(['daw.theme']);
  expect(storage.session).toEqual({});
  expect(storage.cookies).not.toContain('daw.refresh');
  await page.reload();
  await expect(page.getByRole('heading', { name: 'Animales', exact: true })).toBeVisible();
  const second = await context.newPage();
  await second.goto('/animals');
  await expect(second.getByRole('heading', { name: 'Animales', exact: true })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Animales', exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Salir', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Inicia sesión' })).toBeVisible();
  await expect(second.getByRole('heading', { name: 'Inicia sesión' })).toBeVisible();
  await page.reload();
  await expect(page.getByRole('heading', { name: 'Inicia sesión' })).toBeVisible();
  expect((await context.cookies()).some(cookie => cookie.name === 'daw.refresh')).toBe(false);
  expect(errors).toEqual([]);
});

test('search is performed by the API and animal details load on a direct URL', async ({ page }) => {
  await login(page);
  await page.getByLabel('Buscar animal').fill('DEMO-001');
  const request = page.waitForRequest(request => request.url().includes('/api/animals/page?') && request.url().includes('search=DEMO-001'));
  await page.getByRole('button', { name: 'Buscar', exact: true }).click();
  await request;
  await expect(page.getByLabel('Cargando animales')).not.toBeVisible();
  await expect(page.getByRole('article')).toHaveCount(1);
  await page.getByRole('link', { name: 'Ver ficha de DEMO-001' }).click();
  await expect(page.getByRole('heading', { name: 'Identificación y ubicación' })).toBeVisible();
  await page.getByRole('tab', { name: 'Genealogía', exact: true }).click();
  await page.reload();
  await expect(page.getByRole('tab', { name: 'Genealogía', exact: true })).toHaveAttribute('aria-selected', 'true');
  await expect(page.getByRole('heading', { name: 'Genealogía' })).toBeVisible();
  await expect(page.getByText('DEMO-001', { exact: true }).first()).toBeVisible();
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth);
  expect(overflow).toBe(false);
});

test('theme preference persists and wrong credentials show an accessible error', async ({ page }) => {
  await page.goto('/login');
  await expect(page.getByRole('heading', { name: 'Inicia sesión' })).toBeVisible();
  await page.getByRole('button', { name: 'Activar tema oscuro' }).click();
  await page.reload();
  await expect(page.getByRole('button', { name: 'Activar tema claro' })).toBeVisible();
  await page.getByLabel('Usuario o correo').fill(username!);
  await page.getByLabel('Contraseña', { exact: true }).fill('Wrong123!Password');
  await page.getByRole('button', { name: 'Entrar a mi finca' }).click();
  await expect(page.getByRole('alert')).toContainText('Usuario o contraseña incorrectos');
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth);
  expect(overflow).toBe(false);
});

test('PostgreSQL accepts one refresh rotation, rejects replay and preserves the valid successor', async ({ request }) => {
  const login = await request.post('/api/auth/login', { data: { username, password } });
  expect(login.status()).toBe(200);
  const original = (await login.json()).refreshToken as string;
  const attempts = await Promise.all(Array.from({ length: 6 }, () =>
    request.post('/api/auth/refresh', { data: { refreshToken: original } })));
  expect(attempts.filter(response => response.status() === 200)).toHaveLength(1);
  expect(attempts.filter(response => response.status() === 401)).toHaveLength(5);
  const successor = (await attempts.find(response => response.status() === 200)!.json()).refreshToken;
  const replay = await request.post('/api/auth/refresh', { data: { refreshToken: original } });
  expect(replay.status()).toBe(401);
  const valid = await request.post('/api/auth/refresh', { data: { refreshToken: successor } });
  expect(valid.status()).toBe(200);
});

test('persistent cookie restores the session after closing and reopening the browser profile', async ({ playwright }, testInfo) => {
  const profile = testInfo.outputPath('browser-profile');
  const options = { headless: true, baseURL: process.env.E2E_BASE_URL || 'http://localhost:18086' };
  let persistent = await playwright.chromium.launchPersistentContext(profile, options);
  try {
    await login(await persistent.newPage());
    await persistent.close();
    persistent = await playwright.chromium.launchPersistentContext(profile, options);
    const restored = await persistent.newPage();
    await restored.goto('/animals');
    await expect(restored.getByRole('heading', { name: 'Animales', exact: true })).toBeVisible();
    await restored.getByRole('button', { name: 'Salir', exact: true }).click();
    await expect(restored.getByRole('heading', { name: 'Inicia sesión' })).toBeVisible();
  } finally { await persistent.close(); }
});

test('private animal photographs are fetched with authorization and remain inaccessible anonymously', async ({ page, request, playwright }) => {
  const response = await request.post('/api/auth/login', { data: { username, password } });
  const token = (await response.json()).accessToken;
  const headers = { Authorization: 'Bearer ' + token };
  const animals = await request.get('/api/animals/page?search=DEMO-001', { headers });
  const animalId = (await animals.json()).items[0].id;
  const upload = await request.post('/api/animals/' + animalId + '/photo', {
    headers,
    multipart: { file: {
      name: 'phase4-test.png', mimeType: 'image/png',
      buffer: Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+j9S8AAAAASUVORK5CYII=', 'base64'),
    } },
  });
  expect(upload.status()).toBe(200);
  const photo = await upload.json();
  try {
    const anonymous = await playwright.request.newContext({ baseURL: process.env.E2E_BASE_URL || 'http://localhost:18086' });
    try { expect((await anonymous.get(photo.url)).status()).toBe(401); }
    finally { await anonymous.dispose(); }
    await login(page);
    await page.goto('/animals/' + animalId);
    const image = page.getByAltText('Fotografía de DEMO-001').first();
    await expect(image).toBeVisible();
    await expect(image).toHaveAttribute('src', /^blob:/);
    expect(await image.evaluate((element: HTMLImageElement) => element.naturalWidth)).toBe(1);
  } finally {
    const photoId = photo.url.split('/photos/')[1].split('/')[0];
    expect((await request.delete('/api/animals/' + animalId + '/photos/' + photoId, { headers })).status()).toBe(204);
  }
});
