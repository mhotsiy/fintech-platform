import { test, expect } from '../fixtures/fixtures';
import { MerchantsPage } from '../pages/MerchantsPage';
import { generateUniqueMerchant } from '../fixtures/test-data';

test.describe('Merchant Management', () => {
  test('should create merchant and verify correct data', async ({ page }) => {
    const merchant = generateUniqueMerchant();
    const merchantsPage = new MerchantsPage(page);

    await test.step('Navigate to merchants page', async () => {
      await merchantsPage.goto();
      await merchantsPage.verifyPageTitle('Merchants');
    });

    await test.step('Create new merchant', async () => {
      await merchantsPage.createMerchant(merchant.name, merchant.email);
    });

    await test.step('Search and verify merchant exists with correct data', async () => {
      await merchantsPage.searchMerchant(merchant.name);
      await merchantsPage.verifySearchResults();
      await merchantsPage.verifyMerchantData(merchant.name, merchant.email);
    });
  });
});
