# FintechPlatform

A production-ready fintech payment platform built with .NET 8, following Clean Architecture and Domain-Driven Design principles.

## 🏗️ Architecture

- **Clean Architecture**: Separation of concerns with layered design
- **Domain-Driven Design**: Rich domain models with business logic
- **Event-Driven Architecture**: Async messaging with Kafka for scalability
- **ACID Transactions**: Atomic operations for financial safety
- **Event Sourcing**: Immutable ledger for complete audit trail

## 🚀 Tech Stack

- **.NET 8** - Modern C# with minimal APIs
- **ASP.NET Core Web API** - RESTful API framework
- **PostgreSQL 16** - Relational database
- **Entity Framework Core 10** - ORM and migrations
- **Dapper** - High-performance queries
- **Kafka** - Event streaming and async messaging
- **Docker** - Service containerization
- **Swagger/OpenAPI** - API documentation

## 📁 Project Structure

```
fintech-app/
├── src/
│   ├── api/                    # API layer (controllers, DTOs, services)
│   ├── domain/                 # Domain layer (entities, repository interfaces)
│   └── infrastructure/         # Infrastructure layer (EF Core, repositories)
├── db/
│   └── migrations/            # SQL migration scripts
├── docker-compose.yml         # PostgreSQL container setup
└── .github/
    └── copilot-instructions.md
```

## 🎯 Features

### Domain Entities
- **Merchants** - Business accounts with email validation
- **Payments** - Incoming payments with status lifecycle (Pending → Completed/Failed/Refunded)
- **Balances** - Multi-currency wallets with available/pending balances
- **Withdrawals** - Outgoing transfers to bank accounts
- **Ledger Entries** - Immutable audit log of all transactions

### Financial Safety
- ✅ Money stored as integers (minor units) - no floating-point errors
- ✅ Pessimistic locking (FOR UPDATE) - prevents race conditions
- ✅ Optimistic concurrency control - detects conflicts
- ✅ Negative balance prevention - CHECK constraints
- ✅ Atomic transactions - all-or-nothing operations
- ✅ Rebuildable ledger - can reconstruct balances from history

### Event-Driven Architecture
- ✅ **Kafka Event Streaming** - Reliable async message delivery
- ✅ **Domain Events** - PaymentCreated, PaymentCompleted, WithdrawalRequested
- ✅ **Event Consumers** - Background workers process events independently
- ✅ **At-Least-Once Delivery** - Manual offset commits prevent message loss
- ✅ **Ordered Processing** - Partition keys ensure message ordering
- ✅ **Kafka UI** - Web interface for monitoring topics and messages

### API Endpoints

#### Health
- `GET /health` - Database connectivity check

#### Merchants
- `POST /api/merchants` - Create merchant account
- `GET /api/merchants/{id}` - Get merchant by ID
- `GET /api/merchants` - Get all active merchants

#### Payments
- `POST /api/payments` - Create payment
- `GET /api/payments/{id}` - Get payment by ID
- `POST /api/payments/{id}/complete` - Complete payment and update balance

## 🛠️ Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Git](https://git-scm.com/)

### Installation

1. **Clone the repository**
```bash
git clone <your-repo-url>
cd fintech-platform
```

2. **Start all services (Postgres, Kafka, Zookeeper, Kafka UI)**
```bash
docker-compose up -d
```

Wait 30-60 seconds for Kafka to initialize fully.

3. **Verify services are running**
```bash
docker-compose ps
```

You should see: postgres, flyway, zookeeper, kafka, and kafka-ui all healthy.

4. **Apply database migrations**
```bash
dotnet ef database update --project src/infrastructure/FintechPlatform.Infrastructure.csproj --startup-project src/api/FintechPlatform.Api.csproj
```

5. **Run the API**
```bash
cd src/api
dotnet run
```

6. **Access web interfaces**
```
API Swagger UI:  http://localhost:5153/swagger
Kafka UI:        http://localhost:8080
```

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
  --topic payment-events \
  --from-beginning
```

**Check Consumer Groups**
```bash
docker exec -it fintechplatform-kafka kafka-consumer-groups \
  --bootstrap-server localhost:9092 \
  --list
```

**View Consumer Lag**
```bash
docker exec -it fintechplatform-kafka kafka-consumer-groups \
  --bootstrap-server localhost:9092 \
  --group fintechplatform-api \
  --describe
```

## 🔒 Security Considerations

- Connection strings use environment variables (not committed)
- Optimistic + pessimistic locking prevents race conditions
- Input validation on all requests
- Email format validation
- Amount validation (positive values only)

## 🗄️ Database Commands

### Connect to PostgreSQL
```bash
docker exec -it fintechplatform-postgres psql -U postgres -d fintechplatform
```

### List all tables
```bash
docker exec fintechplatform-postgres psql -U postgres -d fintechplatform -c "\dt"
```

### Query merchants
```bash
docker exec fintechplatform-postgres psql -U postgres -d fintechplatform -c "SELECT * FROM merchants;"
```

## 🏛️ Design Patterns

- **Repository Pattern** - Data access abstraction
- **Unit of Work** - Transaction coordination
- **DTO Pattern** - API contract separation
- **Factory Method** - LedgerEntry creation
- **State Pattern** - Payment lifecycle
- **Money Pattern** - Integer-based currency storage
- **Event Sourcing** - Immutable ledger
- **Aggregate Root** - Merchant as aggregate
- **Guard Clauses** - Input validation

## 📈 Development Workflow

1. Make code changes in your IDE
2. Hot reload picks up changes automatically
3. Test via Swagger UI at `http://localhost:5153/swagger`
4. Database persists in Docker volume

## 🐳 Docker Management

```bash
# Start all services (Postgres, Kafka, Zookeeper, Kafka UI)
docker-compose up -d

# View all running services
docker-compose ps

# Stop all services
docker-compose down

# Stop and remove volumes (clean slate)
docker-compose down -v

# View logs for specific service
docker-compose logs -f postgres
docker-compose logs -f kafka
docker-compose logs -f api

# Access Kafka UI
open http://localhost:8080
```

## 🧹 Maintenance

### Create new migration
```bash
dotnet ef migrations add MigrationName --project src/infrastructure/FintechPlatform.Infrastructure.csproj --startup-project src/api/FintechPlatform.Api.csproj
```

### Rollback migration
```bash
dotnet ef database update PreviousMigrationName --project src/infrastructure/FintechPlatform.Infrastructure.csproj --startup-project src/api/FintechPlatform.Api.csproj
```

### Build solution
```bash
dotnet build
```

## 📝 License

[Your License Here]

## 👥 Contributing

[Your Contributing Guidelines Here]

## 📞 Support

[Your Support Information Here]
