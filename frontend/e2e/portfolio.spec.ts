import { randomUUID } from 'node:crypto';
import { expect, test } from '@playwright/test';
import { login, screenshot } from './portfolio-fixtures';

const email = process.env['HOSTGRID_E2E_MANAGER_EMAIL'];
const password = process.env['HOSTGRID_E2E_MANAGER_PASSWORD'];
const recoveryCode = process.env['HOSTGRID_E2E_MANAGER_RECOVERY_CODE'];
const captures = process.env['HOSTGRID_PHASE5_SCREENSHOT_DIR'];

test('Manager manages an owner account and property through the real API', async ({ page }) => {
  test.skip(!email || !password || !recoveryCode, 'Requires a fictitious Manager and one unused MFA recovery code.');
  const suffix = randomUUID().slice(0, 8);
  const ownerName = 'Browser Owner ' + suffix;
  const propertyName = 'Browser Studio ' + suffix;

  await page.addInitScript(() => localStorage.setItem('hostgrid.ui.v1', '{"language":"en","theme":"light"}'));
  await page.goto('/login');
  await page.getByLabel('Email', { exact: true }).fill(email!);
  await page.getByLabel('Password', { exact: true }).fill(password!);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await page.getByLabel('Use a recovery code').check();
  await page.getByLabel('Verification code').fill(recoveryCode!);
  await page.getByRole('button', { name: 'Verify', exact: true }).click();
  await page.waitForURL('**/manager');

  const confirm = () => page.getByRole('dialog').getByRole('button', { name: 'Confirm', exact: true }).click();
  const save = () => page.getByRole('button', { name: 'Save', exact: true }).click();

  await test.step('Create, edit, invite, disable and re-enable an owner', async () => {
    await page.goto('/manager/owners');
    await page.getByRole('link', { name: 'New owner' }).click();
    await page.getByLabel('Name', { exact: true }).fill(ownerName);
    await page.getByLabel('Contact email').fill('browser-' + suffix + '@example.test');
    await save();
    await expect(page.getByRole('heading', { name: ownerName, exact: true })).toBeVisible();
    await expect(page.getByText('No account linked', { exact: true })).toBeVisible();
    await page.getByRole('link', { name: 'Edit owner' }).click();
    await expect(page.getByLabel('Name', { exact: true })).toHaveValue(ownerName);
    await page.getByLabel('Phone').fill('555-0100');
    await save();
    await expect(page.getByText('555-0100')).toBeVisible();
    await page.getByRole('button', { name: 'Send invitation' }).click();
    await expect(page.getByText('Invitation pending', { exact: true })).toBeVisible();
    await page.getByRole('button', { name: 'Disable access', exact: true }).click();
    await confirm();
    await expect(page.getByText('Access disabled', { exact: true })).toBeVisible();
    await page.getByRole('button', { name: 'Re-enable access', exact: true }).click();
    await confirm();
    await expect(page.getByText('Invitation pending', { exact: true })).toBeVisible();
    await screenshot(page, 'phase5-manager-owner-details', captures);
    await page.goto('/manager/owners');
    await expect(page.getByRole('row').filter({ hasText: ownerName })).toBeVisible();
    await screenshot(page, 'phase5-manager-owners', captures);
  });

  await test.step('Create and edit a property, then archive and reactivate it', async () => {
    await page.goto('/manager/properties');
    await page.getByRole('link', { name: 'New property' }).click();
    await page.getByLabel('Name', { exact: true }).fill(propertyName);
    await page.getByRole('combobox', { name: 'Owner', exact: true }).selectOption({ label: ownerName });
    await page.getByRole('combobox', { name: 'Property type', exact: true }).selectOption({ label: 'Studio' });
    await page.getByLabel('City', { exact: true }).fill('Fes');
    await page.getByLabel('Address', { exact: true }).fill('10 Fictitious Street');
    await screenshot(page, 'phase5-manager-property-create', captures);
    await save();
    await expect(page.getByRole('heading', { name: propertyName, exact: true })).toBeVisible();
    await expect(page.getByRole('link', { name: ownerName })).toBeVisible();
    await page.getByRole('link', { name: 'Edit property' }).click();
    await expect(page.getByRole('combobox', { name: 'Owner', exact: true })).toBeDisabled();
    await expect(page.getByLabel('City', { exact: true })).toHaveValue('Fes');
    await page.getByLabel('City', { exact: true }).fill('Rabat');
    await save();
    await expect(page.getByText(/PR-.*Rabat/)).toBeVisible();
    await page.getByRole('button', { name: 'Archive', exact: true }).click();
    await confirm();
    await expect(page.getByText('Archived', { exact: true })).toBeVisible();
    await page.getByRole('button', { name: 'Reactivate', exact: true }).click();
    await confirm();
    await expect(page.getByText('Active', { exact: true })).toBeVisible();
    await screenshot(page, 'phase5-manager-property-details', captures);
    await page.getByRole('combobox', { name: 'Theme' }).selectOption('dark');
    await expect(page.locator('html')).toHaveClass(/hostgrid-dark/);
    await screenshot(page, 'phase5-manager-property-dark', captures);
    await page.getByRole('combobox', { name: 'Theme' }).selectOption('light');
    await page.getByRole('combobox', { name: 'Language' }).selectOption('ar');
    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl');
    await expect(page.locator('#tab-overview')).toBeVisible();
    await screenshot(page, 'phase5-manager-property-arabic', captures);
    await page.getByRole('combobox', { name: 'اللغة' }).selectOption('en');
    await page.locator('#tab-history').click();
    await expect(page.getByText('Property created', { exact: false })).toBeVisible();
  });

  await test.step('Find the same property in grid and server-filtered table views', async () => {
    await page.goto('/manager/properties');
    const search = page.getByRole('main').getByRole('searchbox');
    await search.fill(propertyName);
    await expect(page.getByRole('link', { name: propertyName })).toBeVisible();
    await screenshot(page, 'phase5-manager-properties-grid', captures);
    await page.getByRole('button', { name: 'Table', exact: true }).click();
    await expect(page.getByRole('row', { name: new RegExp(propertyName) })).toBeVisible();
    const sorted = page.waitForResponse(response => {
      const url = new URL(response.url());
      return url.pathname.endsWith('/properties') && url.searchParams.get('sort') === 'name';
    });
    await page.getByRole('columnheader', { name: 'Name', exact: true }).getByRole('button').click();
    expect((await sorted).ok()).toBe(true);
    await expect(page.getByRole('row').filter({ hasText: propertyName })).toBeVisible();
    await screenshot(page, 'phase5-manager-properties-table', captures);
  });

  await test.step('Keep the Manager portfolio usable in French and Arabic', async () => {
    await page.getByRole('combobox', { name: 'Language' }).selectOption('fr');
    await expect(page.getByRole('heading', { name: 'Biens', exact: true })).toBeVisible();
    await page.getByRole('combobox').filter({ has: page.getByRole('option', { name: 'English', exact: true }) }).selectOption('ar');
    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl');
    await expect(page.getByRole('heading', { name: 'العقارات', exact: true })).toBeVisible();
    await page.setViewportSize({ width: 390, height: 840 });
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(0);
    expect(await page.locator('.table-scroll').evaluate(table => table.scrollWidth > table.clientWidth)).toBe(true);
  });
});

