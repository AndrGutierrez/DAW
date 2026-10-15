import { chooseTransferOrigin, chooseTransferDestination, reviewTransfer } from './helpers/transfers';
import { test, expect } from '@playwright/test';
import type { APIRequestContext, Page } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { randomUUID, createHash } from 'node:crypto';
import { mkdir } from 'node:fs/promises';
import { join } from 'node:path';

test.skip(process.env.E2E_ISOLATED_DATABASE !== '1', 'Viewer mutation fixtures require the disposable database.');
let headers: Record<string, string>, farm: string, source: string, target: string, full: string;
let paddocks: string[], animals: string[];

test.beforeEach(async ({ request }) => {
  paddocks = []; animals = [];
  const session = await request.post('/api/auth/login', { data: { username: process.env.E2E_ADMIN_USERNAME, password: process.env.E2E_ADMIN_PASSWORD } });
  expect(session.status()).toBe(200);
  headers = { Authorization: 'Bearer ' + (await session.json()).accessToken };
  async function create(path: string, data: unknown) {
    const response = await request.post(path, { headers, data }); expect(response.status()).toBe(201); return (await response.json()).id as string;
  }
  const prefix = 'VIEW-' + randomUUID().slice(0, 8);
  farm = await create('/api/farms', { name: prefix, code: prefix, isActive: true });
  const species = (await (await request.get('/api/species', { headers })).json()).find((item: { data: { code: string } }) => item.data.code === 'BO').id;
  async function paddock(name: string, capacity: number, x?: number, y?: number) {
    const id = await create('/api/paddocks', { farmId: farm, name, capacity, isActive: true, ...(x !== undefined ? { mapX: x, mapY: y, mapWidth: 30, mapHeight: 35 } : {}) });
    paddocks.push(id); return id;
  }
  source = await paddock('Norte', 10, 5, 5); target = await paddock('Sur', 4, 45, 5); full = await paddock('Centro', 1, 5, 50);
  await paddock('Loma', 8);
  for (const [tag, location] of [['A', source], ['B', source], ['C', full]]) {
    animals.push(await create('/api/animals', { farmId: farm, speciesId: species, internalTag: prefix + '-' + tag, sex: 'Female', purpose: 'Milk', paddockId: location }));
  }
});

test.afterEach(async ({ request }) => {
  for (const id of animals) expect((await request.delete('/api/animals/' + id, { headers })).status()).toBe(204);
  for (const id of paddocks) expect((await request.delete('/api/paddocks/' + id, { headers })).status()).toBe(204);
  if (farm) expect((await request.delete('/api/farms/' + farm, { headers })).status()).toBe(204);
});

async function login(page: Page, path = '/paddocks?farm=' + farm) {
  await page.goto('/login');
  await page.getByLabel('Usuario o correo').fill(process.env.E2E_ADMIN_USERNAME!);
  await page.getByLabel('Contraseña', { exact: true }).fill(process.env.E2E_ADMIN_PASSWORD!);
  await page.getByRole('button', { name: 'Entrar a mi finca' }).click();
  await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toBeVisible();
  await page.goto(path);
  if (path === '/transfers') await page.getByLabel('Finca', { exact: true }).selectOption(farm);
}
async function ready(page: Page) {
  await expect(page.locator('.farm-viewer')).toHaveAttribute('data-scene-state', 'ready', { timeout: 15000 });
  await expect(page.locator('.farm-canvas-host canvas')).toBeVisible();
}
async function selectSourceAnimals(page: Page) {
  await chooseTransferOrigin(page, source);
  for (const suffix of ['A', 'B']) await page.getByRole('checkbox', { name: new RegExp('-' + suffix + '$') }).check();
  await chooseTransferDestination(page);
  await page.getByRole('group', { name: 'Plano interactivo de potreros' }).getByRole('button', { name: /^Sur:/ }).click();
  await page.getByLabel('Motivo del traslado *').fill('Visual rotation');
}
async function location(request: APIRequestContext, id: string) {
  return (await (await request.get('/api/animals/' + id, { headers })).json()).paddockId;
}

