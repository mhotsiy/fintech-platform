# UI Testing Guide

## Overview
This document outlines all testable features in the FintechPlatform Admin UI, with data-testid attributes and testing strategies for end-to-end (E2E) tests.

## Testing Stack Recommendations
- **Framework**: Playwright or Cypress
- **Assertion Library**: Built-in (Playwright) or Chai (Cypress)
- **Test Runner**: Vitest or Jest for unit tests
- **Component Testing**: React Testing Library

## Data Test IDs Reference

### Navigation Component
- `nav-bar` - Main navigation container
- `mobile-menu-button` - Hamburger menu button
- `nav-link-dashboard` - Dashboard navigation link
- `nav-link-merchants` - Merchants navigation link
- `nav-link-health` - Health navigation link

### Merchants Page

#### Search & Filter
- `merchants-page` - Page container
- `page-title` - Page heading
- `create-merchant-button` - Create new merchant button
- `search-input` - Search input field
- `search-clear` - Clear search button
- `status-filter` - Status dropdown filter
- `filter-results` - Filter results text

#### Merchant Grid
- `merchants-grid` - Grid container
- `merchant-card-{id}` - Individual merchant card (dynamic ID)
- `merchant-name` - Merchant name
- `merchant-email` - Merchant email
- `merchant-status` - Merchant status badge
- `merchant-created` - Creation date

#### Empty State
- `empty-state` - Empty state container

#### Pagination
- `pagination` - Pagination container
- `pagination-info` - "Showing X to Y of Z" text
- `pagination-prev` - Previous page button
- `pagination-next` - Next page button
- `pagination-page-{number}` - Page number buttons (dynamic)

#### Create Modal
- `create-merchant-modal` - Modal overlay
- `create-merchant-form` - Form element
- `merchant-name-input` - Name input
- `merchant-email-input` - Email input
- `cancel-button` - Cancel button
- `submit-button` - Submit button

### Payments & Withdrawals

#### Payment Actions
- `create-payment-button` - Create payment button
- `complete-payment-button-{id}` - Complete button (dynamic ID)
- `refund-payment-button-{id}` - Refund button (dynamic ID)
- `payment-card-{id}` - Payment card (dynamic ID)
- `payment-status` - Payment status badge

#### Withdrawal Actions
- `create-withdrawal-button` - Create withdrawal button
- `cancel-withdrawal-button-{id}` - Cancel button (dynamic ID)
- `withdrawal-card-{id}` - Withdrawal card (dynamic ID)
- `withdrawal-status` - Withdrawal status badge

### Confirmation Dialog
- `confirm-dialog-overlay` - Dialog overlay
- `confirm-dialog` - Dialog container
- `confirm-dialog-title` - Dialog title
- `confirm-dialog-message` - Dialog message
- `confirm-dialog-cancel` - Cancel button
- `confirm-dialog-confirm` - Confirm button

### Toast Notifications
- `toast-success` - Success toast
- `toast-error` - Error toast
- `toast-warning` - Warning toast
- `toast-message` - Toast message text
- `toast-close` - Close toast button

## Test Scenarios

### 1. Search Functionality

#### Test: Search merchants by name
```typescript
test('should filter merchants by name', async ({ page }) => {
  await page.goto('/merchants');
  
  await page.fill('[data-testid="search-input"]', 'Acme');
  
  const results = await page.locator('[data-testid^="merchant-card-"]').count();
  expect(results).toBeGreaterThan(0);
  
  const resultText = await page.textContent('[data-testid="filter-results"]');
  expect(resultText).toContain('matching "Acme"');
});
```

#### Test: Search merchants by email
```typescript
test('should filter merchants by email', async ({ page }) => {
  await page.goto('/merchants');
  
  await page.fill('[data-testid="search-input"]', 'acme.com');
  
  const merchantEmail = await page.textContent('[data-testid="merchant-email"]');
  expect(merchantEmail).toContain('acme.com');
});
```

#### Test: Clear search
```typescript
test('should clear search query', async ({ page }) => {
  await page.goto('/merchants');
  
  await page.fill('[data-testid="search-input"]', 'Test');
  await page.click('[data-testid="search-clear"]');
  
  const inputValue = await page.inputValue('[data-testid="search-input"]');
  expect(inputValue).toBe('');
});
```

### 2. Filter Functionality

#### Test: Filter by status
```typescript
test('should filter merchants by status', async ({ page }) => {
  await page.goto('/merchants');
  
  await page.selectOption('[data-testid="status-filter"]', 'Active');
  
  const statuses = await page.locator('[data-testid="merchant-status"]').allTextContents();
  expect(statuses.every(status => status === 'Active')).toBe(true);
});
```

