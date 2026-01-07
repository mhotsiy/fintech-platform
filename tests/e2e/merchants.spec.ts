import { test, expect } from '@playwright/test';

test.describe('Merchants', () => {
  test('should display merchants page', async ({ page }) => {
    await page.goto('/merchants');
    
    await expect(page.locator('[data-testid="page-title"]')).toContainText('Merchants');
  });
});
