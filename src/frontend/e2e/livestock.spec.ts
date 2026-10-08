import { test, expect } from '@playwright/test';
import type { APIRequestContext, Page } from '@playwright/test';
import { randomUUID } from 'node:crypto';

const username = process.env.E2E_ADMIN_USERNAME!;
const password = process.env.E2E_ADMIN_PASSWORD!;
let headers: Record<string, string>;
let farmId: string, speciesId: string, prefix: string;
let created: string[];

async function login(page: Page) {
  await page.goto('/animals');
  await page.getByLabel('Usuario o correo').fill(username);
  await page.getByLabel('Contraseña', { exact: true }).fill(password);
  await page.getByRole('button', { name: 'Entrar a mi finca' }).click();
  await expect(page.getByRole('heading', { name: 'Animales', exact: true })).toBeVisible();
}
async function create(request: APIRequestContext, suffix: string, extra: Record<string, unknown> = {}) {
  const response = await request.post('/api/animals', { headers, data: {
    farmId, speciesId, internalTag: prefix + '-' + suffix, sex: 'Female', purpose: 'Meat', ...extra,
  } });
  expect(response.status()).toBe(201);
  const result = await response.json();
  created.push(result.id);
  return result.id as string;
}
test.beforeEach(async ({ request }, testInfo) => {
  created = []; prefix = ('P4-' + testInfo.project.name[0] + '-' + randomUUID().slice(0, 8)).toUpperCase();
  const response = await request.post('/api/auth/login', { data: { username, password } });
  expect(response.status()).toBe(200); headers = { Authorization: 'Bearer ' + (await response.json()).accessToken };
  farmId = (await (await request.get('/api/farms', { headers })).json()).find((item: { data: { code: string } }) => item.data.code === 'DEMO').id;
  speciesId = (await (await request.get('/api/species', { headers })).json()).find((item: { data: { code: string } }) => item.data.code === 'BO').id;
});
test.afterEach(async ({ request }) => {
  for (const id of [...created].reverse()) {
    const growth = await request.get('/api/animals/' + id + '/growth?pageSize=100', { headers });
    if (growth.ok()) for (const record of (await growth.json()).records)
      expect((await request.delete('/api/weights/' + record.id, { headers })).status()).toBe(204);
    const detail = await request.get('/api/animals/' + id, { headers });
    if (detail.ok()) for (const photo of (await detail.json()).photos)
      expect((await request.delete('/api/animals/' + id + '/photos/' + photo.id, { headers })).status()).toBe(204);
    expect((await request.delete('/api/animals/' + id, { headers })).status()).toBe(204);
  }
});

