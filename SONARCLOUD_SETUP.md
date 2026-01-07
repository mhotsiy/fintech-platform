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
