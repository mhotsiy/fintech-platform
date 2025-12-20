# Integration Testing with WebApplicationFactory

## Overview

This guide explains our approach to API integration testing using **WebApplicationFactory** in .NET. It covers when to use in-memory tests vs post-deployment tests, and provides practical examples from our fintech platform.

---

## What is WebApplicationFactory?

WebApplicationFactory is a built-in .NET testing tool that allows you to:
- Run your API **in-memory** without deploying it
- Test HTTP endpoints without starting a real web server
- Use a test database (SQLite in-memory or real PostgreSQL)
- Get fast feedback during development

### Key Benefits
- ✅ **No deployment needed** - Tests run the API directly in the test process
- ✅ **Fast execution** - No network overhead, everything runs in-memory
- ✅ **Isolated** - Each test gets a fresh database
- ✅ **Realistic** - Tests actual HTTP requests/responses and database operations
- ✅ **Integrated** - Part of CI/CD pipeline, runs on every commit

---

## Testing Pyramid

```
┌─────────────────────────────────────────────┐
│ E2E Tests (10 tests)                        │  After Deployment
│ - Critical user flows                       │  Slow, realistic
│ - Infrastructure validation                 │  
├─────────────────────────────────────────────┤
│ Integration Tests (12 tests)               │  In-Memory
│ - API + Database together                   │  Medium speed
│ - WebApplicationFactory                     │
├─────────────────────────────────────────────┤
│ Unit Tests (55 tests)                       │  In-Memory
│ - Business logic                            │  Fast
│ - Domain entities                           │
│ - Service layer (with mocks)                │
└─────────────────────────────────────────────┘
```

---

## In-Memory Tests vs Post-Deployment Tests

### In-Memory Integration Tests

**When to Use:**
- ✅ Testing business logic integration
- ✅ Verifying API endpoints work correctly
- ✅ Ensuring database operations are correct
- ✅ Validating request/response models
- ✅ Checking data integrity (saves, updates, deletes)
- ✅ Testing transaction atomicity

**What You Get:**
- Fast feedback (runs in seconds)
- No external dependencies
- Perfect for CI/CD pipelines
- Catches integration bugs early

**Example:**
```csharp
[Fact]
public async Task CreatePayment_ValidData_SavesToDatabase()
{
    // Arrange
    var merchant = await CreateMerchant("Stripe Inc");
    var request = new CreatePaymentRequest
    {
        MerchantId = merchant.Id,
        AmountInMinorUnits = 10000,
        Currency = "USD"
    };

    // Act
    var response = await _client.PostAsJsonAsync("/api/payments", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    
    // Verify database state
    var db = _factory.GetDbContext();
    var payment = await db.Payments.FindAsync(paymentId);
    payment.Should().NotBeNull();
    payment.AmountInMinorUnits.Should().Be(10000);
}
```

### Post-Deployment E2E Tests

**When to Use:**
- ✅ Testing against deployed environment (QA/Staging/Production)
- ✅ Verifying deployment configuration
- ✅ Checking HTTPS/SSL certificates
- ✅ Testing CORS policies
- ✅ Validating authentication/authorization
- ✅ Performance testing (response times)
- ✅ External API integrations
- ✅ Critical user journeys

**What You Get:**
- Confidence that deployment succeeded
- Real environment validation
- Integration with external systems
- Production-like scenarios

**Example:**
```csharp
[Fact]
public async Task CompletePaymentFlow_EndToEnd()
{
    var client = new RestClient("https://api.yourfintech.com");
    
    // Create merchant
    var createMerchant = new RestRequest("/api/merchants", Method.Post);
    var merchantResponse = await client.ExecuteAsync(createMerchant);
    
    // Create payment
    var createPayment = new RestRequest("/api/payments", Method.Post);
    var paymentResponse = await client.ExecuteAsync(createPayment);
    
    // Complete payment
    var complete = new RestRequest($"/api/payments/{id}/complete", Method.Put);
    var completeResponse = await client.ExecuteAsync(complete);
    
    // Verify balance updated
    completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

---

## Decision Matrix

| Scenario | In-Memory Test | Post-Deployment Test |
|----------|----------------|----------------------|
| All validation rules | ✅ YES (unit tests) | ❌ NO |
| API returns correct status codes | ✅ YES | ✅ YES (smoke test) |
| Data saves to database | ✅ YES | ✅ YES (verify) |
| Transactions are atomic | ✅ YES | ✅ YES (critical flows) |
| Request/response DTOs work | ✅ YES | ❌ NO (tested in-memory) |
| HTTPS works | ❌ NO | ✅ YES |
| CORS configured correctly | ❌ NO | ✅ YES |
| Authentication tokens valid | ⚠️ Mock in-memory | ✅ YES (real tokens) |
| Response time < 200ms | ❌ NO (not realistic) | ✅ YES |
| External API calls | ⚠️ Mock in-memory | ✅ YES (real calls) |
| Database migrations applied | ❌ NO | ✅ YES |
| Connection strings correct | ❌ NO | ✅ YES |

---

## Our Project Structure

```
tests/
├── FintechPlatform.Tests/              # Unit Tests (55 tests)
│   ├── Domain/Entities/                # Business logic
│   │   ├── MerchantTests.cs
│   │   ├── PaymentTests.cs
│   │   └── BalanceTests.cs
│   └── Api/Services/                   # Service layer
│       └── MerchantServiceTests.cs
│
└── FintechPlatform.IntegrationTests/   # Integration Tests (12 tests)
    ├── TestWebApplicationFactory.cs    # Test setup
    └── Controllers/
        ├── MerchantsControllerTests.cs
        └── PaymentsControllerTests.cs
