# FintechPlatform

A production-ready fintech payment platform built with .NET 8, following Clean Architecture and Domain-Driven Design principles.

## 🚀 Quick Start

### Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (optional, for local development)

### Run Everything with Docker

```bash
# Start all services
docker-compose up -d

# Check status
docker-compose ps
```

### 🌐 Access URLs

- **API**: http://localhost:5153
- **API Documentation (Swagger)**: http://localhost:5153/swagger
- **Admin UI**: http://localhost:5173
- **Kafka UI**: http://localhost:8080
- **Grafana Dashboard**: http://localhost:3000 (admin/admin)
- **Prometheus Metrics**: http://localhost:9090

## 🏗️ Tech Stack

- **.NET 8** - ASP.NET Core Web API
- **PostgreSQL 16** - Database with automatic migrations
- **Kafka + Zookeeper** - Event streaming
- **React + TypeScript** - Admin UI
- **Prometheus + Grafana** - Monitoring
- **Docker** - Containerized services

## 🎯 Key Features

- **Merchants & Payments** - Full payment processing lifecycle
- **Multi-Currency Balances** - Real-time balance tracking
- **Event-Driven** - Kafka-based async processing
- **Financial Safety** - ACID transactions, optimistic locking, no negative balances
- **Admin UI** - Web interface for operations and monitoring
- **Monitoring** - Grafana dashboards and Prometheus metrics

## 📊 Database Schema

### Tables
- `merchants` - Merchant accounts
- `payments` - Payment transactions
- `balances` - Merchant balances (optimistic locking with version field)
- `withdrawals` - Withdrawal requests
- `ledger_entries` - Immutable transaction log

### Key Constraints
- **UNIQUE**: Email per merchant, one balance per merchant+currency
- **CHECK**: Positive amounts, non-negative balances
- **FOREIGN KEYS**: Referential integrity across all tables

## 🧪 Testing the Event-Driven System

### End-to-End Payment Flow with Events

1. **Create a Merchant**
```bash
curl -X POST http://localhost:5153/api/merchants \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Coffee Shop Inc",
    "email": "payments@coffeeshop.com"
  }'
```

Save the returned `id` (merchant GUID).

2. **Create a Payment** (publishes `PaymentCreatedEvent`)
```bash
curl -X POST http://localhost:5153/api/merchants/{merchant-id}/payments \
  -H "Content-Type: application/json" \
  -d '{
    "amountInMinorUnits": 5000,
    "currency": "USD",
    "externalReference": "ORDER-001",
    "description": "Latte order"
  }'
```

Save the returned payment `id`.

3. **Complete the Payment** (publishes `PaymentCompletedEvent`)
```bash
curl -X POST http://localhost:5153/api/merchants/{merchant-id}/payments/{payment-id}/complete
```

4. **View Events in Kafka UI**
- Open http://localhost:8080
- Navigate to **Topics** → **payment-events**
- Click **Messages** tab
- You should see 2 events:
  - `PaymentCreatedEvent` with event details
  - `PaymentCompletedEvent` with balance update

5. **Check Consumer Logs**
```bash
# In terminal where API is running, you should see:
# [PaymentEventConsumer] Payment created: PaymentId=..., Amount=50.00 USD
# [PaymentEventConsumer] Payment completed: PaymentId=..., NewBalance=50.00
```

### Kafka Commands

**List Topics**
```bash
docker exec -it fintechplatform-kafka kafka-topics \
  --list \
  --bootstrap-server localhost:9092
```

**View Messages in Topic**
```bash
docker exec -it fintechplatform-kafka kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  -🧪 Quick Test

Create a merchant and payment using the Admin UI at http://localhost:5173 or via API:

```bash
# Create a merchant
curl -X POST http://localhost:5153/api/merchants \
  -H "Content-Type: application/json" \
  -d '{"name":"Test Shop","email":"test@example.com"}'

# Create a payment (use merchant ID from above)
curl -X POST http://localhost:5153/api/merchants/{merchantId}/payments \
  -H "Content-Type: application/json" \
  -d '{"amountInMinorUnits":5000,"currency":"USD","description":"Test payment"}'
```Useful Commands

```bash
# View logs
docker-compose logs -f api
docker-compose logs -f workers

# Stop all services
docker-compose down

# Restart with fresh data
docker-compose down -v && docker-compose up -d

# Check service health
docker-compose ps
```

## 📚 Architecture

- **Clean Architecture** - Layered design with clear separation
- **Domain-Driven Design** - Rich domain models
- **Event Sourcing** - Immutable ledger for audit trail
- **CQRS Pattern** - Command/Query separation

For detailed documentation, see the `/docs` folder.