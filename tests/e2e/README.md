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

- Tests run sequentially (workers=1) to avoid race conditions
- Failed tests capture screenshots and videos
- Use `--headed` to watch tests execute
- Use `--ui` for interactive debugging