#### Test: Combined search and filter
```typescript
test('should apply both search and status filter', async ({ page }) => {
  await page.goto('/merchants');
  
  await page.fill('[data-testid="search-input"]', 'Corp');
  await page.selectOption('[data-testid="status-filter"]', 'Active');
  
  const resultText = await page.textContent('[data-testid="filter-results"]');
  expect(resultText).toContain('matching "Corp"');
  expect(resultText).toContain('with status "Active"');
});
```

### 3. Pagination

#### Test: Navigate to next page
```typescript
test('should navigate to next page', async ({ page }) => {
  await page.goto('/merchants');
  
  const firstPageFirstMerchant = await page.textContent('[data-testid^="merchant-card-"]:first-child [data-testid="merchant-name"]');
  
  await page.click('[data-testid="pagination-next"]');
  
  const secondPageFirstMerchant = await page.textContent('[data-testid^="merchant-card-"]:first-child [data-testid="merchant-name"]');
  expect(secondPageFirstMerchant).not.toBe(firstPageFirstMerchant);
});
```

#### Test: Jump to specific page
```typescript
test('should jump to specific page', async ({ page }) => {
  await page.goto('/merchants');
  
  await page.click('[data-testid="pagination-page-3"]');
  
  const paginationInfo = await page.textContent('[data-testid="pagination-info"]');
  expect(paginationInfo).toContain('25 to 36'); // Assuming 12 items per page
});
```

#### Test: Previous button disabled on first page
```typescript
test('should disable previous button on first page', async ({ page }) => {
  await page.goto('/merchants');
  
  const prevButton = page.locator('[data-testid="pagination-prev"]');
  await expect(prevButton).toBeDisabled();
});
```

### 4. Toast Notifications

#### Test: Success toast on merchant creation
```typescript
test('should show success toast after creating merchant', async ({ page }) => {
  await page.goto('/merchants');
  
  await page.click('[data-testid="create-merchant-button"]');
  await page.fill('[data-testid="merchant-name-input"]', 'Test Corp');
  await page.fill('[data-testid="merchant-email-input"]', 'test@corp.com');
  await page.click('[data-testid="submit-button"]');
  
  const toast = await page.locator('[data-testid="toast-success"]');
  await expect(toast).toBeVisible();
  
  const toastMessage = await page.textContent('[data-testid="toast-message"]');
  expect(toastMessage).toBe('Merchant created successfully!');
});
```

#### Test: Toast auto-dismisses after duration
```typescript
test('should auto-dismiss toast after 5 seconds', async ({ page }) => {
  await page.goto('/merchants');
  
  // Trigger action that shows toast
  await page.click('[data-testid="create-merchant-button"]');
  await page.fill('[data-testid="merchant-name-input"]', 'Test');
  await page.fill('[data-testid="merchant-email-input"]', 'test@test.com');
  await page.click('[data-testid="submit-button"]');
  
  await expect(page.locator('[data-testid="toast-success"]')).toBeVisible();
  
  await page.waitForTimeout(5100);
  
  await expect(page.locator('[data-testid="toast-success"]')).not.toBeVisible();
});
```

#### Test: Manual toast dismissal
```typescript
test('should manually dismiss toast', async ({ page }) => {
  // ... show toast ...
  
  await page.click('[data-testid="toast-close"]');
  
  await expect(page.locator('[data-testid="toast-success"]')).not.toBeVisible();
});
```

### 5. Confirmation Dialogs

#### Test: Show confirmation before refund
```typescript
test('should show confirmation dialog before refunding payment', async ({ page }) => {
  await page.goto('/merchants/merchant-id-123');
  
  await page.click('[data-testid="refund-payment-button-payment-id-456"]');
  
  await expect(page.locator('[data-testid="confirm-dialog"]')).toBeVisible();
  
  const title = await page.textContent('[data-testid="confirm-dialog-title"]');
  expect(title).toContain('Confirm Refund');
});
```

#### Test: Cancel confirmation dialog
```typescript
test('should cancel refund when clicking cancel', async ({ page }) => {
  await page.goto('/merchants/merchant-id-123');
  
  await page.click('[data-testid="refund-payment-button-payment-id-456"]');
  await page.click('[data-testid="confirm-dialog-cancel"]');
  
  await expect(page.locator('[data-testid="confirm-dialog"]')).not.toBeVisible();
  
  // Verify payment status unchanged
  const status = await page.textContent('[data-testid="payment-status"]');
  expect(status).not.toBe('Refunded');
});
```

