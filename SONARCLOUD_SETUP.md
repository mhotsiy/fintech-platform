# SonarCloud Setup (2 minutes)

## 1. Go to https://sonarcloud.io
- Login with GitHub

## 2. Import Project
- Click "+" → "Analyze new project"
- Select `fintech-platform`
- Click "Set Up"

## 3. Add Secret to GitHub
- Copy the token from SonarCloud
- Go to GitHub: Settings → Secrets → Actions → New secret
- Name: `SONAR_TOKEN`
- Value: [paste token]

## 4. Done!
Push code and SonarCloud analyzes automatically.

View: https://sonarcloud.io/project/overview?id=mhotsiy_fintech-platform

## What it does:
- Scans code for bugs, code smells, security issues
- Shows results on PRs
- Zero configuration needed

---

## GitHub Pages Setup (Optional - for E2E test reports)

1. Go to GitHub repo → Settings → Pages
2. Source: "GitHub Actions"
3. Save
4. Add repository variable: Settings → Variables → Actions → New variable
   - Name: `PAGES_ENABLED`
   - Value: `true`

After setup, test reports will be published at:
https://mhotsiy.github.io/fintech-platform/