test('renders the farm, supports zoom and keyboard, and pauses actual cow animation', async ({ page }, info) => {
  const errors: string[] = []; page.on('pageerror', error => errors.push(error.message));
  await login(page); await ready(page);
  const response = await page.request.get('/');
  expect(response.headers()['content-security-policy']).toContain("script-src 'self'");
  expect(response.headers()['content-security-policy']).not.toContain("'unsafe-eval'");
  const canvas = page.locator('.farm-canvas-host canvas');
  await page.getByRole('button', { name: 'Pausar animación' }).click();
  const hash = async () => createHash('sha256').update(await canvas.screenshot()).digest('hex');
  const paused = await hash(); await page.waitForTimeout(250); expect(await hash()).toBe(paused);
  await page.getByRole('button', { name: 'Acercar el plano' }).click(); expect(await hash()).not.toBe(paused);
  const region = page.getByRole('region', { name: 'Vista isométrica de la finca' });
  await region.focus(); await page.keyboard.press('ArrowRight'); await page.keyboard.press('r');
  await page.getByRole('button', { name: 'Activar animación' }).click();
  const playing = await hash(); await expect.poll(hash, { timeout: 10000 }).not.toBe(playing);
  await page.getByRole('button', { name: 'Pausar animación' }).click();
  await expect(page.getByText(/distribución automática/)).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  await expect(page.getByRole('group', { name: 'Plano interactivo de potreros' }).getByRole('button', { name: /^Norte: 2 animales;/ })).toBeVisible();
  const axe = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa']).analyze(); expect(axe.violations).toEqual([]);
  if (process.env.QUALITY_EVIDENCE_DIR) {
    await mkdir(process.env.QUALITY_EVIDENCE_DIR, { recursive: true });
    await page.locator('.farm-viewer').screenshot({ path: join(process.env.QUALITY_EVIDENCE_DIR, 'farm-' + info.project.name + '.png') });
  }
  expect(errors).toEqual([]);
});

test('creates a paddock using the visual layout and persists its management parameters', async ({ page, request }, info) => {
  await login(page); await ready(page);
  await page.getByRole('button', { name: 'Registrar potrero', exact: true }).click();
  const editor = page.getByRole('region', { name: 'Registrar potrero', exact: true });
  await expect(editor.getByLabel('Finca del potrero *')).toHaveValue(farm);
  await editor.getByLabel('Nombre del potrero *').fill('Pradera Este');
  await editor.getByLabel('Permanencia máxima (días)').fill('10');
  await editor.getByLabel('Capacidad máxima (animales)').fill('12');
  await editor.getByRole('button', { name: 'Mover a la derecha' }).click();
  await editor.getByRole('button', { name: 'Ampliar potrero' }).click();
  const layout = editor.getByRole('group', { name: 'Ubicación del potrero en el plano' });
  const coordinateInputs = editor.locator('.paddock-plan-editor input');
  const expectValidCoordinates = async () => {
    const validity = await coordinateInputs.evaluateAll(elements => elements.map(element => {
      const input = element as HTMLInputElement; return { valid: input.validity.valid, stepMismatch: input.validity.stepMismatch };
    }));
    expect(validity).toHaveLength(4);
    expect(validity.every(input => input.valid && !input.stepMismatch)).toBe(true);
  };
  const point = await layout.evaluate(element => {
    const svg = element as SVGSVGElement, target = svg.createSVGPoint();
    target.x = (84.411865234375 - 19.921247209821431) * 7;
    target.y = (84.411865234375 + 19.921247209821431) * 3.5;
    const screen = target.matrixTransform(svg.getScreenCTM()!); return { x: screen.x, y: screen.y };
  });
  if (info.project.name === 'mobile') await page.touchscreen.tap(point.x, point.y);
  else await page.mouse.click(point.x, point.y);
  await expectValidCoordinates();
  const center = await layout.locator('.layout-draft').evaluate(element => {
    const polygon = element as SVGPolygonElement, box = polygon.getBBox(), point = polygon.ownerSVGElement!.createSVGPoint();
    point.x = box.x + box.width / 2; point.y = box.y + box.height / 2;
    const screen = point.matrixTransform(polygon.getScreenCTM()!); return { x: screen.x, y: screen.y };
  });
  await page.mouse.move(center.x, center.y); await page.mouse.down();
  await page.mouse.move(center.x - 23.7, center.y + 9.3, { steps: 5 }); await page.mouse.up();
  await expectValidCoordinates();
  expect(await page.evaluate(() => window.getSelection()?.toString() || '')).toBe('');
  const handle = await layout.locator('.layout-resize').boundingBox(); expect(handle).not.toBeNull();
  await page.mouse.move(handle!.x + handle!.width / 2, handle!.y + handle!.height / 2); await page.mouse.down();
  await page.mouse.move(handle!.x + handle!.width / 2 + 13.2, handle!.y + handle!.height / 2 + 8.7, { steps: 5 }); await page.mouse.up();
  await expectValidCoordinates();
  const expectedCoordinates = await coordinateInputs.evaluateAll(elements => Object.fromEntries(elements.map(element => [(element as HTMLInputElement).name, Number((element as HTMLInputElement).value)])));
  const axe = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa']).analyze(); expect(axe.violations).toEqual([]);
  if (process.env.QUALITY_EVIDENCE_DIR) await editor.screenshot({ path: join(process.env.QUALITY_EVIDENCE_DIR, 'workflow-create-' + info.project.name + '.png') });
  const saved = page.waitForResponse(reply => reply.url().endsWith('/api/paddocks') && reply.request().method() === 'POST');
  await editor.getByRole('button', { name: 'Guardar potrero', exact: true }).click();
  const response = await saved; expect(response.status()).toBe(201); const id = (await response.json()).id; paddocks.push(id);
  const created = (await (await request.get('/api/paddocks/' + id, { headers })).json()).data;
  expect(created).toMatchObject({ farmId: farm, name: 'Pradera Este', capacity: 12, maxStayDays: 10, ...expectedCoordinates });
  expect(created.mapX + created.mapWidth).toBeLessThanOrEqual(100); expect(created.mapY + created.mapHeight).toBeLessThanOrEqual(100);
  await expect(page.getByRole('status').filter({ hasText: 'Potrero guardado.' })).toBeVisible();
});

