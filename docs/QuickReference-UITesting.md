# Quick Reference: UI Testing Features

## Summary of Testable Features Added

### 1. **Search** 🔍
- **Location**: Merchants page
- **Test ID**: `search-input`, `search-clear`
- **Test Scenarios**:
  - Search by merchant name
  - Search by merchant email
  - Clear search query
  - No results found

### 2. **Filter** 🎯  
- **Location**: Merchants page
- **Test ID**: `status-filter`, `filter-results`
- **Test Scenarios**:
  - Filter by Active status
  - Filter by Inactive status
  - Combine search + filter
  - Reset to "All Status"

### 3. **Pagination** 📄
- **Location**: Merchants page (12 items/page)
- **Test IDs**: 
  - `pagination-prev` - Previous button
  - `pagination-next` - Next button  
  - `pagination-page-{N}` - Page number buttons
  - `pagination-info` - Results count
- **Test Scenarios**:
  - Navigate to next/previous page
  - Jump to specific page number
  - Boundary conditions (first/last page)
  - Info text shows correct range

### 4. **Toast Notifications** 🔔
- **Location**: Top-right corner (global)
- **Test IDs**:
  - `toast-success` - Green success toast
  - `toast-error` - Red error toast
  - `toast-warning` - Yellow warning toast
  - `toast-message` - Message text
  - `toast-close` - Close button
- **Test Scenarios**:
  - Success toast on merchant create
  - Error toast on API failure
  - Auto-dismiss after 5 seconds
  - Manual dismiss with X button
  - Multiple toasts stack correctly

### 5. **Confirmation Dialogs** ⚠️
- **Location**: Before destructive actions
- **Test IDs**:
  - `confirm-dialog-overlay` - Modal background
  - `confirm-dialog` - Dialog container
  - `confirm-dialog-title` - Dialog title
  - `confirm-dialog-message` - Description
  - `confirm-dialog-cancel` - Cancel button
  - `confirm-dialog-confirm` - Confirm button
- **Test Scenarios**:
  - Shows before refund
  - Shows before cancel withdrawal
  - Can cancel without executing action
  - Confirms and executes action
  - Loading state during execution

### 6. **Form Inputs** 📝
**Create Merchant Modal**:
- `create-merchant-modal` - Modal overlay
- `create-merchant-form` - Form element
- `merchant-name-input` - Name field
- `merchant-email-input` - Email field
- `cancel-button` - Cancel button
- `submit-button` - Submit button

**Test Scenarios**:
- Required field validation
- Email format validation
- Success creates merchant + toast
- Error shows toast
- Form resets after success

### 7. **Navigation** 🧭
- `nav-link-dashboard` - Dashboard link
- `nav-link-merchants` - Merchants link
- `nav-link-health` - Health link
- `mobile-menu-button` - Mobile menu toggle

**Test Scenarios**:
- Navigate between pages
- Active link highlighting
- Mobile menu opens/closes
- Logo returns to dashboard

### 8. **Merchant List** 📋
- `merchants-page` - Page container
- `page-title` - Page heading
- `create-merchant-button` - Create button
- `merchants-grid` - Grid container
- `merchant-card-{id}` - Individual card
- `merchant-name` - Name text
- `merchant-email` - Email text
- `merchant-status` - Status badge
- `merchant-created` - Date text
- `empty-state` - Empty state container

**Test Scenarios**:
- Grid renders all merchants
- Click card navigates to detail
- Empty state shows when no data
- Empty state shows create button

### 9. **Custom Hooks** 🪝

#### useToast
```typescript
const { success, error, warning } = useToast();
success('Operation successful!');
```

#### usePagination
```typescript
const { paginatedItems, currentPage, goToPage } = usePagination(items, 12);
```

## E2E Test Example (Playwright)

