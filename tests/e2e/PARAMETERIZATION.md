# E2E Test Parameterization Guide

## Running Tests with Different Configurations

### Local Development

```bash
# Run with default settings (chromium, headless)
npm test

# Run with specific browser
BROWSER=firefox npm test

# Run in headed mode (see browser)
HEADLESS=false npm test

# Run with all browsers
BROWSER=all npm test

# Run with more workers (parallel)
WORKERS=4 npm test

# Combine multiple settings
BROWSER=firefox HEADLESS=false WORKERS=2 npm test

# Test against different environment
BASE_URL=https://staging.example.com npm test
```

### CI/CD Configurations

#### Manual Workflow Trigger (GitHub UI)

1. Go to Actions → E2E Tests → Run workflow
2. Select browser: `chromium`, `firefox`, `webkit`, or `all`
3. Select environment: `dev`, `staging`, `prod`

#### Environment-Specific Workflows

**Staging:**
```yaml
env:
  BASE_URL: https://staging-admin.example.com
  API_URL: https://staging-api.example.com
  BROWSER: chromium
```

**Production Smoke Tests:**
```yaml
env:
  BASE_URL: https://admin.example.com
  API_URL: https://api.example.com
  BROWSER: all
  WORKERS: 1  # Sequential for prod
```

### Environment Variables Reference

| Variable | Default | Options | Description |
|----------|---------|---------|-------------|
| `BASE_URL` | `http://localhost:5173` | Any URL | Admin UI endpoint |
| `API_URL` | `http://localhost:5153` | Any URL | Backend API endpoint |
| `BROWSER` | `chromium` | `chromium`, `firefox`, `webkit`, `all` | Browser to test |
| `HEADLESS` | `true` | `true`, `false` | Run without UI |
| `WORKERS` | `1` (local), `4` (CI) | Number | Parallel workers |
| `TEST_TIMEOUT` | `60000` | Milliseconds | Overall test timeout |
| `ACTION_TIMEOUT` | `10000` | Milliseconds | Click/type timeout |
| `NAV_TIMEOUT` | `30000` | Milliseconds | Page load timeout |
| `RETRIES` | `0` (local), `2` (CI) | Number | Retry failed tests |

### Using .env Files

```bash
# Copy example
cp .env.example .env.local

# Edit with your settings
nano .env.local

# Run tests (automatically loads .env.local)
npm test
```

### Matrix Strategy in CI

The workflow uses GitHub Actions matrix to run tests across multiple browsers:

```yaml
strategy:
  matrix:
    browser: [chromium, firefox, webkit]
```

Each browser runs in parallel as a separate job.

### Advanced Examples

#### Cross-browser smoke test
```bash
BROWSER=all WORKERS=3 npm test -- smoke.spec.ts
```

#### Slow connection simulation
```bash
NAV_TIMEOUT=60000 ACTION_TIMEOUT=20000 npm test
```

#### Debug mode (headed, slow)
```bash
HEADLESS=false WORKERS=1 npm test -- --debug
```

#### Production verification
```bash
BASE_URL=https://prod.example.com \
API_URL=https://api.prod.example.com \
BROWSER=chromium \
WORKERS=1 \
npm test -- critical-flows.spec.ts
```

## Best Practices

1. **Local**: Use `.env.local` for your settings (git-ignored)
2. **CI**: Use GitHub Secrets for sensitive URLs
3. **Staging**: Test all browsers before prod deployment
4. **Production**: Run only critical paths with single worker
5. **Debug**: Always use `HEADLESS=false WORKERS=1` for troubleshooting
