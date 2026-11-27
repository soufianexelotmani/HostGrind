import { randomUUID } from 'node:crypto';
import { expect, test } from '@playwright/test';
import { login, screenshot } from './portfolio-fixtures';

const email = process.env['HOSTGRID_E2E_MANAGER_EMAIL'];
const password = process.env['HOSTGRID_E2E_MANAGER_PASSWORD'];
const recovery = process.env['HOSTGRID_E2E_PRICING_MANAGER_RECOVERY_CODE'];
const owner = process.env['HOSTGRID_E2E_FILES_OWNER_NAME'];
const captures = process.env['HOSTGRID_PRICING_SCREENSHOT_DIR'];

test('Manager configures amenities, inventory, nightly rates and manual listings', async ({ page }) => {
  test.skip(!email || !password || !recovery || !owner, 'Requires fictitious Manager and Owner fixtures.');
  await page.setViewportSize({ width: 1440, height: 1050 });
  await login(page, email!, password!, recovery!);
  await page.waitForURL('**/manager');
  await page.goto('/manager/properties/new');
  const name = 'Medina Studio ' + randomUUID().slice(0, 5);
  await page.getByLabel('Name', { exact: true }).fill(name);
  await page.getByRole('combobox', { name: 'Owner', exact: true }).selectOption({ label: owner! });
  await page.getByRole('combobox', { name: 'Property type', exact: true }).selectOption({ label: 'Studio' });
  await page.getByLabel('City', { exact: true }).fill('Marrakech');
  await page.getByLabel('Address', { exact: true }).fill('12 Fictitious Medina');
  const details = page.waitForResponse(response => response.request().method() === 'GET' &&
    /\/properties\/[a-f0-9-]{36}$/i.test(new URL(response.url()).pathname));
  await page.getByRole('button', { name: 'Save', exact: true }).click();
  const { businessDate } = await (await details).json() as { businessDate: string };

  const panel = page.locator('#property-tab-content');
  await page.getByRole('tab', { name: 'Amenities and inventory' }).click();
  await panel.getByLabel('Wi-Fi', { exact: true }).check();
  const savedAmenities = page.waitForResponse(response => response.request().method() === 'PUT' &&
    new URL(response.url()).pathname.endsWith('/amenities'));
  await panel.getByRole('button', { name: 'Save', exact: true }).click();
  expect((await savedAmenities).status()).toBe(200);
  await expect(panel.getByRole('button', { name: 'Save', exact: true })).toBeEnabled();
  await expect(panel.getByLabel('Wi-Fi', { exact: true })).toBeChecked();
  const inventory = panel.locator('form');
  await inventory.getByLabel('Name', { exact: true }).fill('Coffee maker');
  await inventory.getByLabel('Quantity', { exact: true }).fill('2');
  await inventory.getByLabel('Condition', { exact: true }).fill('Good');
  await inventory.locator('[formControlName=notes]').fill('Spare filters in the kitchen cabinet');
  await inventory.getByRole('button', { name: 'Add inventory item' }).click();
  await expect(panel.getByRole('row').filter({ hasText: 'Coffee maker' })).toContainText('Spare filters');
  await screenshot(page, 'phase5-inventory', captures);

  await page.getByRole('tab', { name: 'Pricing' }).click();
  const base = panel.locator('form').filter({ has: page.getByRole('button', { name: 'Save base rate' }) });
  await base.locator('[formControlName=nightlyRate]').fill('475.50');
  await base.getByRole('button', { name: 'Save base rate' }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Confirm', exact: true }).click();
  await expect(base.getByRole('button', { name: 'Save base rate' })).toBeEnabled();

  const friday = new Date(businessDate + 'T12:00:00Z');
  friday.setUTCDate(friday.getUTCDate() + 7);
  while (friday.getUTCDay() !== 5) friday.setUTCDate(friday.getUTCDate() + 1);
  const monday = new Date(friday);
  monday.setUTCDate(monday.getUTCDate() + 3);
  const start = friday.toISOString().slice(0, 10);
  const end = monday.toISOString().slice(0, 10);
  const rules = panel.locator('form').filter({ has: page.getByRole('button', { name: 'Add pricing rule' }) });
  await rules.getByRole('combobox', { name: 'Pricing rule type', exact: true }).selectOption({ label: 'Weekend' });
  await rules.locator('[formControlName=startDate]').fill(start);
  await rules.locator('[formControlName=endDate]').fill(end);
  await rules.locator('[formControlName=nightlyRate]').fill('650');
  await rules.getByRole('button', { name: 'Add pricing rule' }).click();
  await expect(panel.getByText(/Weekend.*650/)).toBeVisible();

  const listings = panel.locator('form').filter({ has: page.locator('[formControlName=externalReference]') });
  await listings.locator('[formControlName=externalReference]').fill('AIRBNB-DEMO-55');
  await listings.locator('[formControlName=url]').fill('https://example.test/listings/demo');
  await listings.getByRole('button', { name: 'Save', exact: true }).click();
  await expect(panel.getByText(/Airbnb.*AIRBNB-DEMO-55/)).toBeVisible();

  const preview = panel.locator('form').filter({ has: page.getByRole('button', { name: 'Preview nightly rates' }) });
  await preview.locator('[formControlName=start]').fill(start);
  await preview.locator('[formControlName=end]').fill(end);
  await preview.getByRole('button', { name: 'Preview nightly rates' }).click();
  const table = panel.getByRole('table', { name: 'Preview nightly rates' });
  await expect(table.getByRole('row')).toHaveCount(4);
  await expect(table.getByRole('row').filter({ hasText: 'Weekend' })).toHaveCount(2);
  await expect(table.getByRole('row').filter({ hasText: 'Base nightly rate' })).toHaveCount(1);
  await screenshot(page, 'phase5-pricing', captures);
});