test('guides a source group through four stages without sending before final review', async ({ page, request }, info) => {
  test.setTimeout(60_000);
  await login(page, '/transfers'); await ready(page);
  let posts = 0; page.on('request', request => { if (request.method() === 'POST' && request.url().endsWith('/api/animal-movements/batch')) posts++; });
  const map = page.getByRole('group', { name: 'Plano interactivo de potreros' });
  const evidence = async (step: string) => {
    const axe = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa']).analyze(); expect(axe.violations).toEqual([]);
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    if (process.env.QUALITY_EVIDENCE_DIR) await page.screenshot({ path: join(process.env.QUALITY_EVIDENCE_DIR, 'transfer-wizard-' + step + '-' + info.project.name + '.png'), fullPage: true });
  };
  await map.getByRole('button', { name: /^Norte:/ }).click();
  await expect(page.getByLabel('Potrero de origen', { exact: true })).toHaveValue(source);
  await evidence('origin');
  await page.getByRole('button', { name: 'Elegir animales', exact: true }).click();
  await expect(page.getByRole('checkbox', { name: /-A$/ })).toBeVisible();
  await expect(page.getByRole('checkbox', { name: /-C$/ })).toHaveCount(0);
  await page.getByRole('checkbox', { name: /-A$/ }).check();
  await expect(page.getByRole('heading', { name: 'Reúne los animales' })).toBeVisible();
  await expect(page.getByLabel('Potrero de destino *')).toHaveCount(0);
  await page.getByRole('button', { name: 'Seleccionar todos los resultados (2)' }).click();
  await evidence('animals');
  await chooseTransferDestination(page);
  await map.getByRole('button', { name: /^Sur:/ }).click();
  await page.getByRole('button', { name: 'Descanso del potrero', exact: true }).click();
  await evidence('destination');
  await reviewTransfer(page);
  await evidence('review');
  await page.getByRole('button', { name: 'Atrás', exact: true }).click();
  await expect(page.getByLabel('Motivo del traslado *')).toHaveValue('Descanso del potrero');
  await expect(page.getByLabel('Potrero de destino *')).toHaveValue(target);
  await reviewTransfer(page); expect(posts).toBe(0);
  expect(await location(request, animals[0])).toBe(source);
  await page.getByRole('button', { name: 'Confirmar traslado', exact: true }).click();
  await expect(page.getByRole('heading', { name: '2 animales trasladados' })).toBeVisible();
  expect(posts).toBe(1);
  expect(await location(request, animals[0])).toBe(target); expect(await location(request, animals[1])).toBe(target);
  expect(await location(request, animals[2])).toBe(full);
});

