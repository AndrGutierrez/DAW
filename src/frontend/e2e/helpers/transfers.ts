import { expect } from '@playwright/test';
import type { Page } from '@playwright/test';

export async function chooseTransferOrigin(page: Page, source = 'all') {
  await page.getByLabel('Potrero de origen', { exact: true }).selectOption(source);
  await page.getByRole('button', { name: 'Elegir animales', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Reúne los animales' })).toBeVisible();
}
export async function chooseTransferDestination(page: Page) {
  await page.getByRole('button', { name: 'Elegir destino', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Elige el destino', exact: true })).toBeVisible();
}
export async function reviewTransfer(page: Page) {
  await page.getByRole('button', { name: 'Revisar traslado', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Confirmar traslado', exact: true })).toBeVisible();
}
