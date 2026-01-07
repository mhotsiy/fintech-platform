import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './BasePage';

export class MerchantsPage extends BasePage {
  readonly createButton: Locator;
  readonly searchInput: Locator;
  readonly searchClearButton: Locator;
  readonly merchantCards: Locator;
  readonly nameInput: Locator;
  readonly emailInput: Locator;
  readonly submitButton: Locator;

  constructor(page: Page) {
    super(page);
    this.createButton = page.locator('[data-testid="create-merchant-button"]');
    this.searchInput = page.locator('[data-testid="search-input"]');
    this.searchClearButton = page.locator('[data-testid="search-clear"]');
    this.merchantCards = page.locator('[data-testid^="merchant-card-"]');
    this.nameInput = page.locator('[data-testid="merchant-name-input"]');
    this.emailInput = page.locator('[data-testid="merchant-email-input"]');
    this.submitButton = page.locator('[data-testid="submit-button"]');
  }

  async goto() {
    await this.page.goto('/merchants');
  }

  async createMerchant(name: string, email: string) {
    await this.createButton.click();
    await this.nameInput.fill(name);
    await this.emailInput.fill(email);
    await this.submitButton.click();
    
    // Wait for modal to close
    await expect(this.nameInput).not.toBeVisible({ timeout: 5000 });
    
    // Wait for the new merchant card to appear with the name
    await expect(
      this.page.locator('[data-testid^="merchant-card-"]', { hasText: name })
    ).toBeVisible({ timeout: 10000 });
  }

  async searchMerchant(query: string) {
    await this.searchInput.fill(query);
    // Playwright auto-waits for the input, no need for manual timeout
  }

  async clearSearch() {
    await this.searchClearButton.click();
  }

  async getMerchantCount(): Promise<number> {
    return await this.merchantCards.count();
  }

  async clickFirstMerchant() {
    await this.merchantCards.first().click();
  }

  async verifySearchResults(minCount: number = 1) {
    // Wait for at least minCount merchant cards to be visible
    if (minCount > 0) {
      await expect(this.merchantCards.first()).toBeVisible({ timeout: 10000 });
    }
    
    // Verify we have at least the minimum count
    const count = await this.merchantCards.count();
    expect(count).toBeGreaterThanOrEqual(minCount);
  }

  async verifyMerchantData(expectedName: string, expectedEmail: string) {
    const firstCard = this.merchantCards.first();
    await expect(firstCard.locator('[data-testid="merchant-name"]')).toContainText(expectedName);
    await expect(firstCard.locator('[data-testid="merchant-email"]')).toContainText(expectedEmail);
  }

  async verifySearchInputCleared() {
    await expect(this.searchInput).toHaveValue('');
  }
}