test('shows rotation estimates from dated API snapshots while retaining the forage limitation', async ({ page }, info) => {
  // Only the response dates/window are controlled; this is a rendering fixture, not database history evidence.
  await page.route('**/api/paddocks/destinations?farmId=' + farm, async route => {
    const response = await route.fetch(), items = await response.json();
    const ago = (days: number) => new Date(Date.now() - days * 86400000).toISOString().slice(0, 10);
    await route.fulfill({ response, json: items.map((item: { id: string; data: object }) => item.id === source || item.id === full
      ? { ...item, data: { ...item.data, maxStayDays: 10 }, unknownArrivals: 0, oldestKnownArrival: ago(item.id === source ? 8 : 12) } : item) });
  });
  await login(page); await ready(page);
  const map = page.getByRole('group', { name: 'Plano interactivo de potreros' });
  await expect(map.getByRole('button', { name: /^Norte:/ })).toContainText('Rotación próxima');
  await expect(map.getByRole('button', { name: /^Centro:/ })).toContainText('Revisar rotación');
  await expect(map.getByRole('button', { name: /^Sur:/ })).toContainText('Sin pastoreo activo');
  await expect(page.getByText(/no una medición del pasto/)).toBeVisible();
  if (process.env.QUALITY_EVIDENCE_DIR) await page.locator('.farm-viewer').screenshot({ path: join(process.env.QUALITY_EVIDENCE_DIR, 'workflow-estimate-' + info.project.name + '.png') });
});