test('create and edit preserve animal identification, location, genealogy and notes', async ({ page, request }) => {
  const mother = await create(request, 'MOTHER', { birthDate: '2020-01-01' });
  await login(page);
  await page.getByRole('link', { name: 'Registrar animal', exact: true }).click();
  await page.getByRole('button', { name: 'Guardar animal' }).click();
  await expect(page.getByLabel('Especie *', { exact: true })).toHaveAttribute('aria-invalid', 'true');
  await page.getByLabel('Finca *', { exact: true }).selectOption(farmId);
  await page.getByLabel('Especie *', { exact: true }).selectOption(speciesId);
  await page.getByLabel('Arete interno *', { exact: true }).fill(prefix + '-CHILD');
  await page.getByLabel('Nombre', { exact: true }).fill('Luna de prueba');
  await page.getByLabel('Identificación oficial', { exact: true }).fill(prefix + '-OFFICIAL');
  await page.getByLabel('RFID', { exact: true }).fill(prefix + '-RFID');
  await page.getByLabel('Fecha de nacimiento').fill('2025-01-01');
  await page.getByLabel('Peso al nacer (kg)').fill('32.25');
  await page.getByLabel('Observaciones', { exact: true }).fill('Preserve this information.');
  await page.getByRole('button', { name: 'Madre', exact: true }).click();
  await page.getByLabel('Buscar madre').fill(prefix + '-MOTHER');
  await page.getByRole('dialog', { name: 'Seleccionar madre' }).getByRole('button', { name: prefix + '-MOTHER', exact: true }).click();
  const lots = await (await request.get('/api/lots', { headers })).json();
  const lot = lots.find((item: { data: { farmId: string; speciesId: string; isActive: boolean } }) => item.data.farmId === farmId && item.data.speciesId === speciesId && item.data.isActive);
  await page.getByLabel('Lote', { exact: true }).selectOption(lot.id);
  await expect(page.getByLabel('Potrero', { exact: true })).toHaveValue('');
  if (lot.data.paddockId) await page.getByLabel('Potrero', { exact: true }).selectOption(lot.data.paddockId);
  const createdResponse = page.waitForResponse(response => response.url().endsWith('/api/animals') && response.request().method() === 'POST');
  await page.getByRole('button', { name: 'Guardar animal' }).click();
  const newAnimal = await (await createdResponse).json(); created.push(newAnimal.id);
  await expect(page.getByRole('heading', { name: 'Luna de prueba', exact: true })).toBeVisible();
  await page.getByRole('link', { name: 'Editar ficha' }).click();
  await expect(page.getByLabel('Finca *', { exact: true })).toBeDisabled();
  await expect(page.getByLabel('Observaciones', { exact: true })).toHaveValue('Preserve this information.');
  await page.getByLabel('Nombre', { exact: true }).fill('Luna actualizada');
  await page.getByRole('button', { name: 'Guardar animal' }).click();
  await expect(page.getByRole('heading', { name: 'Luna actualizada', exact: true })).toBeVisible();
  const saved = await (await request.get('/api/animals/' + newAnimal.id, { headers })).json();
  expect(saved.officialId).toBe(prefix + '-OFFICIAL'); expect(saved.rfid).toBe(prefix + '-RFID');
  expect(saved.damId).toBe(mother); expect(saved.notes).toBe('Preserve this information.');
  expect(saved.lotId).toBe(lot.id); expect(saved.paddockId).toBe(lot.data.paddockId); expect(saved.birthWeightKg).toBe(32.25);
  expect(await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth)).toBe(false);
});

test('photo upload compresses a large original and removal requires confirmation', async ({ page, request }) => {
  const id = await create(request, 'PHOTO');
  await login(page); await page.goto('/animals/' + id + '?tab=photos');
  const encoded = await page.evaluate(() => {
    const canvas = document.createElement('canvas'); canvas.width = 2400; canvas.height = 1600;
    const context = canvas.getContext('2d')!; const image = context.createImageData(canvas.width, canvas.height);
    let state = 123456789;
    for (let i = 0; i < image.data.length; i += 4) {
      state ^= state << 13; state ^= state >>> 17; state ^= state << 5;
      image.data[i] = state & 255; image.data[i + 1] = (state >>> 8) & 255; image.data[i + 2] = (state >>> 16) & 255; image.data[i + 3] = 255;
    }
    context.putImageData(image, 0, 0); return canvas.toDataURL('image/png').split(',')[1];
  });
  const original = Buffer.from(encoded, 'base64');
  expect(original.length).toBeGreaterThan(5 * 1024 * 1024);
  await page.getByLabel('Añadir fotografía').setInputFiles({ name: 'original.png', mimeType: 'image/png', buffer: original });
  await expect(page.getByAltText('Vista previa de la fotografía preparada')).toBeVisible();
  const uploaded = page.waitForResponse(response => response.url().endsWith('/photo') && response.request().method() === 'POST');
  await page.getByRole('button', { name: 'Subir fotografía' }).click();
  const photo = await (await uploaded).json();
  const content = await request.get(photo.url, { headers }); const compressed = await content.body();
  expect(compressed.length).toBeLessThan(original.length); expect(compressed.length).toBeLessThan(5 * 1024 * 1024);
  expect(compressed.subarray(0, 2).toString('hex')).toBe('ffd8');
  const image = page.getByAltText('Fotografía de ' + prefix + '-PHOTO').first();
  await expect(image).toBeVisible(); expect(await image.evaluate((element: HTMLImageElement) => element.naturalWidth)).toBe(1920);
  await page.getByRole('button', { name: 'Eliminar fotografía' }).click();
  await expect(page.getByRole('dialog')).toBeVisible();
  await page.getByRole('button', { name: 'Cancelar', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Eliminar fotografía' })).toBeVisible();
  await page.getByRole('button', { name: 'Eliminar fotografía' }).click();
  await page.getByRole('button', { name: 'Confirmar', exact: true }).click();
  await expect(page.getByText('Aún no hay fotografías registradas.')).toBeVisible();
});

test('growth uses recorded weights, same-day selection and an explicit gain target', async ({ page, request }) => {
  const id = await create(request, 'GROWTH');
  for (const [date, weightKg] of [['2026-01-01', 100], ['2026-01-11', 110], ['2026-01-11', 112]] as const) {
    expect((await request.post('/api/weights', { headers, data: { farmId, animalId: id, date, weightKg } })).status()).toBe(201);
  }
  await login(page); await page.goto('/animals/' + id + '?tab=growth');
  await expect(page.getByRole('group', { name: 'Curva de peso del animal', exact: true })).toBeVisible();
  await expect(page.locator('.chart-point')).toHaveCount(2);
  await page.getByRole('button', { name: 'Pesaje siguiente en la curva' }).click();
  await expect(page.locator('.chart-inspector')).toContainText('GDP: 1,2 kg/día');
  await expect(page.getByRole('table', { name: 'Historial completo de pesajes' })).toContainText('Otro pesaje del mismo día');
  await page.getByRole('button', { name: 'GDP', exact: true }).click();
  await expect(page.locator('.chart-point')).toHaveCount(1);
  await page.getByLabel('Objetivo de GDP (kg/día)').fill('2');
  await expect(page.getByText(/La última GDP está por debajo/)).toBeVisible();
  await page.getByLabel('Objetivo de GDP (kg/día)').fill('');
  await expect(page.getByText(/La última GDP está por debajo/)).not.toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth)).toBe(false);
});

