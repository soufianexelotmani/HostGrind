import { createHmac, randomBytes, randomUUID } from 'node:crypto';
import { expect, test } from '@playwright/test';
import { demoPdf, login, screenshot } from './portfolio-fixtures';

const managerEmail = process.env['HOSTGRID_E2E_MANAGER_EMAIL'];
const managerPassword = process.env['HOSTGRID_E2E_MANAGER_PASSWORD'];
const recovery = process.env['HOSTGRID_E2E_OWNER_PORTAL_MANAGER_RECOVERY_CODE'];
const mailpit = process.env['HOSTGRID_E2E_MAILPIT_URL'];
const captures = process.env['HOSTGRID_OWNER_SCREENSHOT_DIR'];

function authenticatorCode(key: string): string {
  const alphabet = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567';
  const bits = Array.from(key.toUpperCase(), char => alphabet.indexOf(char).toString(2).padStart(5, '0')).join('');
  const secret = Buffer.from((bits.match(/.{8}/g) ?? []).map(byte => parseInt(byte, 2)));
  const counter = Buffer.alloc(8);
  counter.writeBigUInt64BE(BigInt(Math.floor(Date.now() / 30_000)));
  const hash = createHmac('sha1', secret).update(counter).digest();
  const offset = hash[hash.length - 1] & 15;
  return ((hash.readUInt32BE(offset) & 0x7fffffff) % 1_000_000).toString().padStart(6, '0');
}

