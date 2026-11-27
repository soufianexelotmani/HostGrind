import { mkdir } from 'node:fs/promises';
import { join } from 'node:path';
import type { Page } from '@playwright/test';

export async function login(page: Page, email: string, password: string, code?: string): Promise<void> {
  await page.addInitScript(() => localStorage.setItem('hostgrid.ui.v1', '{"language":"en","theme":"light"}'));
  await page.goto('/login');
  await page.getByLabel('Email', { exact: true }).fill(email);
  await page.getByLabel('Password', { exact: true }).fill(password);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  if (code) {
    await page.getByLabel('Use a recovery code').check();
    await page.getByLabel('Verification code').fill(code);
    await page.getByRole('button', { name: 'Verify', exact: true }).click();
  }
}

export function demoPdf(): Buffer {
  const text = 'BT /F1 14 Tf 40 120 Td (Fictitious property information) Tj ET';
  const objects = [
    '<< /Type /Catalog /Pages 2 0 R >>',
    '<< /Type /Pages /Kids [3 0 R] /Count 1 >>',
    '<< /Type /Page /Parent 2 0 R /MediaBox [0 0 340 180] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>',
    '<< /Length ' + text.length + ' >>\nstream\n' + text + '\nendstream',
    '<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>',
  ];
  let content = '%PDF-1.4\n';
  const offsets = objects.map((object, index) => {
    const offset = content.length;
    content += (index + 1) + ' 0 obj\n' + object + '\nendobj\n';
    return String(offset).padStart(10, '0') + ' 00000 n \n';
  });
  const crossReference = content.length;
  return Buffer.from(content + 'xref\n0 6\n0000000000 65535 f \n' + offsets.join('') +
    'trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n' + crossReference + '\n%%EOF\n');
}

export async function screenshot(page: Page, name: string, captures?: string): Promise<void> {
  if (!captures) return;
  await mkdir(captures, { recursive: true });
  await page.evaluate(() => document.fonts.ready.then(() => undefined));
  await page.screenshot({ path: join(captures, name + '.png'), fullPage: true, animations: 'disabled' });
}
