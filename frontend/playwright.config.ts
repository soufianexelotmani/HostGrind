import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  workers: 1,
  timeout: 90_000,
  use: { baseURL: 'http://localhost:4200', channel: process.env['HOSTGRID_BROWSER_CHANNEL'] || undefined },
  webServer: { command: 'npm start', url: 'http://localhost:4200', reuseExistingServer: true, timeout: 120_000 },
});
