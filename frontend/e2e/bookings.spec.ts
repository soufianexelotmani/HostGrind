import { randomUUID } from 'node:crypto';
import { expect, test } from '@playwright/test';
import { demoPdf, login, screenshot } from './portfolio-fixtures';

const mail = process.env['HOSTGRID_E2E_MANAGER_EMAIL'];
const password = process.env['HOSTGRID_E2E_MANAGER_PASSWORD'];
const recovery = process.env['HOSTGRID_E2E_BOOKING_MANAGER_RECOVERY_CODE'];
const property = process.env['HOSTGRID_E2E_BOOKING_PROPERTY'];
const ownerEmail = process.env['HOSTGRID_E2E_FILES_OWNER_EMAIL'];
const ownerPassword = process.env['HOSTGRID_E2E_FILES_OWNER_PASSWORD'];
const captures = process.env['HOSTGRID_BOOKING_SCREENSHOT_DIR'];

test('Manager creates, revises and records a historical reservation with private evidence', async ({ page, browser, baseURL, request }) => {
  test.skip(!mail || !password || !recovery || !property || !ownerEmail || !ownerPassword,
    'Requires fictitious Manager, Owner and Active property fixtures with a current and historical commission schedule.');
  test.setTimeout(150_000);
  page.setDefaultTimeout(15_000);
  const verify = page.waitForResponse(response => new URL(response.url()).pathname === '/api/v1/auth/mfa/verify');
  await login(page, mail!, password!, recovery);
  const session = await (await verify).json();
  const org = session.user.memberships.find((item: { role: string }) => item.role === 'Manager').organizationId;
  const base = '/api/v1/organizations/' + org;
  const headers = { Authorization: 'Bearer ' + session.accessToken, 'X-HostGrid-CSRF': '1' };
  const suffix = randomUUID().slice(0, 6);
  const guest = 'Fictitious Guest ' + suffix;
  const confirm = () => page.getByRole('dialog').getByRole('button', { name: 'Confirm', exact: true }).click();
  await page.waitForURL('**/manager');
  await page.setViewportSize({ width: 1440, height: 940 });

  await page.goto('/manager/guests');
  await expect(page.getByRole('heading', { name: 'Guests', exact: true })).toBeVisible();
  await page.getByRole('link', { name: 'New guest' }).click();
  await page.getByLabel('Name', { exact: true }).fill(guest);
  await page.getByRole('button', { name: 'Save', exact: true }).click();
  await expect(page.getByRole('heading', { name: guest, exact: true })).toBeVisible();
  const guestId = new URL(page.url()).pathname.split('/').at(-1);
  await screenshot(page, 'phase6-guest-details', captures);
  await page.goto('/manager/guests');
  await expect(page.getByRole('row').filter({ hasText: guest })).toBeVisible();
  await screenshot(page, 'phase6-guests', captures);

  const today = new Intl.DateTimeFormat('en-CA', { timeZone: 'Africa/Casablanca', year: 'numeric', month: '2-digit',
    day: '2-digit' }).format(new Date());
  const date = (offset: number) => {
    const day = new Date(today + 'T12:00:00Z');
    day.setUTCDate(day.getUTCDate() + offset);
    return day.toISOString().slice(0, 10);
  };
  const selectStay = async (start: string, end: string) => {
    await expect(page.locator('form.portfolio-form button[type="submit"]')).toBeEnabled({ timeout: 15_000 });
    const propertySelect = page.getByRole('combobox', { name: 'Property', exact: true });
    await propertySelect.selectOption(property!, { timeout: 15_000 });
    await expect(propertySelect).toHaveValue(property!);
    await page.getByRole('combobox', { name: 'Guest', exact: true }).selectOption(guestId!, { timeout: 15_000 });
    await page.getByLabel('Check-in date', { exact: true }).fill(start);
    await page.getByLabel('Checkout date', { exact: true }).fill(end);
  };
  const detail = async () => {
    await expect(page.getByRole('heading', { name: /^RS-/ })).toBeVisible();
    return new URL(page.url()).pathname.split('/').at(-1)!;
  };
  await page.goto('/manager/reservations');
  await expect(page.getByRole('heading', { name: 'Reservations', exact: true })).toBeVisible();
  await screenshot(page, 'phase6-reservations', captures);
  await page.getByRole('link', { name: 'New reservation' }).click();
  await selectStay(date(10), date(12));
  await page.getByRole('button', { name: 'Check availability' }).click();
  await expect(page.getByRole('status').filter({ hasText: /^Available$/ })).toBeVisible();
  await screenshot(page, 'phase6-reservation-create', captures);
  await page.getByRole('button', { name: 'Preview nightly prices' }).click();
  await expect(page.getByRole('heading', { name: 'Nightly prices' })).toBeVisible();
  await page.getByLabel('Manual override ' + date(10)).fill('123.45');
  await screenshot(page, 'phase6-reservation-pricing', captures);
  await page.getByRole('button', { name: 'Save as Pending' }).click();
  await detail();
  await expect(page.getByText('Pending', { exact: true })).toBeVisible();

  await page.goto('/manager/reservations/new');
  await selectStay(date(11), date(13));
  await page.getByRole('button', { name: 'Save as Pending' }).click();
  await expect(page.getByRole('alert').filter({ hasText: 'overlap a reservation' })).toBeVisible();
  await page.getByLabel('Check-in date', { exact: true }).fill(date(12));
  await page.getByLabel('Checkout date', { exact: true }).fill(date(14));
  await page.getByLabel('External reference', { exact: true }).fill('EX-' + suffix);
  await page.getByRole('button', { name: 'Preview nightly prices' }).click();
  await page.getByLabel('Manual override ' + date(12)).fill('153.40');
  await page.getByRole('button', { name: 'Save as Confirmed' }).click();
  await confirm();
  const confirmedId = await detail();
  await expect(page.getByText('18.5%', { exact: true })).toBeVisible();
  await expect(page.getByText('Confirmed', { exact: true })).toBeVisible();
  await screenshot(page, 'phase6-reservation-details', captures);
  await page.getByRole('link', { name: 'Edit reservation' }).click();
  await page.getByLabel('Checkout date', { exact: true }).fill(date(15));
  await page.getByRole('button', { name: 'Preview nightly prices' }).click();
  await expect(page.getByRole('listitem').filter({ hasText: /153.40.*Manual override/ })).toBeVisible();
  await page.getByLabel('I accept the pricing changes').check();
  await screenshot(page, 'phase6-reservation-revision', captures);
  await page.getByRole('button', { name: 'Save', exact: true }).click();
  await confirm();
  expect(await detail()).toBe(confirmedId);

  await page.goto('/manager/reservations/new');
  await selectStay(date(15), date(16));
  await page.getByLabel('External reference', { exact: true }).fill('EX-' + suffix);
  await page.getByRole('button', { name: 'Save as Pending' }).click();
  await expect(page.getByRole('alert').filter({ hasText: 'already exists for the booking source' })).toBeVisible();

  await page.goto('/manager/reservations/historical');
  await selectStay('2025-09-11', '2025-09-13');
  await page.getByLabel('Confirmation date', { exact: true }).fill('2025-08-30');
  await page.getByLabel('Actual arrival date', { exact: true }).fill('2025-09-11');
  await page.getByLabel('Actual departure date', { exact: true }).fill('2025-09-13');
  await page.getByLabel('Reason for historical entry').fill('Fictitious statement for local verification');
  await page.getByLabel('Net amount', { exact: true }).fill('440.01');
  await page.getByLabel('Private PDF evidence').setInputFiles({ name: 'Fictitious evidence.pdf', mimeType: 'application/pdf', buffer: demoPdf() });
  await screenshot(page, 'phase6-historical-entry', captures);
  await page.getByRole('button', { name: 'Save', exact: true }).click();
  await confirm();
  const historical = await detail();
  await expect(page.getByText('Allocated from total', { exact: false }).first()).toBeVisible();
  const document = await request.get(base + `/reservations/${historical}/evidence`, { headers });
  expect(document.status()).toBe(200);
  expect(document.headers()['content-type']).toContain('application/pdf');

  await page.getByRole('combobox', { name: 'Theme' }).selectOption('dark');
  await expect(page.locator('html')).toHaveClass(/hostgrid-dark/);
  await screenshot(page, 'phase6-reservation-dark', captures);
  await page.getByRole('combobox', { name: 'Language' }).selectOption('fr');
  await expect(page.getByRole('heading', { name: 'Saisie historique' })).toBeVisible();
  await page.getByRole('combobox', { name: 'Langue' }).selectOption('ar');
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl');
  await page.setViewportSize({ width: 390, height: 844 });
  await expect(page.getByRole('heading', { name: 'إدخال حجز سابق' })).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await screenshot(page, 'phase6-reservation-arabic-mobile', captures);

  const ownerContext = await browser.newContext({ baseURL });
  try {
    const owner = await ownerContext.newPage();
    const auth = owner.waitForResponse(response => new URL(response.url()).pathname === '/api/v1/auth/login');
    await login(owner, ownerEmail!, ownerPassword!);
    const ownerSession = await (await auth).json();
    await expect(owner).toHaveURL(/\/owner$/);
    const denied = await request.get(base + `/reservations/${historical}/evidence`,
      { headers: { Authorization: 'Bearer ' + ownerSession.accessToken } });
    expect(denied.status()).toBe(403);
  } finally { await ownerContext.close(); }
});
