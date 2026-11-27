import { randomUUID } from 'node:crypto';
import { expect, test } from '@playwright/test';
import { demoPdf, login, screenshot } from './portfolio-fixtures';

const managerEmail = process.env['HOSTGRID_E2E_MANAGER_EMAIL'];
const managerPassword = process.env['HOSTGRID_E2E_MANAGER_PASSWORD'];
const recoveryCode = process.env['HOSTGRID_E2E_CONTRACTS_MANAGER_RECOVERY_CODE'];
const ownerEmail = process.env['HOSTGRID_E2E_FILES_OWNER_EMAIL'];
const ownerPassword = process.env['HOSTGRID_E2E_FILES_OWNER_PASSWORD'];
const ownerName = process.env['HOSTGRID_E2E_FILES_OWNER_NAME'];
const captures = process.env['HOSTGRID_CONTRACTS_SCREENSHOT_DIR'];

test('Manager configures the agreement and rate history and Owner can read them', async ({ page, browser, baseURL }) => {
  test.skip(!managerEmail || !managerPassword || !recoveryCode || !ownerEmail || !ownerPassword || !ownerName,
    'Requires fictitious Manager and Owner accounts and an unused Manager MFA recovery code.');
  const name = 'Atlas Courtyard ' + randomUUID().slice(0, 5);
  const filename = 'Fictitious management agreement.pdf';
  await page.setViewportSize({ width: 1440, height: 960 });
  await login(page, managerEmail!, managerPassword!, recoveryCode!);
  await page.waitForURL('**/manager');
  await page.goto('/manager/properties/new');
  await page.getByLabel('Name', { exact: true }).fill(name);
  await page.getByRole('combobox', { name: 'Owner', exact: true }).selectOption({ label: ownerName! });
  await page.getByRole('combobox', { name: 'Property type', exact: true }).selectOption({ label: 'Riad' });
  await page.getByLabel('City', { exact: true }).fill('Marrakech');
  await page.getByLabel('Address', { exact: true }).fill('10 Fictitious Courtyard');
  const details = page.waitForResponse(response => response.request().method() === 'GET' &&
    /\/properties\/[a-f0-9-]{36}$/i.test(new URL(response.url()).pathname));
  await page.getByRole('button', { name: 'Save', exact: true }).click();
  const { businessDate } = await (await details).json() as { businessDate: string };
  await expect(page.getByRole('heading', { name, exact: true })).toBeVisible();
  await page.getByRole('tab', { name: 'Documents' }).click();
  const picker = page.locator('#property-tab-content input[type=file]');
  await expect(picker).toBeEnabled();
  await picker.setInputFiles({ name: filename, mimeType: 'application/pdf', buffer: demoPdf() });
  await expect(page.getByText(filename, { exact: true })).toBeVisible();

  await page.getByRole('tab', { name: 'Owner and contract' }).click();
  const form = page.locator('#property-tab-content form').first();
  await form.getByLabel('Start date').fill(businessDate);
  await form.getByLabel('Agreed contract rate').fill('22.5');
  await form.getByRole('combobox', { name: 'Documents', exact: true }).selectOption({ label: filename });
  const submittedAgreement = page.waitForResponse(response => response.request().method() === 'POST' &&
    new URL(response.url()).pathname.endsWith('/contracts'));
  await form.getByRole('button', { name: 'Add contract' }).click();
  expect((await submittedAgreement).status()).toBe(200);
  await expect(page.getByRole('heading', { name: 'Active contract' })).toBeVisible();
  await expect(page.getByText(/agreed contract rate differs from the effective property rate/i)).toBeVisible();
  await expect(page.getByRole('button', { name: new RegExp(filename) })).toBeVisible();
  await screenshot(page, 'phase5-management-contract', captures);

  await page.getByRole('tab', { name: 'Pricing' }).click();
  const next = new Date(businessDate + 'T12:00:00Z');
  next.setUTCDate(next.getUTCDate() + 7);
  const future = next.toISOString().slice(0, 10);
  const rateForm = page.locator('#property-tab-content form').first();
  for (const [date, rate] of [[businessDate, '22.5'], [future, '24.75']]) {
    await rateForm.locator('[formControlName=effectiveFrom]').fill(date);
    await rateForm.locator('[formControlName=rate]').fill(rate);
    await rateForm.getByRole('button', { name: 'Add commission rate' }).click();
    const dialog = page.getByRole('dialog');
    await expect(dialog.getByText(/signed contract rates and closed records are preserved/i)).toBeVisible();
    const submittedRate = page.waitForResponse(response => response.request().method() === 'POST' &&
      new URL(response.url()).pathname.endsWith('/commission-rates'));
    await dialog.getByRole('button', { name: 'Confirm', exact: true }).click();
    expect((await submittedRate).status()).toBe(200);
    await expect(page.getByRole('row').filter({ hasText: rate + '%' })).toBeVisible();
  }
  await expect(page.getByText(/Current property commission.*22.5%/)).toBeVisible();
  await screenshot(page, 'phase5-commission-history', captures);

  const context = await browser.newContext({ baseURL });
  try {
    const owner = await context.newPage();
    await login(owner, ownerEmail!, ownerPassword!);
    await owner.waitForURL('**/owner');
    await owner.goto('/owner/properties');
    await owner.getByRole('link', { name, exact: true }).click();
    await owner.getByRole('tab', { name: 'Owner and contract' }).click();
    await expect(owner.getByRole('heading', { name: 'Active contract' })).toBeVisible();
    await expect(owner.getByRole('button', { name: 'Add contract' })).toHaveCount(0);
    const [download] = await Promise.all([
      owner.waitForEvent('download'), owner.getByRole('button', { name: new RegExp(filename) }).click(),
    ]);
    expect(download.suggestedFilename()).toBe(filename);
    await owner.getByRole('tab', { name: 'Pricing' }).click();
    await expect(owner.getByRole('row').filter({ hasText: '24.75%' })).toBeVisible();
    await expect(owner.getByRole('button', { name: 'Add commission rate' })).toHaveCount(0);
  } finally {
    await context.close();
  }
});