#### Test: Confirm destructive action
```typescript
test('should execute refund when confirming', async ({ page }) => {
  await page.goto('/merchants/merchant-id-123');
  
  await page.click('[data-testid="refund-payment-button-payment-id-456"]');
  await page.click('[data-testid="confirm-dialog-confirm"]');
  
  // Wait for success toast
  await expect(page.locator('[data-testid="toast-success"]')).toBeVisible();
  
  // Verify payment status changed
  const status = await page.textContent('[data-testid="payment-status"]');
  expect(status).toBe('Refunded');
});
```

### 6. Form Validation

#### Test: Required field validation
```typescript
test('should prevent submission with empty fields', async ({ page }) => {
  await page.goto('/merchants');
  
  await page.click('[data-testid="create-merchant-button"]');
  await page.click('[data-testid="submit-button"]');
  
  // HTML5 validation should prevent submission
  const nameInput = page.locator('[data-testid="merchant-name-input"]');
  await expect(nameInput).toHaveAttribute('required');
});
```

#### Test: Email format validation
```typescript
test('should validate email format', async ({ page }) => {
  await page.goto('/merchants');
  
  await page.click('[data-testid="create-merchant-button"]');
  await page.fill('[data-testid="merchant-name-input"]', 'Test');
  await page.fill('[data-testid="merchant-email-input"]', 'invalid-email');
  await page.click('[data-testid="submit-button"]');
  
  // HTML5 email validation
  const emailInput = page.locator('[data-testid="merchant-email-input"]');
  await expect(emailInput).toHaveAttribute('type', 'email');
});
```

### 7. Empty States

#### Test: Show empty state when no results
```typescript
test('should show empty state when search returns no results', async ({ page }) => {
  await page.goto('/merchants');
  
  await page.fill('[data-testid="search-input"]', 'NonExistentMerchant12345');
  
  await expect(page.locator('[data-testid="empty-state"]')).toBeVisible();
  
  const emptyText = await page.textContent('[data-testid="empty-state"]');
  expect(emptyText).toContain('No merchants found');
});
```

#### Test: Empty state with no merchants
```typescript
test('should show create button in empty state', async ({ page }) => {
  // Assuming fresh database
  await page.goto('/merchants');
  
  if (await page.locator('[data-testid="empty-state"]').isVisible()) {
    await expect(page.locator('[data-testid="empty-state"] button')).toBeVisible();
  }
});
```

### 8. Loading States

#### Test: Show loading spinner
```typescript
test('should show loading spinner while fetching data', async ({ page }) => {
  // Intercept API and delay response
  await page.route('**/api/merchants', async route => {
    await page.waitForTimeout(2000);
    await route.continue();
  });
  
  const loadingPromise = page.goto('/merchants');
  
  // Check for loading spinner while request is pending
  // (Implementation depends on your loading component structure)
  
  await loadingPromise;
});
```

### 9. Navigation

#### Test: Navigate between pages
```typescript
test('should navigate to merchant detail', async ({ page }) => {
  await page.goto('/merchants');
  
  await page.click('[data-testid^="merchant-card-"]:first-child');
  
  await expect(page).toHaveURL(/\/merchants\/[a-z0-9-]+/);
});
```

#### Test: Active link highlighting
```typescript
test('should highlight active navigation link', async ({ page }) => {
  await page.goto('/merchants');
  
  const merchantsLink = page.locator('[data-testid="nav-link-merchants"]');
  await expect(merchantsLink).toHaveClass(/active/); // Adjust class name
});
```

### 10. Responsive Design

#### Test: Mobile menu toggle
```typescript
test('should toggle mobile menu', async ({ page }) => {
  await page.setViewportSize({ width: 375, height: 667 }); // Mobile size
  
  await page.goto('/');
  
  await page.click('[data-testid="mobile-menu-button"]');
  
  // Check if mobile menu is visible
  await expect(page.locator('[data-testid="nav-link-dashboard"]')).toBeVisible();
});
```

## Unit Testing Examples

### Testing Utility Functions

