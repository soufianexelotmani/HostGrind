import { randomUUID } from 'node:crypto';
import { expect, test } from '@playwright/test';
import { demoPdf, login, screenshot } from './portfolio-fixtures';

const managerEmail = process.env['HOSTGRID_E2E_MANAGER_EMAIL'];
const managerPassword = process.env['HOSTGRID_E2E_MANAGER_PASSWORD'];
const recoveryCode = process.env['HOSTGRID_E2E_FILES_MANAGER_RECOVERY_CODE'];
const ownerEmail = process.env['HOSTGRID_E2E_FILES_OWNER_EMAIL'];
const ownerPassword = process.env['HOSTGRID_E2E_FILES_OWNER_PASSWORD'];
const ownerName = process.env['HOSTGRID_E2E_FILES_OWNER_NAME'];
const captures = process.env['HOSTGRID_FILES_SCREENSHOT_DIR'];

test('Manager uploads private media and the assigned Owner reads it without write controls', async ({ page, browser, baseURL }) => {
  test.skip(!managerEmail || !managerPassword || !recoveryCode || !ownerEmail || !ownerPassword || !ownerName,
    'Requires fictitious Manager and Owner accounts and an unused Manager MFA recovery code.');
  const name = 'Courtyard Studio ' + randomUUID().slice(0, 5);
  await page.setViewportSize({ width: 1440, height: 900 });
  await login(page, managerEmail!, managerPassword!, recoveryCode!);
  await page.waitForURL('**/manager');
  await page.goto('/manager/properties/new');
  await page.getByLabel('Name', { exact: true }).fill(name);
  await page.getByRole('combobox', { name: 'Owner', exact: true }).selectOption({ label: ownerName! });
  await page.getByRole('combobox', { name: 'Property type', exact: true }).selectOption({ label: 'Studio' });
  await page.getByLabel('City', { exact: true }).fill('Marrakech');
  await page.getByLabel('Address', { exact: true }).fill('10 Fictitious Courtyard');
  await page.getByRole('button', { name: 'Save', exact: true }).click();
  await expect(page.getByRole('heading', { name, exact: true })).toBeVisible();

  const photos = await page.evaluate(async () => {
    const image = new Image();
    image.src = '/brand/riad.jpg';
    await image.decode();
    const canvas = document.createElement('canvas');
    canvas.width = 720;
    canvas.height = 450;
    const context = canvas.getContext('2d')!;
    return [0, 0.29].map(offset => {
      context.drawImage(image, Math.floor(image.width * offset), 0, Math.floor(image.width * 0.7), image.height, 0, 0, 720, 450);
      return canvas.toDataURL('image/png').split(',')[1];
    });
  });
  const names = ['Courtyard.png', 'Riad details.png'];
  await page.getByRole('tab', { name: 'Photos' }).click();
  const photosPanel = page.getByRole('tabpanel', { name: 'Photos', exact: true });
  await expect(photosPanel).toBeVisible();
  const picker = photosPanel.locator('input[type=file]');
  await expect(picker).toBeEnabled();
  const uploadedPhoto = page.waitForResponse(response =>
    response.request().method() === 'POST' && new URL(response.url()).pathname.endsWith('/photos'));
  await picker.setInputFiles(photos.map((image, index) => ({
    name: names[index], mimeType: 'image/png', buffer: Buffer.from(image, 'base64'),
  })));
  expect((await uploadedPhoto).status()).toBe(201);
  await expect(page.getByRole('img', { name: names[0] })).toBeVisible();
  await expect(page.getByRole('img', { name: names[1] })).toBeVisible();
  await expect(page.getByRole('img', { name: names[1] })).toHaveJSProperty('complete', true);
  const second = page.locator('article').filter({ hasText: names[1] });
  const reordered = page.waitForResponse(response =>
    response.request().method() === 'PUT' && new URL(response.url()).pathname.endsWith('/photos/order'));
  await second.getByRole('button', { name: 'Set as cover' }).click();
  expect((await reordered).ok()).toBeTruthy();
  await expect(page.locator('#property-tab-content article').first()).toContainText(names[1]);
  await expect(page.getByText('Changes saved.', { exact: true })).toBeVisible();
  await screenshot(page, 'phase5-property-photos', captures);

  await page.getByRole('tab', { name: 'Documents' }).click();
  const filename = 'Property information.pdf';
  const documents = page.getByRole('tabpanel', { name: 'Documents', exact: true });
  await expect(documents.getByRole('heading', { name: 'Upload PDFs' })).toBeVisible();
  const pdfPicker = documents.locator('input[type=file][accept=".pdf"]');
  await expect(pdfPicker).toBeEnabled();
  const upload = page.waitForResponse(response =>
    response.request().method() === 'POST' && new URL(response.url()).pathname.endsWith('/documents'));
  await pdfPicker.setInputFiles({
    name: filename, mimeType: 'application/pdf', buffer: demoPdf(),
  });
  expect((await upload).status()).toBe(201);
  await expect(page.getByText(filename, { exact: true })).toBeVisible();
  const [file] = await Promise.all([
    page.waitForEvent('download'), page.getByRole('button', { name: 'Download', exact: true }).click(),
  ]);
  expect(file.suggestedFilename()).toBe(filename);
  await screenshot(page, 'phase5-private-documents', captures);

  const context = await browser.newContext({ baseURL });
  try {
    const owner = await context.newPage();
    const signedIn = owner.waitForResponse(response =>
      response.request().method() === 'POST' && new URL(response.url()).pathname === '/api/v1/auth/login',
    { timeout: 15_000 });
    await login(owner, ownerEmail!, ownerPassword!);
    const loginResponse = await signedIn;
    expect(loginResponse.status(), 'Owner sign-in response').toBe(200);
    expect((await loginResponse.json()).status, 'Owner sign-in result').toBe('authenticated');
    await expect(owner).toHaveURL(/\/owner$/);
    await owner.goto('/owner/properties');
    await owner.getByRole('link', { name, exact: true }).click();
    await owner.getByRole('tab', { name: 'Photos' }).click();
    await expect(owner.getByRole('img', { name: names[1] })).toBeVisible();
    await expect(owner.getByRole('button', { name: 'Set as cover' })).toHaveCount(0);
    await expect(owner.getByRole('button', { name: 'Remove', exact: true })).toHaveCount(0);
    await owner.getByRole('tab', { name: 'Documents' }).click();
    await expect(owner.getByText(filename, { exact: true })).toBeVisible();
    await expect(owner.locator('input[type=file]')).toHaveCount(0);
    const [download] = await Promise.all([
      owner.waitForEvent('download'), owner.getByRole('button', { name: 'Download', exact: true }).click(),
    ]);
    expect(download.suggestedFilename()).toBe(filename);
  } finally {
    await context.close();
  }
});
