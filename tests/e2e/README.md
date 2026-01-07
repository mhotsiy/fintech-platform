# E2E Tests

Simple Playwright test for the FintechPlatform UI.

## Setup

```bash
cd tests/e2e
npm install
npm run install  # Install Chromium browser
```

## Run Test

```bash
npm test                # Run in headless mode
npm run test:ui        # Run with Playwright UI
```

Make sure the app is running on http://localhost:5173 before running tests.