```typescript
import { test, expect } from '@playwright/test';

test('search, filter, and paginate merchants', async ({ page }) => {
  // Navigate to merchants page
  await page.goto('/merchants');
  
  // Search for merchant
  await page.fill('[data-testid="search-input"]', 'Acme');
  await expect(page.locator('[data-testid="filter-results"]')).toContainText('matching "Acme"');
  
  // Apply status filter
  await page.selectOption('[data-testid="status-filter"]', 'Active');
  await expect(page.locator('[data-testid="filter-results"]')).toContainText('with status "Active"');
  
  // Clear search
  await page.click('[data-testid="search-clear"]');
  await expect(page.locator('[data-testid="search-input"]')).toHaveValue('');
  
  // Navigate to next page (if exists)
  if (await page.locator('[data-testid="pagination-next"]').isEnabled()) {
    await page.click('[data-testid="pagination-next"]');
    await expect(page.locator('[data-testid="pagination-info"]')).toContainText('13 to 24');
  }
  
  // Create merchant
  await page.click('[data-testid="create-merchant-button"]');
  await page.fill('[data-testid="merchant-name-input"]', 'Test Corp');
  await page.fill('[data-testid="merchant-email-input"]', 'test@corp.com');
  await page.click('[data-testid="submit-button"]');
  
  // Verify success toast
  await expect(page.locator('[data-testid="toast-success"]')).toBeVisible();
  await expect(page.locator('[data-testid="toast-message"]')).toContainText('Merchant created successfully!');
});
```

## Component Test Example (React Testing Library)

```typescript
import { render, screen, fireEvent } from '@testing-library/react';
import { SearchInput } from '@/components/SearchInput';

test('SearchInput clears value when clicking clear button', () => {
  const onChange = vi.fn();
  render(<SearchInput value="test" onChange={onChange} />);
  
  fireEvent.click(screen.getByTestId('search-clear'));
  
  expect(onChange).toHaveBeenCalledWith('');
});
```

## Test Data Setup

### Seed Merchants
```bash
curl -X POST http://localhost:5153/api/merchants \
  -H "Content-Type: application/json" \
  -d '{"name":"Test Merchant 1","email":"test1@example.com"}'
```

### Create Multiple Merchants (Loop)
```bash
for i in {1..50}; do
  curl -X POST http://localhost:5153/api/merchants \
    -H "Content-Type: application/json" \
    -d "{\"name\":\"Merchant $i\",\"email\":\"merchant$i@test.com\"}"
done
```

## Coverage Checklist

- [ ] Search by name returns correct results
- [ ] Search by email returns correct results
- [ ] Clear search button works
- [ ] Status filter "Active" works
- [ ] Status filter "Inactive" works
- [ ] Combined search + filter works
- [ ] Empty state shows when no results
- [ ] Pagination shows correct page count
- [ ] Next button navigates forward
- [ ] Previous button navigates backward
- [ ] Page number buttons work
- [ ] Pagination info shows correct range
- [ ] Create merchant shows success toast
- [ ] Create merchant with error shows error toast
- [ ] Toast auto-dismisses after 5s
- [ ] Toast manual dismiss works
- [ ] Confirmation dialog appears for refund
- [ ] Confirmation can be canceled
- [ ] Confirmation executes action
- [ ] Navigation links work
- [ ] Mobile menu opens/closes
- [ ] Form validation works
- [ ] All test IDs present

## Running Tests

### Playwright
```bash
# Install
npm install -D @playwright/test

# Run all tests
npx playwright test

# Run specific test
npx playwright test merchants.spec.ts

# Run in headed mode
npx playwright test --headed

# Run with UI mode
npx playwright test --ui
```

### Vitest (Unit Tests)
```bash
# Install
npm install -D vitest @testing-library/react @testing-library/jest-dom

# Run tests
npm run test

# Watch mode
npm run test:watch

# Coverage
npm run test:coverage
```

## Debugging

### Playwright
```typescript
await page.pause(); // Opens Playwright Inspector
await page.screenshot({ path: 'screenshot.png' });
```

### React DevTools
- Install browser extension
- Inspect component state
- View React Query cache

### Vite Console
```bash
docker logs -f fintechplatform-admin-ui
```

## Performance Tips

1. **Use data-testid**: 10x faster than CSS selectors
2. **Avoid waitForTimeout**: Use waitForSelector instead
3. **Parallel tests**: Run independent tests in parallel
4. **Mock API**: Use MSW for faster tests
5. **Headless mode**: Run tests without browser UI for speed

## Resources

- **Full Testing Guide**: `/docs/UITestingGuide.md`
- **Feature Documentation**: `/docs/AdminUI-Features.md`
- **Component README**: `/admin-ui/README.md`
- **Playwright Docs**: https://playwright.dev
- **Testing Library**: https://testing-library.com