test('opens an individual transfer from its paddock and moves only that animal', async ({ page, request }) => {
  await login(page, '/transfers?farm=' + farm + '&source=' + source + '&animal=' + animals[0]); await ready(page);
  await expect(page.getByRole('heading', { name: 'Elige el destino', exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Cambiar grupo' }).click();
  await expect(page.getByRole('checkbox', { name: /-A$/ })).toBeChecked();
  await expect(page.getByRole('checkbox', { name: /-B$/ })).not.toBeChecked();
  await chooseTransferDestination(page);
  await page.getByRole('group', { name: 'Plano interactivo de potreros' }).getByRole('button', { name: /^Sur:/ }).click();
  await page.getByRole('button', { name: 'Reagrupación', exact: true }).click();
  await reviewTransfer(page);
  await page.getByRole('button', { name: 'Confirmar traslado', exact: true }).click();
  await expect(page.getByRole('heading', { name: '1 animal trasladado', exact: true })).toBeVisible();
  expect(await location(request, animals[0])).toBe(target); expect(await location(request, animals[1])).toBe(source);
});

test('animates a group only after receiving the server confirmation', async ({ page, request }) => {
  await login(page, '/transfers'); await ready(page); await selectSourceAnimals(page);
  const map = page.getByRole('group', { name: 'Plano interactivo de potreros' });
  await expect(map.getByRole('button', { name: /^Norte: 0 animales;/ })).toContainText('2 → 0');
  await expect(map.getByRole('button', { name: /^Sur: 2 animales;/ })).toContainText('0 → 2');
  expect(await location(request, animals[0])).toBe(source);
  let release!: () => void, posted = false;
  const gate = new Promise<void>(resolve => { release = resolve; });
  await page.route('**/api/animal-movements/batch', async route => {
    const response = await route.fetch(); expect(response.status()).toBe(201); posted = true;
    await gate; await route.fulfill({ response });
  });
  await reviewTransfer(page);
  await page.getByRole('button', { name: 'Confirmar traslado', exact: true }).click();
  await expect.poll(() => posted).toBe(true);
  await expect(page.locator('.farm-canvas-host')).toHaveAttribute('data-travel-state', 'idle');
  await expect(page.getByRole('button', { name: 'Destino', exact: true })).toBeDisabled();
  release();
  await expect(page.getByRole('heading', { name: '2 animales trasladados' })).toBeVisible();
  await expect(page.locator('.farm-canvas-host')).toHaveAttribute('data-travel-state', 'playing');
  expect(await location(request, animals[0])).toBe(target); expect(await location(request, animals[1])).toBe(target);
  await expect(map.getByRole('button', { name: /^Sur: 2 animales;/, includeHidden: true })).toContainText('2 / 4');
  await expect(page.locator('.farm-canvas-host')).toHaveAttribute('data-travel-state', 'idle', { timeout: 10000 });
});

test('preserves the group and does not animate when the server refuses a movement', async ({ page, request }) => {
  await login(page, '/transfers'); await ready(page); await selectSourceAnimals(page);
  await page.route('**/api/animal-movements/batch', route => route.fulfill({ status: 409, contentType: 'application/problem+json', body: JSON.stringify({ detail: 'The animal location changed. Refresh before moving it.' }) }));
  await reviewTransfer(page);
  await page.getByRole('button', { name: 'Confirmar traslado', exact: true }).click();
  await expect(page.locator('.transfer-review').getByRole('alert')).toContainText('Otro usuario cambió la ubicación');
  await expect(page.locator('.farm-canvas-host')).toHaveAttribute('data-travel-state', 'idle');
  await expect(page.getByRole('heading', { name: 'Trasladar 2 animales' })).toBeVisible();
  expect(await location(request, animals[0])).toBe(source);
});

test('honors reduced motion and retains the full-capacity guard', async ({ page, request }) => {
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await login(page, '/transfers'); await ready(page); await selectSourceAnimals(page);
  await expect(page.getByRole('button', { name: 'Activar animación' })).toBeVisible();
  const map = page.getByRole('group', { name: 'Plano interactivo de potreros' });
  await map.getByRole('button', { name: /^Centro:/ }).click();
  await expect(page.getByRole('button', { name: 'Revisar traslado', exact: true })).toBeDisabled();
  await map.getByRole('button', { name: /^Sur:/ }).click();
  await reviewTransfer(page);
  await page.getByRole('button', { name: 'Confirmar traslado', exact: true }).click();
  await expect(page.getByRole('heading', { name: '2 animales trasladados' })).toBeVisible();
  await expect(page.locator('.farm-canvas-host')).toHaveAttribute('data-travel-state', 'idle');
  expect(await location(request, animals[0])).toBe(target);
});

test('keeps selection and occupancy usable after the graphics module fails to load', async ({ page }) => {
  await page.route(/(renderer-[\w-]+\.js|paddock-scene\/renderer\.ts)/, route => route.abort('failed'));
  await login(page);
  await expect(page.locator('.farm-viewer')).toHaveAttribute('data-scene-state', 'fallback');
  await expect(page.getByText('Vista sencilla disponible.')).toBeVisible();
  await page.getByRole('group', { name: 'Plano interactivo de potreros' }).getByRole('button', { name: /^Norte: 2 animales;/ }).click();
  await expect(page.getByRole('table', { name: 'Animales presentes en el potrero' })).toBeVisible();
});

test('locks the review after a lost response and replays the same batch once', async ({ page, request }) => {
  await login(page, '/transfers'); await ready(page); await selectSourceAnimals(page); await reviewTransfer(page);
  const before = await Promise.all(animals.slice(0, 2).map(async id => (await (await request.get('/api/animals/' + id + '/movements', { headers })).json()).total));
  const submissions: unknown[] = [];
  await page.route('**/api/animal-movements/batch', async route => {
    submissions.push(route.request().postDataJSON());
    if (submissions.length === 1) { const response = await route.fetch(); expect(response.status()).toBe(201); await route.abort('failed'); }
    else await route.continue();
  });
  await page.getByRole('button', { name: 'Confirmar traslado', exact: true }).click();
  const retry = page.getByRole('button', { name: 'Reintentar y confirmar registro' }); await expect(retry).toBeVisible();
  await expect(page.getByRole('button', { name: 'Atrás', exact: true })).toBeDisabled();
  await expect(page.getByRole('button', { name: 'Animales', exact: true })).toBeDisabled();
  await retry.click(); await expect(page.getByRole('heading', { name: '2 animales trasladados' })).toBeVisible();
  expect(submissions).toHaveLength(2); expect(submissions[0]).toEqual(submissions[1]);
  for (let index = 0; index < 2; index++) {
    expect(await location(request, animals[index])).toBe(target);
    expect((await (await request.get('/api/animals/' + animals[index] + '/movements', { headers })).json()).total).toBe(before[index] + 1);
  }
});


test('reserves inactive geometry and preserves a small paddock when editing and saving', async ({ page, request }) => {
  const create = await request.post('/api/paddocks', { headers, data: { farmId: farm, name: 'Reserva inactiva', capacity: 2, isActive: false, mapX: 10, mapY: 70, mapWidth: .0001, mapHeight: .5 } });
  expect(create.status()).toBe(201); const id = (await create.json()).id; paddocks.push(id);
  await login(page); await ready(page);
  await page.getByRole('button', { name: 'Registrar potrero', exact: true }).click();
  const newEditor = page.getByRole('region', { name: 'Registrar potrero', exact: true });
  await expect(newEditor.getByRole('img', { name: 'Reserva inactiva: espacio ocupado · inactivo' })).toBeAttached();
  await newEditor.getByRole('button', { name: 'Cancelar', exact: true }).click();
  await page.getByLabel('Estado del potrero').selectOption('false');
  await page.getByRole('button', { name: 'Buscar', exact: true }).click();
  await page.getByRole('button', { name: 'Editar Reserva inactiva', exact: true }).click();
  const editor = page.getByRole('region', { name: 'Editar potrero', exact: true });
  await expect(editor.getByRole('img', { name: /Reserva inactiva: espacio ocupado/ })).toHaveCount(0);
  await editor.getByRole('button', { name: 'Mover a la derecha', exact: true }).click();
  const valid = await editor.locator('.paddock-plan-editor input').evaluateAll(elements => elements.every(element => (element as HTMLInputElement).validity.valid));
  expect(valid).toBe(true);
  const saved = page.waitForResponse(response => response.url().endsWith('/api/paddocks/' + id) && response.request().method() === 'PUT');
  await editor.getByRole('button', { name: 'Guardar potrero', exact: true }).click();
  expect((await saved).ok()).toBe(true);
  const persisted = await (await request.get('/api/paddocks/' + id, { headers })).json();
  expect(persisted.data).toMatchObject({ mapX: 12, mapY: 70, mapWidth: .0001, mapHeight: .5, isActive: false });
});


test('navigates between the running scene and transfers without a cleanup crash', async ({ page }) => {
  test.setTimeout(60_000);
  const errors: string[] = []; page.on('pageerror', error => errors.push(error.message));
  await login(page); await ready(page);
  for (let iteration = 0; iteration < 2; iteration++) {
    await page.getByRole('group', { name: 'Plano interactivo de potreros' }).getByRole('button', { name: /^Norte:/ }).click();
    await page.getByRole('link', { name: 'Trasladar desde este potrero', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Reúne los animales', exact: true })).toBeVisible();
    await expect(page.locator('.farm-viewer')).toHaveAttribute('data-scene-state', 'ready', { timeout: 15000 });
    await expect(page.locator('.farm-canvas-host canvas')).toBeAttached();
    await page.getByRole('link', { name: 'Potreros', exact: true }).click();
    await page.getByLabel('Finca', { exact: true }).selectOption(farm);
    await page.getByRole('button', { name: 'Buscar', exact: true }).click();
    await ready(page);
  }
  await expect(page.getByRole('heading', { name: 'Unexpected Application Error!' })).toHaveCount(0);
  expect(errors).toEqual([]);
});


test('keeps editor and saved map positions identical when adding a paddock on free ground', async ({ page, request }) => {
  await page.route(/(renderer-[\w-]+\.js|paddock-scene\/renderer\.ts)/, route => route.abort('failed'));
  await login(page);
  await expect(page.locator('.farm-viewer')).toHaveAttribute('data-scene-state', 'fallback');
  const automaticPolygon = () => page.locator('.farm-static-plan g').filter({ hasText: 'Loma' }).locator('polygon');
  const before = await automaticPolygon().getAttribute('points');
  expect(before).toBeTruthy();
  await page.getByRole('button', { name: 'Registrar potrero', exact: true }).click();
  const editor = page.getByRole('region', { name: 'Registrar potrero', exact: true });
  await expect(editor.getByRole('img', { name: 'Loma: espacio ocupado · posición automática' }).locator('.layout-peer')).toHaveAttribute('points', before!);
  await editor.getByLabel('Nombre del potrero *').fill('Pradera libre');
  await editor.getByText('Coordenadas avanzadas (opcional)', { exact: true }).click();
  for (const [label, value] of [['Posición X (%)', '70'], ['Posición Y (%)', '50'], ['Ancho (%)', '28'], ['Alto (%)', '34']]) await editor.getByLabel(label, { exact: true }).fill(value);
  const draft = await editor.locator('.layout-draft').getAttribute('points');
  const saved = page.waitForResponse(response => response.url().endsWith('/api/paddocks') && response.request().method() === 'POST');
  await editor.getByRole('button', { name: 'Guardar potrero', exact: true }).click();
  const response = await saved; expect(response.status()).toBe(201); const id = (await response.json()).id; paddocks.push(id);
  await expect(page.getByRole('status').filter({ hasText: 'Potrero guardado.' })).toBeVisible();
  await expect(automaticPolygon()).toHaveAttribute('points', before!);
  await expect(page.locator('.farm-static-plan g').filter({ hasText: 'Pradera libre' }).locator('polygon')).toHaveAttribute('points', draft!);
  await page.reload();
  await expect(automaticPolygon()).toHaveAttribute('points', before!);
  const catalog = await (await request.get('/api/paddocks', { headers })).json();
  expect(catalog.find((item: { data: { name: string; farmId: string } }) => item.data.farmId === farm && item.data.name === 'Loma').data.mapX).toBeNull();
});