```

---

## Test Coverage Strategy

### Unit Tests (55 tests)
**Purpose:** Test business logic in isolation

**Cover:**
- ✅ All validation rules
- ✅ All state transitions
- ✅ All edge cases
- ✅ Calculations and business rules

**Example:**
```csharp
[Fact]
public void Debit_InsufficientFunds_Throws()
{
    var balance = new Balance(merchantId, "USD");
    balance.AddToAvailableBalance(1000);

    Action act = () => balance.DeductFromAvailableBalance(2000);

    act.Should().Throw<InvalidOperationException>()
        .WithMessage("*insufficient*");
}
```

### Integration Tests (12 tests)
**Purpose:** Test API + Database integration

**Cover:**
- ✅ One happy path per endpoint
- ✅ One error path per endpoint
- ✅ Database operations work
- ✅ Transactions are atomic
- ✅ Data integrity (large numbers, special characters)
- ✅ Foreign key constraints
- ✅ Concurrency control

**Example:**
```csharp
[Fact]
public async Task CompletePayment_UpdatesBalanceAtomically()
{
    var merchant = await CreateMerchant("Stripe Inc");
    var payment = await CreatePayment(merchant.Id, 10000);
    
    var response = await _client.PutAsync($"/api/payments/{payment.Id}/complete", null);
    
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    
    // Verify BOTH payment AND balance updated (ACID test)
    var dbPayment = await _db.Payments.FindAsync(payment.Id);
    var dbBalance = await _db.Balances.FirstOrDefaultAsync(b => 
        b.MerchantId == merchant.Id && b.Currency == "USD");
    
    dbPayment.Status.Should().Be(PaymentStatus.Completed);
    dbBalance.AvailableBalanceInMinorUnits.Should().Be(10000);
}
```

### E2E Tests (5-10 tests)
**Purpose:** Validate deployment and critical flows

**Cover:**
- ✅ Critical user journeys
- ✅ Payment processing end-to-end
- ✅ Merchant onboarding flow
- ✅ HTTPS and CORS
- ✅ Performance SLAs
- ✅ Health checks

---

## Technology Stack

| Component | Technology | Purpose |
|-----------|------------|---------|
| Test Framework | xUnit | Standard .NET testing framework |
| API Testing | WebApplicationFactory | In-memory API hosting |
| Database (Tests) | SQLite In-Memory | Fast, isolated test database |
| Database (Production) | PostgreSQL | Real production database |
| Assertions | FluentAssertions | Readable test assertions |
| Mocking | Moq | Mock dependencies in unit tests |
| E2E Testing | RestSharp | Real HTTP calls to deployed API |

---

## SQLite vs PostgreSQL in Tests

### SQLite In-Memory (Current Setup)

**Advantages:**
- ✅ No database server needed
- ✅ Super fast (everything in RAM)
- ✅ No Docker required
- ✅ Perfect isolation
- ✅ Auto cleanup

**Disadvantages:**
- ⚠️ Not 100% identical to PostgreSQL
- ⚠️ Might miss PostgreSQL-specific bugs

**When to Use:**
- 90% of integration tests
- Fast feedback during development
- CI/CD pipelines

### Real PostgreSQL

**Advantages:**
- ✅ Tests actual production database
- ✅ Catches PostgreSQL-specific issues
- ✅ More realistic

**Disadvantages:**
- ❌ Requires Docker or database server
- ❌ Slower
- ❌ Need cleanup between tests

**When to Use:**
- Critical transaction tests
- Final validation before release
- Tagged tests: `[Trait("Category", "PostgreSQL")]`

---

## Running Tests

### All Tests (No Docker Required)
```bash
# Run all unit and integration tests
dotnet test