test('consecutive weighing saves two animals without reloading and keeps a failed submission', async ({ page, request }) => {
  const first = await create(request, 'A'); const second = await create(request, 'B');
  await login(page); await page.getByRole('link', { name: 'Pesaje consecutivo', exact: true }).click();
  await page.getByLabel('Finca', { exact: true }).selectOption(farmId);
  await page.getByLabel('Buscar animales para pesar').fill(prefix);
  await page.getByRole('button', { name: 'Buscar', exact: true }).click();
  await page.getByRole('checkbox', { name: 'Seleccionar ' + prefix + '-A' }).check();
  await page.getByRole('checkbox', { name: 'Seleccionar ' + prefix + '-B' }).check();
  await page.getByRole('button', { name: 'Comenzar pesaje' }).click();
  let documents = 0; page.on('request', request => { if (request.isNavigationRequest() && request.resourceType() === 'document') documents++; });
  await page.getByLabel('Peso vivo (kg) *', { exact: true }).fill('-1');
  await page.getByRole('button', { name: 'Guardar y continuar' }).click();
  await expect(page.getByText('Ingresa un peso mayor que cero, con máximo dos decimales.')).toBeVisible();
  await page.getByLabel('Peso vivo (kg) *', { exact: true }).fill('130.25');
  await page.getByRole('button', { name: 'Guardar y continuar' }).click();
  await expect(page.getByRole('heading', { name: prefix + '-B', exact: true })).toBeVisible();
  await page.getByLabel('Peso vivo (kg) *', { exact: true }).fill('140');
  await page.getByRole('button', { name: 'Guardar y continuar' }).click();
  await expect(page.getByRole('heading', { name: 'Pesaje completado' })).toBeVisible();
  expect(documents).toBe(0);
  for (const [id, expected] of [[first, 130.25], [second, 140]] as const) {
    const result = await (await request.get('/api/animals/' + id + '/growth', { headers })).json();
    expect(result.records).toHaveLength(1); expect(result.records[0].weightKg).toBe(expected);
  }
});

