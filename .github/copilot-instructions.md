# FintechPlatform - Copilot Instructions

This is a fintech payment platform built with .NET 8.

## Architecture
- **Layered Architecture**: Clean separation between API, Domain, and Infrastructure
- **Domain-Driven Design**: Business logic isolated from IO operations
- **ACID Transactions**: All balance operations must be atomic

## Critical Rules
- All monetary values stored as integers (minor units, e.g., cents)
- Prevent negative balances at all costs
- Prevent double spending through proper transaction isolation
- Payment creation + balance update must be atomic
- Ledger must be rebuildable from transaction history

## Tech Stack
- .NET 8 / C#
- ASP.NET Web API
- Postgres database
- Dapper + EF Core
- Flyway migrations
- Docker for local development

## Code Standards
- No stubs, TODOs, or placeholder code
- Async/await for all IO operations
- Proper error handling and validation
- Descriptive naming conventions
- Dependency injection throughout