test('Super Admin can open an explicitly selected organization portfolio', async ({ page }) => {
  const adminEmail = process.env['HOSTGRID_E2E_ADMIN_EMAIL'];
  const adminCode = process.env['HOSTGRID_E2E_ADMIN_RECOVERY_CODE'];
  const organization = process.env['HOSTGRID_E2E_ORGANIZATION_ID'];
  test.skip(!adminEmail || !password || !adminCode || !organization, 'Requires the fictitious live administrator fixture.');
  await login(page, adminEmail!, password!, adminCode!);
  await page.waitForURL('**/admin');
  const scopedProperties = page.waitForResponse(response => {
    const url = new URL(response.url());
    return url.pathname === '/api/v1/organizations/' + organization + '/properties' && response.request().method() === 'GET';
  });
  await page.goto('/admin/properties?organization=' + organization);
  expect((await scopedProperties).ok()).toBeTruthy();
  await expect(page.getByRole('combobox', { name: 'Organization', exact: true })).toHaveValue(organization!);
  await expect(page.getByRole('link', { name: 'New property' })).toBeVisible();
  await expect.poll(() => page.locator('.portfolio-card .card-body').evaluateAll(cards =>
    cards.every(card => card.scrollWidth <= card.clientWidth))).toBeTruthy();
  await screenshot(page, 'phase5-admin-properties', captures);
  await page.setViewportSize({ width: 768, height: 940 });
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(0);
});