test('lost response after a successful write can be retried without creating a second weight', async ({ page, request }) => {
  const id = await create(request, 'NETWORK');
  await login(page); await page.goto('/animals/' + id);
  await page.getByRole('button', { name: 'Registrar peso', exact: true }).click();
  await page.getByLabel('Peso vivo (kg) *', { exact: true }).fill('145');
  let intercepted = false;
  await page.route('**/api/animals/' + id + '/weights', async route => {
    if (!intercepted) { intercepted = true; const response = await route.fetch(); expect(response.status()).toBe(201); await route.abort('failed'); }
    else await route.continue();
  });
  await page.getByRole('button', { name: 'Guardar pesaje', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Reintentar y confirmar pesaje' })).toBeVisible();
  await expect(page.getByLabel('Peso vivo (kg) *', { exact: true })).toBeDisabled();
  const replay = page.waitForResponse(response => response.url().endsWith('/weights') && response.request().method() === 'POST');
  await page.getByRole('button', { name: 'Reintentar y confirmar pesaje' }).click();
  expect((await replay).status()).toBe(200);
  await expect(page.getByText('Pesaje confirmado; ya estaba guardado.')).toBeVisible();
  const growth = await (await request.get('/api/animals/' + id + '/growth', { headers })).json();
  expect(growth.records).toHaveLength(1);
});

test('duplicate tag returns a useful message and preserves form values', async ({ page, request }) => {
  await create(request, 'DUPLICATE');
  await login(page); await page.goto('/animals/new');
  await page.getByLabel('Finca *', { exact: true }).selectOption(farmId);
  await page.getByLabel('Especie *', { exact: true }).selectOption(speciesId);
  await page.getByLabel('Arete interno *', { exact: true }).fill(prefix + '-DUPLICATE');
  await page.getByLabel('Nombre', { exact: true }).fill('Keep my input');
  await page.getByRole('button', { name: 'Guardar animal' }).click();
  await expect(page.getByRole('alert').first()).toBeVisible();
  await expect(page.getByLabel('Nombre', { exact: true })).toHaveValue('Keep my input');
  await page.getByRole('button', { name: 'Cancelar', exact: true }).click();
  await expect(page.getByRole('dialog')).toBeVisible();
  await page.getByRole('button', { name: 'Confirmar', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Animales', exact: true })).toBeVisible();
});

test('concurrent PostgreSQL submissions leave exactly one weight and one creation', async ({ request }) => {
  const id = await create(request, 'CONCURRENT');
  const body = { submissionId: randomUUID(), date: new Date().toISOString().slice(0, 10), weightKg: 150 };
  const responses = await Promise.all(Array.from({ length: 6 }, () => request.post('/api/animals/' + id + '/weights', { headers, data: body })));
  expect(responses.filter(response => response.status() === 201)).toHaveLength(1);
  expect(responses.every(response => [201, 200, 409].includes(response.status()))).toBe(true);
  const confirmed = await request.post('/api/animals/' + id + '/weights', { headers, data: body });
  expect(confirmed.status()).toBe(200); expect((await confirmed.json()).replayed).toBe(true);
  const growth = await (await request.get('/api/animals/' + id + '/growth', { headers })).json();
  expect(growth.records).toHaveLength(1);
});

test('an animal that becomes inactive can be explicitly omitted from the queue', async ({ page, request }) => {
  const id = await create(request, 'INACTIVE');
  await login(page); await page.goto('/weighing');
  await page.getByLabel('Finca', { exact: true }).selectOption(farmId);
  await page.getByLabel('Buscar animales para pesar').fill(prefix);
  await page.getByRole('button', { name: 'Buscar', exact: true }).click();
  await page.getByRole('checkbox', { name: 'Seleccionar ' + prefix + '-INACTIVE' }).check();
  await page.getByRole('button', { name: 'Comenzar pesaje' }).click();
  const update = await request.put('/api/animals/' + id, { headers, data: { farmId, speciesId, internalTag: prefix + '-INACTIVE', sex: 'Female', purpose: 'Meat', status: 'Sold' } });
  expect(update.status()).toBe(200);
  await page.getByLabel('Peso vivo (kg) *', { exact: true }).fill('160');
  await page.getByRole('button', { name: 'Guardar y continuar' }).click();
  await expect(page.getByText('Este animal ya no está activo. Actualiza la lista antes de pesarlo.').first()).toBeVisible();
  await page.getByRole('button', { name: 'Omitir este animal' }).click();
  await page.getByRole('button', { name: 'Confirmar', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Pesaje completado' })).toBeVisible();
  await expect(page.getByText('0 registros confirmados en el servidor. 1 animales omitidos sin registrar un peso.')).toBeVisible();
  const growth = await (await request.get('/api/animals/' + id + '/growth', { headers })).json();
  expect(growth.records).toHaveLength(0);
});

test('employee can record weights while photo deletion remains restricted', async ({ page, request }) => {
  const employeeUsername = process.env.E2E_EMPLOYEE_USERNAME;
  const employeePassword = process.env.E2E_EMPLOYEE_PASSWORD;
  if (!employeeUsername || !employeePassword) throw new Error('Set isolated E2E employee credentials.');
  const id = await create(request, 'EMPLOYEE');
  const photo = await request.post('/api/animals/' + id + '/photo', { headers, multipart: { file: {
    name: 'employee-photo.png', mimeType: 'image/png',
    buffer: Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+j9S8AAAAASUVORK5CYII=', 'base64'),
  } } });
  expect(photo.status()).toBe(200);
  const photoData = await photo.json();
  const employee = await request.post('/api/auth/login', { data: { username: employeeUsername, password: employeePassword } });
  const employeeHeaders = { Authorization: 'Bearer ' + (await employee.json()).accessToken };
  const photoId = photoData.url.split('/photos/')[1].split('/')[0];
  expect((await request.delete('/api/animals/' + id + '/photos/' + photoId, { headers: employeeHeaders })).status()).toBe(403);
  await page.goto('/login'); await page.getByLabel('Usuario o correo').fill(employeeUsername);
  await page.getByLabel('Contraseña', { exact: true }).fill(employeePassword);
  await page.getByRole('button', { name: 'Entrar a mi finca' }).click();
  await expect(page.getByRole('heading', { name: 'Animales', exact: true })).toBeVisible();
  await page.goto('/animals/' + id + '?tab=photos');
  await expect(page.getByRole('link', { name: 'Editar ficha' })).toBeVisible();
  await expect(page.getByAltText('Fotografía de ' + prefix + '-EMPLOYEE').first()).toBeVisible();
  await expect(page.getByRole('button', { name: 'Eliminar fotografía' })).toHaveCount(0);
  await page.getByRole('button', { name: 'Registrar peso', exact: true }).click();
  await page.getByLabel('Peso vivo (kg) *', { exact: true }).fill('165');
  await page.getByRole('button', { name: 'Guardar pesaje', exact: true }).click();
  await expect(page.getByText('Pesaje guardado.', { exact: true })).toBeVisible();
  const growth = await (await request.get('/api/animals/' + id + '/growth', { headers })).json();
  expect(growth.records).toHaveLength(1);
});

test('editing birth date cannot invalidate an existing weighing', async ({ page, request }) => {
  const id = await create(request, 'BIRTH', { birthDate: '2020-01-01' });
  expect((await request.post('/api/weights', { headers, data: { farmId, animalId: id, date: '2026-01-10', weightKg: 100 } })).status()).toBe(201);
  await login(page); await page.goto('/animals/' + id + '/edit');
  await page.getByLabel('Fecha de nacimiento').fill('2026-01-11');
  await page.getByRole('button', { name: 'Guardar animal' }).click();
  await expect(page.getByText('La fecha de nacimiento no puede ser posterior a un pesaje ya registrado.').first()).toBeVisible();
  await expect(page.getByLabel('Fecha de nacimiento')).toHaveValue('2026-01-11');
  const preserved = await (await request.get('/api/animals/' + id, { headers })).json();
  expect(preserved.birthDate).toBe('2020-01-01');
});

test('weight history requests the next page from the server and preserves the GDP view', async ({ page, request }) => {
  const id = await create(request, 'PAGING');
  for (let index = 1; index <= 21; index++) {
    expect((await request.post('/api/weights', { headers, data: { farmId, animalId: id, date: '2026-01-' + String(index).padStart(2, '0'), weightKg: 100 + index } })).status()).toBe(201);
  }
  await login(page); await page.goto('/animals/' + id + '?tab=growth');
  await expect(page.locator('#growth tbody tr')).toHaveCount(20);
  await page.getByRole('button', { name: 'GDP', exact: true }).click();
  const response = page.waitForResponse(response => response.url().includes('/growth?page=2&pageSize=20'));
  await page.locator('#growth').getByRole('button', { name: 'Siguiente', exact: true }).click();
  const result = await (await response).json();
  expect(result.total).toBe(21); expect(result.records).toHaveLength(1);
  await expect(page.locator('#growth tbody tr')).toHaveCount(1);
  await expect(page.getByRole('button', { name: 'GDP', exact: true })).toHaveAttribute('aria-pressed', 'true');
  await expect(page.locator('#growth').getByText('Página 2 de 2')).toBeVisible();
});
