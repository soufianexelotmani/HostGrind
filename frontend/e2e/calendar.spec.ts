import { expect, test } from '@playwright/test';
import { randomUUID } from 'node:crypto';
import { login, screenshot } from './portfolio-fixtures';

const email = process.env['HOSTGRID_E2E_MANAGER_EMAIL'];
const password = process.env['HOSTGRID_E2E_MANAGER_PASSWORD'];
const recovery = process.env['HOSTGRID_E2E_CALENDAR_MANAGER_RECOVERY_CODE'];
const property = process.env['HOSTGRID_E2E_BOOKING_PROPERTY'];
const captures = process.env['HOSTGRID_CALENDAR_SCREENSHOT_DIR'];
const ownerEmail = process.env['HOSTGRID_E2E_FILES_OWNER_EMAIL'];
const ownerPassword = process.env['HOSTGRID_E2E_FILES_OWNER_PASSWORD'];

test('Manager uses the portfolio and property calendar and protects hidden and blocked nights', async ({ page, request, browser, baseURL }) => {
  test.skip(!email || !password || !recovery || !property || !ownerEmail || !ownerPassword, 'Requires the synthetic live calendar fixture.');
  test.setTimeout(150_000);
  await page.setViewportSize({ width: 1440, height: 960 });
  const signIn = page.waitForResponse(response => new URL(response.url()).pathname === '/api/v1/auth/mfa/verify');
  await login(page, email!, password!, recovery);
  const session = await (await signIn).json();
  const organization = session.user.memberships.find((item: { role: string }) => item.role === 'Manager').organizationId;
  const api = `/api/v1/organizations/${organization}`;
  const headers = { Authorization: 'Bearer ' + session.accessToken, 'X-HostGrid-CSRF': '1' };
  const guest = await request.post(api + '/guests', { headers, data: { name: 'Fictitious Calendar Guest ' + randomUUID().slice(0, 6) } });
  expect(guest.status()).toBe(201);
  const { id: guestId } = await guest.json();

  await page.waitForURL('**/manager');
  await page.getByRole('link', { name: 'Calendar', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Portfolio calendar' })).toBeVisible();
  await expect(page.locator('[data-calendar-date]')).toHaveCount(30);
  await expect(page.getByRole('region', { name: 'Daily indicators' })).toBeVisible();
  await screenshot(page, 'phase63-portfolio', captures);
  await screenshot(page, 'phase63-daily-indicators', captures);
  const dates = await page.locator('[data-calendar-date]').evaluateAll(columns => columns.map(column => column.getAttribute('data-calendar-date')!));
  const row = page.locator(`[data-property="${property}"]`);
  const cell = (date: string) => row.locator(`[data-date="${date}"]`);
  const panel = () => page.getByRole('dialog');
  await page.getByRole('searchbox', { name: 'Find a property' }).fill('Fictitious Booking Riad');
  await expect(row).toBeVisible();
  await screenshot(page, 'phase63-filters', captures);
  const availableNight = cell(dates[18]).getByRole('button', { name: /Select dates/ });
  await availableNight.focus();
  await availableNight.press('Enter');
  await expect(panel().getByLabel('Start date', { exact: true })).toHaveValue(dates[18]);
  await panel().getByRole('link', { name: 'Create reservation' }).click();
  await expect(page.getByRole('combobox', { name: 'Property', exact: true })).toHaveValue(property!);
  await expect(page.getByLabel('Check-in date', { exact: true })).toHaveValue(dates[18]);
  await expect(page.getByLabel('Checkout date', { exact: true })).toHaveValue(dates[19]);
  await screenshot(page, 'phase63-quick-reservation', captures);
  await page.getByRole('combobox', { name: 'Guest', exact: true }).selectOption(guestId);
  await page.getByRole('button', { name: 'Save as Pending' }).click();
  await expect(page).toHaveURL(/\/manager\/calendar\?/);
  await cell(dates[18]).getByRole('button', { name: /Pending/ }).click();
  await expect(panel().getByRole('link', { name: 'View reservation' })).toBeVisible();
  await screenshot(page, 'phase63-reservation-preview', captures);
  await panel().getByRole('button', { name: 'Close' }).click();
  await page.getByRole('combobox', { name: 'Booking source' }).selectOption('0');
  await expect(cell(dates[18])).toHaveClass('held');
  await expect(cell(dates[18]).getByRole('button')).toHaveCount(0);

  await cell(dates[19]).getByRole('button', { name: /Select dates/ }).click();
  await panel().getByRole('button', { name: 'Block dates' }).click();
  await panel().getByRole('combobox', { name: 'Block reason' }).selectOption({ label: 'Maintenance' });
  await panel().getByLabel('Start date', { exact: true }).fill(dates[18]);
  await panel().getByRole('button', { name: 'Save block' }).click();
  await expect(panel().getByRole('alert')).toContainText('overlap');
  await panel().getByLabel('Start date', { exact: true }).fill(dates[19]);
  await screenshot(page, 'phase63-block-create', captures);
  await panel().getByRole('button', { name: 'Save block' }).click();
  await cell(dates[19]).getByRole('button', { name: /Maintenance/ }).click();
  await panel().getByRole('button', { name: 'Edit block' }).click();
  await panel().getByLabel('End date (exclusive)', { exact: true }).fill(dates[21]);
  await screenshot(page, 'phase63-block-edit', captures);
  await panel().getByRole('button', { name: 'Save block' }).click();
  await cell(dates[20]).getByRole('button', { name: /Maintenance/ }).click();
  await panel().getByRole('button', { name: 'Cancel block' }).click();
  await panel().getByLabel('Cancellation reason').fill('Synthetic calendar acceptance');
  await panel().getByRole('button', { name: 'Confirm cancellation' }).click();
  await expect(cell(dates[20]).getByRole('button', { name: /Select dates/ })).toBeVisible();
  await page.getByRole('combobox', { name: 'Booking source' }).selectOption('');
  await page.getByLabel('Show cancelled reservations').check();
  await expect(cell(dates[22]).getByRole('button', { name: /CALENDAR-CANCELLED/ })).toBeVisible();
  await expect(cell(dates[22]).getByRole('button', { name: /Select dates/ })).toBeVisible();

  await row.getByRole('link').click();
  await expect(page.locator('.fc-dayGridMonth-view')).toBeVisible();
  await expect(page.locator('.calendar-page')).toHaveAttribute('aria-busy', 'false');
  await expect(page.locator('.fc-event-draggable')).toHaveCount(0);
  await screenshot(page, 'phase63-property-month', captures);
  await page.locator('.fc-dayGridWeek-button').click();
  await expect(page.locator('.fc-dayGridWeek-view')).toBeVisible();
  await expect(page.locator('.calendar-page')).toHaveAttribute('aria-busy', 'false');
  await screenshot(page, 'phase63-property-week', captures);
  await page.locator('.fc-dayGridDay-button').click();
  await expect(page.locator('.fc-dayGridDay-view')).toBeVisible();
  await page.getByLabel('Business date', { exact: true }).fill(dates[18]);
  await expect(page.locator('.fc-dayGridDay-view .fc-event-title').filter({ hasText: 'Pending' })).toBeVisible();
  await screenshot(page, 'phase63-property-day', captures);

  await page.getByRole('link', { name: 'Portfolio calendar', exact: true }).click();
  if (process.env['HOSTGRID_CALENDAR_PERFORMANCE'] === '1') {
    await page.getByLabel('Business date', { exact: true }).fill('2031-01-01');
    const started = performance.now();
    await page.getByRole('searchbox', { name: 'Find a property' }).fill('Synthetic Calendar Perf');
    await expect(page.locator('[data-property]')).toHaveCount(30);
    await expect(page.locator('[data-property]').last()).toContainText('Synthetic Calendar Perf 030');
    const rendering = Math.round(performance.now() - started);
    const scroll = await page.locator('.calendar-grid').evaluate(async grid => {
      const started = performance.now();
      grid.scrollLeft = 1200;
      await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
      return { moved: grid.scrollLeft, duration: Math.round(performance.now() - started) };
    });
    expect(scroll.moved).toBeGreaterThan(0);
    console.log(`Calendar browser performance: 30 rows visible in ${rendering} ms, scroll painted in ${scroll.duration} ms`);
    await page.getByRole('searchbox', { name: 'Find a property' }).fill('Fictitious Booking Riad');
    await page.getByRole('button', { name: 'Today', exact: true }).click();
    await expect(row).toBeVisible();
  }
  await page.getByRole('banner').getByLabel('Language').selectOption('fr');
  await expect(page.getByRole('heading', { name: 'Calendrier du portefeuille' })).toBeVisible();
  await page.getByRole('banner').getByLabel('Langue').selectOption('ar');
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl');
  await screenshot(page, 'phase63-arabic-rtl', captures);
  await page.getByRole('banner').getByLabel(/المظهر/).selectOption('dark');
  await expect(page.locator('html')).toHaveClass(/hostgrid-dark/);
  await screenshot(page, 'phase63-dark', captures);
  await page.setViewportSize({ width: 390, height: 844 });
  await expect(page.locator('.calendar-agenda')).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(390);
  await screenshot(page, 'phase63-mobile', captures);

  const owner = await browser.newContext({ baseURL });
  try {
    const ownerPage = await owner.newPage();
    const response = ownerPage.waitForResponse(result => new URL(result.url()).pathname === '/api/v1/auth/login');
    await login(ownerPage, ownerEmail!, ownerPassword!);
    const ownerSession = await (await response).json();
    await ownerPage.waitForURL('**/owner');
    await ownerPage.goto('/manager/calendar');
    await expect(ownerPage).toHaveURL(/\/forbidden/);
    const denied = await request.post(api + '/calendar-blocks', { headers: { ...headers, Authorization: 'Bearer ' + ownerSession.accessToken },
      data: { propertyId: property, block: { startDate: dates[22], endDate: dates[23], reason: 0 } } });
    expect(denied.status()).toBe(403);
  } finally { await owner.close(); }
});
