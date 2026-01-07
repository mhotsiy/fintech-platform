import { defineConfig, devices } from '@playwright/test';
import dotenv from 'dotenv';

// Load .env file from working directory (local development)
// Command line env vars take precedence over .env
dotenv.config();

/**
 * Playwright configuration for FintechPlatform E2E tests
 * Environment variables for CI parameterization:
 * - BASE_URL: Admin UI URL (default: http://localhost:5173)
 * - API_URL: Backend API URL (default: http://localhost:5153)
 * - BROWSER: Browser to test (chromium|firefox|webkit|all, default: chromium)
 * - HEADLESS: Run headless (true|false, default: true in CI)
 * - WORKERS: Number of parallel workers (default: 4 in CI, 1 locally)
 * 
 * @see https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: './tests',
  outputDir: 'test-results/test-output',
  timeout: parseInt(process.env.TEST_TIMEOUT || '60000'),
  fullyParallel: true,
  retries: process.env.CI ? parseInt(process.env.RETRIES || '2') : 0,
  workers: process.env.WORKERS || (process.env.CI ? 4 : 1),
  reporter: [
    ['list'],
    ['html', { outputFolder: 'playwright-report', open: 'never' }],
    ['json', { outputFile: 'test-results/results.json' }]
  ],
  
  use: {
    baseURL: process.env.BASE_URL || 'http://localhost:5173',
    screenshot: 'only-on-failure',
    video: process.env.CI ? 'retain-on-failure' : 'off',
    trace: process.env.CI ? 'retain-on-failure' : 'off',
    actionTimeout: parseInt(process.env.ACTION_TIMEOUT || '10000'),
    navigationTimeout: parseInt(process.env.NAV_TIMEOUT || '30000'),
    headless: process.env.HEADLESS !== 'false',
  },

  projects: getBrowserProjects(),
});

/**
 * Get browser projects based on BROWSER env variable
 * Examples:
 *   BROWSER=chromium  -> Only Chromium
 *   BROWSER=firefox   -> Only Firefox
 *   BROWSER=all       -> All browsers
 */
function getBrowserProjects() {
  const browser = process.env.BROWSER || 'chromium';
  
  const allProjects = [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },
    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] },
    },
  ];

  if (browser === 'all') {
    return allProjects;
  }

  return allProjects.filter(p => p.name === browser);
}
