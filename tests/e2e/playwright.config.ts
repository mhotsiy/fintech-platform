import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright configuration for FintechPlatform E2E tests
 * @see https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: './',
  timeout: 30 * 1000,
  fullyParallel: true,
  retries: 0,
  reporter: 'list',
  
  use: {
    baseURL: 'http://localhost:5173',
    screenshot: 'only-on-failure',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