```typescript
import { formatCurrency, formatDate, generateIdempotencyKey } from '@/lib/utils';

describe('formatCurrency', () => {
  it('should format minor units to currency', () => {
    expect(formatCurrency(10000, 'USD')).toBe('$100.00');
    expect(formatCurrency(12345, 'EUR')).toBe('€123.45');
  });
  
  it('should handle zero', () => {
    expect(formatCurrency(0, 'USD')).toBe('$0.00');
  });
});

describe('generateIdempotencyKey', () => {
  it('should generate unique keys', () => {
    const key1 = generateIdempotencyKey();
    const key2 = generateIdempotencyKey();
    
    expect(key1).not.toBe(key2);
    expect(key1.length).toBeGreaterThan(10);
  });
});
```

### Testing Custom Hooks

```typescript
import { renderHook, act } from '@testing-library/react';
import { usePagination } from '@/hooks/usePagination';

describe('usePagination', () => {
  const items = Array.from({ length: 50 }, (_, i) => ({ id: i }));
  
  it('should paginate items', () => {
    const { result } = renderHook(() => usePagination(items, 10));
    
    expect(result.current.paginatedItems.length).toBe(10);
    expect(result.current.totalPages).toBe(5);
    expect(result.current.currentPage).toBe(1);
  });
  
  it('should navigate to next page', () => {
    const { result } = renderHook(() => usePagination(items, 10));
    
    act(() => {
      result.current.nextPage();
    });
    
    expect(result.current.currentPage).toBe(2);
    expect(result.current.paginatedItems[0].id).toBe(10);
  });
});
```

### Testing Components

```typescript
import { render, screen, fireEvent } from '@testing-library/react';
import { SearchInput } from '@/components/SearchInput';

describe('SearchInput', () => {
  it('should call onChange when typing', () => {
    const onChange = vi.fn();
    render(<SearchInput value="" onChange={onChange} />);
    
    const input = screen.getByTestId('search-input');
    fireEvent.change(input, { target: { value: 'test' } });
    
    expect(onChange).toHaveBeenCalledWith('test');
  });
  
  it('should show clear button when value is not empty', () => {
    render(<SearchInput value="test" onChange={() => {}} />);
    
    expect(screen.getByTestId('search-clear')).toBeInTheDocument();
  });
  
  it('should clear value when clicking clear button', () => {
    const onChange = vi.fn();
    render(<SearchInput value="test" onChange={onChange} />);
    
    fireEvent.click(screen.getByTestId('search-clear'));
    
    expect(onChange).toHaveBeenCalledWith('');
  });
});
```

## Test Data Setup

### Seed Data Script
```typescript
// tests/helpers/seed.ts
export async function seedTestData(apiClient) {
  const merchants = await Promise.all([
    apiClient.createMerchant({ name: 'Acme Corp', email: 'acme@test.com' }),
    apiClient.createMerchant({ name: 'Tech Inc', email: 'tech@test.com' }),
    apiClient.createMerchant({ name: 'Global LLC', email: 'global@test.com' }),
  ]);
  
  for (const merchant of merchants) {
    await apiClient.createPayment(merchant.id, {
      amountInMinorUnits: 10000,
      currency: 'USD',
      description: 'Test payment',
    });
  }
  
  return { merchants };
}
```

## CI/CD Integration

### GitHub Actions Example
```yaml
name: E2E Tests

on: [push, pull_request]

jobs:
  e2e:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: '20'
      
      - name: Install dependencies
        run: npm ci
      
      - name: Start services
        run: docker-compose up -d
      
      - name: Wait for services
        run: npx wait-on http://localhost:5173 http://localhost:5153
      
      - name: Run Playwright tests
        run: npx playwright test
      
      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: playwright-report
          path: playwright-report/
```

## Coverage Goals

- **E2E Tests**: 80% coverage of critical user flows
- **Unit Tests**: 90% coverage of utility functions and hooks
- **Component Tests**: 85% coverage of reusable components
- **Integration Tests**: 75% coverage of API interactions

## Best Practices

1. **Use data-testid for stability**: Prefer `data-testid` over CSS selectors or text content
2. **Test user behavior**: Focus on what users do, not implementation details
3. **Avoid brittle tests**: Don't rely on exact text or timing
4. **Use Page Object Model**: Encapsulate page interactions for reusability
5. **Mock external services**: Use MSW or similar for API mocking
6. **Test accessibility**: Include aria-label and keyboard navigation tests
7. **Visual regression**: Consider Percy or Chromatic for visual testing
8. **Performance**: Monitor page load times and bundle sizes

## Next Steps

1. Set up Playwright or Cypress
2. Create test fixtures and helpers
3. Implement core user flow tests
4. Add visual regression testing
5. Integrate with CI/CD
6. Monitor test flakiness
7. Achieve target coverage goals
