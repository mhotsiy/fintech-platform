import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './BasePage';

export class MerchantDetailPage extends BasePage {
  readonly createPaymentButton: Locator;
  readonly bulkPaymentsButton: Locator;
  readonly filtersButton: Locator;
  readonly balanceCards: Locator;
  readonly paymentCards: Locator;
  readonly paymentAmountInput: Locator;
  readonly paymentCurrencySelect: Locator;
  readonly bulkCountInput: Locator;
  readonly bulkAmountInput: Locator;
  readonly submitButton: Locator;
  readonly confirmDialogConfirm: Locator;
  readonly statusFilter: Locator;

  constructor(page: Page) {
    super(page);
    this.createPaymentButton = page.locator('[data-testid="create-payment-button"]');
    this.bulkPaymentsButton = page.locator('button:has-text("Bulk Payments")');
    this.filtersButton = page.locator('button:has-text("Filters")');
    this.balanceCards = page.locator('[data-testid="balance-card"]');
    this.paymentCards = page.locator('[data-testid^="payment-card-"]');
    this.paymentAmountInput = page.locator('[data-testid="payment-amount-input"]');
    this.paymentCurrencySelect = page.locator('[data-testid="payment-currency-select"]');
    this.bulkCountInput = page.locator('[data-testid="bulk-count-input"]');
    this.bulkAmountInput = page.locator('[data-testid="bulk-amount-input"]');
    this.submitButton = page.locator('[data-testid="submit-button"]');
    this.confirmDialogConfirm = page.locator('[data-testid="confirm-dialog-confirm"]');
    this.statusFilter = page.locator('[data-testid="status-filter"]');
  }

  async createPayment(amount: string, currency: string = 'USD') {
    await this.createPaymentButton.click();
    await this.paymentAmountInput.fill(amount);
    await this.paymentCurrencySelect.selectOption(currency);
    await this.submitButton.click();
  }

  async createBulkPayments(count: number, amount: string) {
    await this.bulkPaymentsButton.click();
    await this.bulkCountInput.fill(count.toString());
    await this.bulkAmountInput.fill(amount);
    await this.submitButton.click();
  }

  async getAvailableBalance(): Promise<string> {
    const balanceText = await this.page.locator('[data-testid="available-balance"]').textContent();
    return balanceText || '0';
  }

  async waitForPaymentCard(timeout: number = 30000) {
    await this.paymentCards.first().waitFor({ timeout });
  }

  async getPaymentCardsCount(): Promise<number> {
    return await this.paymentCards.count();
  }

  async completeFirstPayment() {
    await this.page.locator('[data-testid^="complete-payment-button-"]').first().click();
  }

  async refundFirstPayment() {
    await this.page.locator('[data-testid^="refund-payment-button-"]').first().click();
    await this.confirmDialogConfirm.click();
  }

  async openFilters() {
    if (await this.filtersButton.isVisible()) {
      await this.filtersButton.click();
    }
  }

  async filterByStatus(status: string) {
    if (await this.statusFilter.isVisible()) {
      await this.statusFilter.selectOption(status);
    }
  }

  getFirstPaymentStatus(): Locator {
    return this.page.locator('[data-testid="payment-status"]').first();
  }

  async verifyPaymentCardExists() {
    await expect(this.paymentCards.first()).toBeVisible();
  }

  async verifyPaymentStatus(expectedStatus: string) {
    await expect(this.getFirstPaymentStatus()).toContainText(expectedStatus);
  }

  async verifyMinimumPaymentCount(minCount: number) {
    const count = await this.getPaymentCardsCount();
    expect(count).toBeGreaterThanOrEqual(minCount);
  }

  async waitForUrlPattern(pattern: RegExp) {
    await this.page.waitForURL(pattern);
  }
}