# Runs in < 5 seconds
# 67 tests total (55 unit + 12 integration)
```

### Unit Tests Only
```bash
dotnet test tests/FintechPlatform.Tests/FintechPlatform.Tests.csproj

# 55 tests, < 1 second
```

### Integration Tests Only
```bash
dotnet test tests/FintechPlatform.IntegrationTests/FintechPlatform.IntegrationTests.csproj

# 12 tests, ~3 seconds
```

### With PostgreSQL (Optional)
```bash
# Start database
docker-compose up -d

# Run PostgreSQL-specific tests
dotnet test --filter Category=PostgreSQL

# Cleanup
docker-compose down
```

---

## Best Practices

### ✅ DO

1. **Test one thing per test**
   ```csharp
   [Fact] // Good - focused
   public async Task CreateMerchant_ValidData_Returns201() { }
   ```

2. **Use realistic test data**
   ```csharp
   var merchant = new Merchant("Stripe Inc", "dev@stripe.com");
   // NOT: "Test Company", "test@test.com"
   ```

3. **Verify database state in integration tests**
   ```csharp
   var dbMerchant = await _db.Merchants.FindAsync(id);
   dbMerchant.Should().NotBeNull();
   ```

4. **Test critical paths thoroughly**
   ```csharp
   [Fact]
   public async Task CompletePayment_UpdatesBalanceAtomically() { }
   ```

5. **Keep tests independent**
   - Each test should work in isolation
   - Don't rely on test execution order

### ❌ DON'T

1. **Don't re-test business logic in integration tests**
   ```csharp
   // Bad - already tested in unit tests
   [Theory]
   [InlineData(0), InlineData(-100), InlineData(-999)]
   public async Task CreatePayment_InvalidAmounts_Returns400(long amount) { }
   ```

2. **Don't test every validation rule in integration tests**
   - Test representative cases only
   - Unit tests cover all edge cases

3. **Don't share state between tests**
   ```csharp
   // Bad - shared state
   private static Merchant _merchant;
   
   // Good - create fresh data per test
   var merchant = await CreateMerchant("Stripe Inc");
   ```

4. **Don't test external APIs in unit/integration tests**
   - Mock external calls
   - Test real calls in E2E tests only

---

## Common Patterns

### Creating Test Data
```csharp
private async Task<MerchantResponse> CreateMerchant(string name, string email)
{
    var request = new CreateMerchantRequest { Name = name, Email = email };
    var response = await _client.PostAsJsonAsync("/api/merchants", request);
    return (await response.Content.ReadFromJsonAsync<MerchantResponse>())!;
}
```

### Testing ACID Transactions
```csharp
[Fact]
public async Task CompletePayment_RollsBackOnError()
{
    // Create payment
    var payment = await CreatePayment(merchantId, 10000);
    
    // Force an error condition
    // ...
    
    // Verify NOTHING changed (transaction rolled back)
    var dbPayment = await _db.Payments.FindAsync(payment.Id);
    dbPayment.Status.Should().Be(PaymentStatus.Pending);
}
```

### Testing Concurrency
```csharp
[Fact]
public async Task CompletePayment_Concurrent_HandledSafely()
{
    var payment = await CreatePayment(merchantId, 10000);
    
    // Simulate two simultaneous requests
    var task1 = _client.PutAsync($"/api/payments/{payment.Id}/complete", null);
    var task2 = _client.PutAsync($"/api/payments/{payment.Id}/complete", null);
    
    var results = await Task.WhenAll(task1, task2);
    
    // One succeeds, one fails
    results.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(1);
}
```

---

## CI/CD Integration

### GitHub Actions Example
```yaml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Run tests
        run: dotnet test --no-restore --verbosity normal
```

**No Docker required!** Tests run entirely in-memory.

---

## Troubleshooting

### Test Fails Locally But Passes in CI
- Check for shared state between tests
- Ensure tests don't depend on execution order
- Verify no hardcoded paths or environment-specific settings

### SQLite Behaves Differently Than PostgreSQL
- Use PostgreSQL for critical tests: `[Trait("Category", "PostgreSQL")]`
- Most differences are minor (string comparison, date handling)

### Tests Are Slow
- Check if you're using real PostgreSQL instead of SQLite
- Ensure tests are truly isolated (not waiting on locks)
- Consider parallel test execution

---

## Resources

- [WebApplicationFactory Documentation](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Testing Best Practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)

---

## Questions?

For questions or improvements to this guide, contact the development team or create a ticket in Jira.

**Last Updated:** December 20, 2025