test('invited Owner activates, uses the read-only portfolio and loses the revoked session', async ({ page, browser, request, baseURL }) => {
  test.skip(!managerEmail || !managerPassword || !recovery || !mailpit, 'Requires fictitious Manager credentials and a local Mailpit inbox.');
  test.setTimeout(150_000);
  const verify = page.waitForResponse(response => new URL(response.url()).pathname === '/api/v1/auth/mfa/verify');
  await login(page, managerEmail!, managerPassword!, recovery!);
  const manager = await (await verify).json();
  const org = manager.user.memberships.find((scope: { role: string }) => scope.role === 'Manager').organizationId;
  const orgPath = '/api/v1/organizations/' + org;
  const suffix = randomUUID().slice(0, 6);
  const ownerEmail = `owner-portal+${suffix}@example.test`;
  const ownerPassword = randomBytes(24).toString('base64url');
  const managerHeaders = { Authorization: 'Bearer ' + manager.accessToken, 'X-HostGrid-CSRF': '1' };

  async function createOwner(name: string, email?: string): Promise<string> {
    const reply = await request.post(orgPath + '/owners', { headers: managerHeaders, data: { name, email } });
    expect(reply.status()).toBe(201);
    return (await reply.json()).id;
  }

  const ownerId = await createOwner('Fictitious Portfolio Owner ' + suffix, ownerEmail);
  const otherId = await createOwner('Fictitious Neighbor ' + suffix);
  const types = await request.get(orgPath + '/properties/types', { headers: managerHeaders });
  const typeId = (await types.json()).find((type: { code: string }) => type.code === 'Riad').id;
  async function property(ownerId: string, name: string): Promise<string> {
    const reply = await request.post(orgPath + '/properties', { headers: managerHeaders, data: {
      ownerId, typeId, name, address: '12 Fictitious Courtyard', city: 'Marrakech', maxGuests: 4,
      bedrooms: 2, bathrooms: 1, baseNightlyRate: 500, checkInTime: '15:00:00', checkOutTime: '11:00:00',
    } });
    expect(reply.status()).toBe(201);
    return (await reply.json()).id;
  }
  const name = 'Atlas Courtyard ' + suffix;
  const first = await property(ownerId, name);
  await property(ownerId, 'Cedar Riad ' + suffix);
  const neighbor = await property(otherId, 'Private Neighbor ' + suffix);
  const propertyPath = orgPath + '/properties/' + first;
  const detail = await request.get(propertyPath, { headers: managerHeaders });
  const { businessDate } = await detail.json() as { businessDate: string };
  const filename = 'Fictitious agreement.pdf';
  const pdf = await request.post(propertyPath + '/documents', { headers: managerHeaders,
    multipart: { file: { name: filename, mimeType: 'application/pdf', buffer: demoPdf() } } });
  expect(pdf.status()).toBe(201);
  const documentId = (await pdf.json()).id;
  const photoAsset = await request.get('/brand/riad.jpg');
  const photo = await request.post(propertyPath + '/photos', { headers: managerHeaders, multipart: {
    file: { name: 'Courtyard.jpg', mimeType: 'image/jpeg', buffer: await photoAsset.body() },
  } });
  expect(photo.status()).toBe(201);
  const amenityCatalog = await request.get(orgPath + '/properties/amenities', { headers: managerHeaders });
  const wifi = (await amenityCatalog.json()).find((item: { code: string }) => item.code === 'Wifi');
  const latest = await request.get(propertyPath, { headers: managerHeaders });
  expect((await request.put(propertyPath + '/amenities', { headers: managerHeaders,
    data: { ids: [wifi.id], version: (await latest.json()).version } })).ok()).toBeTruthy();
  expect((await request.post(propertyPath + '/inventory', { headers: managerHeaders,
    data: { name: 'Coffee maker', quantity: 1 } })).ok()).toBeTruthy();
  expect((await request.post(propertyPath + '/contracts', { headers: managerHeaders,
    data: { startDate: businessDate, rate: 22.5, documentId } })).ok()).toBeTruthy();
  expect((await request.post(orgPath + '/owners/' + ownerId + '/invitation', { headers: managerHeaders })).ok()).toBeTruthy();

  const inbox = await request.get(mailpit + '/api/v1/messages');
  const mail = (await inbox.json()).messages.find((item: { To: { Address: string }[] }) => item.To.some(to => to.Address === ownerEmail));
  expect(mail).toBeTruthy();
  const message = await (await request.get(mailpit + '/api/v1/message/' + mail.ID)).json();
  const invitation = new URL(message.Text.match(/https?:\/\/[^\s]+\/activate#[^\s]+/)[0]);
  expect(invitation.origin).toBe(new URL(baseURL!).origin);

  const context = await browser.newContext({ baseURL, viewport: { width: 1440, height: 940 } });
  try {
    const portal = await context.newPage();
    await portal.addInitScript(() => {
      if (!localStorage.getItem('hostgrid.ui.v1')) localStorage.setItem('hostgrid.ui.v1', '{"language":"en","theme":"light"}');
    });
    await portal.goto(invitation.href);
    await portal.getByLabel('New password', { exact: true }).fill(ownerPassword);
    const activated = portal.waitForResponse(response => new URL(response.url()).pathname === '/api/v1/auth/activate');
    await portal.getByRole('button', { name: 'Set password' }).click();
    const activation = await activated;
    const submitted = activation.request().postDataJSON();
    expect(submitted.email === ownerEmail, 'activation email matches the invitation').toBeTruthy();
    expect(submitted.token === new URLSearchParams(invitation.hash.slice(1)).get('token'),
      'activation proof matches the invitation').toBeTruthy();
    expect(activation.status()).toBe(204);
    await login(portal, ownerEmail, ownerPassword);
    await portal.waitForURL('**/owner');
    await portal.goto('/security');
    await portal.getByLabel('Current password', { exact: true }).fill(ownerPassword);
    await portal.getByRole('button', { name: 'Set up an authenticator' }).click();
    const key = portal.getByRole('region', { name: 'Authenticator verification' }).locator('code');
    await expect(key).toBeVisible();
    await portal.getByLabel('Verification code').fill(authenticatorCode(await key.innerText()));
    await portal.getByRole('button', { name: 'Verify', exact: true }).click();
    await expect(portal.locator('.codes li').first()).toBeVisible();
    const recoveryCodes = await portal.locator('.codes li').allTextContents();
    expect(recoveryCodes.length).toBeGreaterThanOrEqual(2);
    await portal.getByRole('link', { name: 'Continue to your account' }).click();
    await portal.goto('/owner/properties');
    const ownProperty = portal.getByRole('link', { name, exact: true });
    await expect(ownProperty).toBeVisible();
    await expect(portal.getByRole('link', { name: 'Cedar Riad ' + suffix })).toBeVisible();
    await expect(portal.getByRole('link', { name: 'Private Neighbor ' + suffix })).toHaveCount(0);
    await expect(portal.getByRole('img', { name, exact: true })).toBeVisible();
    await screenshot(portal, 'phase5-owner-desktop', captures);
    await portal.setViewportSize({ width: 390, height: 840 });
    await screenshot(portal, 'phase5-owner-mobile', captures);
    await portal.getByRole('button', { name: 'Table', exact: true }).click();
    await expect(portal.getByRole('row').filter({ hasText: name })).toBeVisible();
    const sorted = portal.waitForResponse(response => {
      const url = new URL(response.url());
      return url.pathname.endsWith('/properties') && url.searchParams.get('sort') === 'name';
    });
    await portal.getByRole('columnheader', { name: 'Name', exact: true }).getByRole('button').click();
    expect((await sorted).ok()).toBeTruthy();
    for (const width of [360, 390, 768, 1024, 1440]) {
      await portal.setViewportSize({ width, height: 940 });
      await expect.poll(() => portal.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(0);
    }
    await portal.getByRole('button', { name: 'Grid', exact: true }).click();
    const search = portal.locator('main hg-search-input').getByRole('searchbox');
    await expect(search).toBeVisible();
    const noMatches = 'no property matches this query';
    const searched = portal.waitForResponse(response => {
      const url = new URL(response.url());
      return url.pathname.endsWith('/properties') && url.searchParams.get('search') === noMatches;
    }, { timeout: 15_000 });
    await search.fill(noMatches);
    expect((await searched).ok()).toBeTruthy();
    await expect(portal.getByText('No records to display.', { exact: true })).toBeVisible();
    await search.fill(name);
    await ownProperty.click();
    await expect(portal.getByRole('heading', { name, exact: true })).toBeVisible();
    await expect(portal.getByRole('link', { name: 'Edit property' })).toHaveCount(0);
    await expect(portal.locator('#tab-history')).toHaveCount(0);
    await screenshot(portal, 'phase5-owner-details', captures);
    const overview = portal.locator('#tab-overview');
    await overview.focus();
    await overview.press('ArrowRight');
    await expect(portal.getByRole('img', { name: 'Courtyard.jpg' })).toBeVisible();
    await portal.locator('#tab-contracts').click();
    await expect(portal.getByText(/22.5%/).first()).toBeVisible();
    await screenshot(portal, 'phase5-owner-contract', captures);
    await portal.locator('#tab-inventory').click();
    await expect(portal.getByText('Wi-Fi', { exact: true })).toBeVisible();
    await expect(portal.getByRole('cell', { name: 'Coffee maker' })).toBeVisible();
    await portal.locator('#tab-pricing').click();
    await expect(portal.getByText(/do not show the prices charged/)).toBeVisible();
    await portal.locator('#tab-documents').click();
    await expect(portal.getByText(filename, { exact: true })).toBeVisible();
    const download = portal.waitForEvent('download');
    await portal.getByRole('button', { name: 'Download', exact: true }).click();
    expect((await download).suggestedFilename()).toBe(filename);
    await screenshot(portal, 'phase5-owner-documents', captures);

    const refresh = portal.waitForResponse(response => new URL(response.url()).pathname === '/api/v1/auth/refresh');
    await portal.reload();
    const session = await (await refresh).json();
    const headers = { Authorization: 'Bearer ' + session.accessToken, 'X-HostGrid-CSRF': '1' };
    expect((await request.get(orgPath + '/properties/' + neighbor, { headers })).status()).toBe(404);
    expect((await request.put(propertyPath + '/listings/Direct', { headers, data: { externalReference: 'forbidden' } })).status()).toBe(404);
    await portal.goto('/manager/properties');
    await portal.waitForURL('**/forbidden');
    await portal.goto('/owner/properties');
    await expect(ownProperty).toBeVisible();
    await portal.getByRole('combobox', { name: 'Theme' }).selectOption('dark');
    await expect(portal.locator('html')).toHaveClass(/hostgrid-dark/);
    await screenshot(portal, 'phase5-owner-dark', captures);
    await portal.getByRole('combobox', { name: 'Theme' }).selectOption('system');
    await portal.emulateMedia({ colorScheme: 'light' });
    await expect(portal.locator('html')).not.toHaveClass(/hostgrid-dark/);
    await portal.getByRole('combobox', { name: 'Language' }).selectOption('fr');
    await expect(portal.locator('html')).toHaveAttribute('lang', 'fr');
    await portal.getByRole('combobox', { name: 'Langue' }).selectOption('ar');
    await expect(portal.locator('html')).toHaveAttribute('dir', 'rtl');
    await expect(ownProperty).toBeVisible();
    await portal.setViewportSize({ width: 390, height: 840 });
    await expect.poll(() => portal.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(0);
    await screenshot(portal, 'phase5-owner-arabic', captures);
    await portal.setViewportSize({ width: 1440, height: 940 });
    await portal.getByRole('combobox', { name: 'اللغة' }).selectOption('en');

    const lifecycle = orgPath + '/owners/' + ownerId;
    expect((await request.post(lifecycle + '/disable-access', { headers: managerHeaders })).ok()).toBeTruthy();
    expect((await request.get(propertyPath, { headers })).status()).toBe(401);
    await portal.reload();
    await portal.waitForURL('**/unauthorized');
    expect((await request.post(lifecycle + '/enable-access', { headers: managerHeaders })).ok()).toBeTruthy();
    expect((await request.get(propertyPath, { headers })).status()).toBe(401);
    await login(portal, ownerEmail, ownerPassword, recoveryCodes[0]);
    await portal.waitForURL('**/owner');
    await portal.locator('summary[aria-label="Account menu"]').click();
    await portal.getByRole('button', { name: 'Sign out', exact: true }).click();
    await portal.waitForURL('**/login');
  } finally {
    await context.close();
  }
});
