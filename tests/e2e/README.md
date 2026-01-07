# E2E Tests

Playwright end-to-end tests for the FintechPlatform.

## Project Structure

```
tests/e2e/
├── tests/                    # Test specs
│   ├── merchant-workflow.spec.ts
│   └── payment-operations.spec.ts
├── pages/                    # Page Object Models
│   ├── MerchantsPage.ts
│   └── MerchantDetailPage.ts
├── helpers/                  # Test utilities
│   └── api-helper.ts
├── fixtures/                 # Test data
│   └── test-data.ts
└── playwright.config.ts      # Playwright configuration
```

## Setup

```bash
cd tests/e2e
npm install
npm run install  # Install Chromium browser
```

## Prerequisites

Make sure the following services are running:
- Admin UI: http://localhost:5173
- API: http://localhost:5153
- Workers: Processing payments in background

Start all services:
```bash
cd /home/marian/fintech-platform
docker-compose up -d
```

## Run Tests

```bash
npm test                # Run all tests (headless)
npm run test:headed     # Run with browser visible
npm run test:ui         # Interactive UI mode
npm run test:debug      # Debug mode with step-through
npm run test:report     # View HTML test report
```

## Environment Variables

Create `.env.local` (optional):
```
BASE_URL=http://localhost:5173
API_URL=http://localhost:5153
```

## Test Scenarios

### Merchant Workflow
- Create merchant
- Search merchant
- Navigate to merchant detail
- Create payment
- Bulk payment creation

### Payment Operations
- Payment refund flow
- Payment filtering
- Status verification

## Tips

- Tests run in parallel (4 workers in CI, 1 locally)
- Failed tests capture screenshots, videos, and **console errors**
- Use `--headed` to watch tests execute
- Use `--ui` for interactive debugging
- **Console errors and warnings** are automatically captured and attached to test reports
- Check `playwright-report/` for detailed console logs after test runs

## Console Error Reporting

Tests automatically capture and report:
- ❌ **Console errors**: Red flag in terminal and attached to HTML report
- ⚠️ **Console warnings**: Attached to test report
- ℹ️ **Failed network requests**: Logged for debugging API issues

After test completion, check:
1. Terminal output for real-time console errors
2. HTML report (`playwright-report/index.html`) for attached console logs
3. `test-results/results.json` for programmatic access
