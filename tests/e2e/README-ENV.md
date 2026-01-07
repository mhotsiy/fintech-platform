# Environment Configuration

## Quick Start

```bash
# 1. Copy example to create your local config
cp .env.example .env

# 2. Edit with your preferences
nano .env

# 3. Run tests (automatically uses .env)
npm test
```

## How It Works

**Priority order (highest to lowest):**
1. Command line: `BROWSER=firefox npm test`
2. .env file: `BROWSER=chromium` 
3. Defaults in playwright.config.ts

**Examples:**
```bash
# Use .env settings
npm test

# Override .env for one run
BROWSER=firefox npm test

# Use .env but override one setting
HEADLESS=false npm test
```

## Files

| File | Committed? | Purpose |
|------|-----------|---------|
| `.env.example` | ✅ Yes | Template with all options |
| `.env` | ❌ No | Your personal settings |
| `.env.local` | ❌ No | Alternative name (also works) |

## CI/CD

In CI, we don't use .env files. GitHub Actions sets environment variables directly:

```yaml
env:
  BROWSER: chromium
  WORKERS: 4
```

This is more secure and explicit for CI environments.
