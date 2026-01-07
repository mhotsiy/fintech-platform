import { Page, Locator, expect } from '@playwright/test';

export class BasePage {
  readonly page: Page;
  readonly successToast: Locator;
  readonly errorToast: Locator;
  readonly pageTitle: Locator;

  constructor(page: Page) {
    this.page = page;
    this.successToast = page.locator('[data-testid="toast-success"]');
    this.errorToast = page.locator('[data-testid="toast-error"]');
    this.pageTitle = page.locator('[data-testid="page-title"]');
  }

  async waitForSuccessToast() {
    await expect(this.successToast).toBeVisible({ timeout: 5000 });
  }

  async waitForErrorToast() {
    await expect(this.errorToast).toBeVisible({ timeout: 5000 });
  }

  async verifyPageTitle(expectedTitle: string) {
    await expect(this.pageTitle).toContainText(expectedTitle);
  }
}
