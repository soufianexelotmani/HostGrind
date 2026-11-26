import { expect, Page, test } from '@playwright/test';
import { mkdir } from 'node:fs/promises';
import { join } from 'node:path';

type Role = 'SuperAdmin' | 'Manager' | 'Owner';
const captures = process.env['HOSTGRID_SCREENSHOT_DIR'];

async function screenshot(page: Page, name: string) {
  if (!captures) return;
  await mkdir(captures, { recursive: true });
  await page.evaluate(() => document.fonts.ready.then(() => undefined));
  await page.screenshot({ path: join(captures, name + '.png'), animations: 'disabled', fullPage: true });
}

async function fixture(page: Page, role: Role | null, language: 'en' | 'fr' | 'ar', theme: 'light' | 'dark') {
  await page.addInitScript(value => localStorage.setItem('hostgrid.ui.v1', JSON.stringify(value)), { language, theme });
  await page.route('**/api/v1/auth/refresh', route => role ? route.fulfill({ json: {
    status: 'authenticated', accessToken: 'browser-fixture-only',
    user: { id: 'fixture', email: 'workspace@example.test', superAdmin: role === 'SuperAdmin', mfaEnabled: true,
      memberships: role === 'SuperAdmin' ? [] : [{ organizationId: 'fixture', role, ownerId: role === 'Owner' ? 'owner' : null }] },
  } }) : route.fulfill({ status: 401, json: { title: 'Sign-in required' } }));
}

async function noOverflow(page: Page) {
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
}

test('login layout, translations and password visibility', async ({ page }) => {
  await fixture(page, null, 'en', 'light');
  await page.setViewportSize({ width: 1440, height: 940 });
  await page.goto('/login', { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible();
  await expect(page.locator('input[name=password]')).toHaveAttribute('type', 'password');
  await page.getByRole('button', { name: 'Show password' }).click();
  await expect(page.locator('input[name=password]')).toHaveAttribute('type', 'text');
  await page.getByRole('button', { name: 'Hide password' }).click();
  await screenshot(page, 'login-desktop');
  await page.getByRole('combobox', { name: 'Language' }).selectOption('fr');
  await expect(page.getByRole('heading', { name: 'Se connecter' })).toBeVisible();
  await page.getByRole('combobox', { name: 'Thème' }).selectOption('dark');
  await expect(page.locator('html')).toHaveClass(/hostgrid-dark/);
  for (const width of [360, 390, 768, 1024, 1440]) {
    await page.setViewportSize({ width, height: 850 });
    await noOverflow(page);
  }
});

test('administrator desktop shell supports the keyboard palette and sidebar', async ({ page }) => {
  const errors: Error[] = [];
  page.on('pageerror', error => errors.push(error));
  await fixture(page, 'SuperAdmin', 'en', 'light');
  await page.setViewportSize({ width: 1440, height: 940 });
  await page.goto('/admin');
  await expect(page.getByRole('heading', { name: 'Platform overview' })).toBeVisible();
  await expect(page.locator('.sidebar').getByText('Organizations')).toBeVisible();
  await screenshot(page, 'admin-desktop');
  await page.getByRole('button', { name: 'Collapse sidebar' }).click();
  await expect(page.locator('.workspace')).toHaveClass(/collapsed/);
  await page.keyboard.press('Control+k');
  const search = page.getByRole('searchbox', { name: 'Search navigation' });
  await expect(search).toBeFocused();
  await search.fill('organization');
  await expect(page.getByText('No matching pages')).toBeVisible();
  await search.fill('dashboard');
  await expect(page.getByRole('dialog').getByRole('link', { name: 'Dashboard' })).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(page.getByRole('dialog')).toHaveCount(0);
  expect(errors).toEqual([]);
});

test('French manager and Arabic owner at tablet and mobile widths', async ({ browser, baseURL }) => {
  for (const [role, language, title, url] of [
    ['Manager', 'fr', 'Vue de gestion', '/manager'],
    ['Owner', 'ar', 'نظرة عامة للمالك', '/owner'],
  ] as const) {
    const context = await browser.newContext({ baseURL, viewport: { width: 768, height: 900 } });
    try {
      const page = await context.newPage();
      await fixture(page, role, language, 'dark');
      await page.goto(url);
      await expect(page.getByRole('heading', { name: title })).toBeVisible();
      await expect(page.locator('html')).toHaveAttribute('dir', language === 'ar' ? 'rtl' : 'ltr');
      for (const width of [360, 390, 768, 1024, 1440]) {
        await page.setViewportSize({ width, height: 900 });
        await noOverflow(page);
      }
      await page.setViewportSize({ width: role === 'Owner' ? 390 : 768, height: 900 });
      await expect(page.locator('.bottom-navigation')).toBeVisible();
      await screenshot(page, role === 'Owner' ? 'owner-mobile-rtl' : 'manager-tablet-dark');
      await page.locator('.bottom-navigation button').click();
      await expect(page.getByRole('dialog')).toBeVisible();
      await page.keyboard.press('Escape');
      await page.goto('/admin');
      await expect(page).toHaveURL(/\/forbidden$/);
    } finally { await context.close(); }
  }
});
