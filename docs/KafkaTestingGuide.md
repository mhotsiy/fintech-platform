# Kafka Event System Testing Guide

## Overview

This guide covers testing the event-driven architecture implementation in FintechPlatform, including Kafka infrastructure, event publishing, and event consumption.

## Prerequisites

- Docker Desktop running
- .NET 10 SDK installed
- All services started via `docker-compose up -d`

## 1. Infrastructure Verification

### Check All Services Are Running

```bash
docker-compose ps
```

Expected output:
```
NAME                        STATUS
fintechplatform-kafka       Up (healthy)
fintechplatform-kafka-ui    Up
fintechplatform-postgres    Up (healthy)
fintechplatform-zookeeper   Up (healthy)
```

### Access Kafka UI

1. Open browser: http://localhost:8080
2. You should see the Kafka UI dashboard
3. Click "Topics" in left menu
4. Topics `payment-events` and `withdrawal-events` will be auto-created when first message is published

### Verify Kafka is Accepting Connections

```bash
docker exec -it fintechplatform-kafka kafka-broker-api-versions \
  --bootstrap-server localhost:9092
```

Should return broker API versions without errors.

## 2. API Testing with Event Publishing

### Start the API

```bash
cd src/api
dotnet run
```

Watch the console output for:
```
[Information] Payment event consumer initialized
[Information] Withdrawal event consumer initialized
[Information] Payment event consumer starting - subscribing to payment-events topic
[Information] Withdrawal event consumer starting - subscribing to withdrawal-events topic
```

### Test Payment Creation Flow

**Step 1: Create Merchant**
```bash
curl -X POST http://localhost:5153/api/merchants \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Merchant",
    "email": "test@merchant.com"
  }'
```

**Expected Response:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Test Merchant",
  "email": "test@merchant.com",
  "isActive": true,
  "createdAt": "2026-01-01T12:00:00Z"
}
```

Save the `id` value.

**Step 2: Create Payment (Publishes PaymentCreatedEvent)**
```bash
MERCHANT_ID="<paste-merchant-id-here>"

curl -X POST http://localhost:5153/api/merchants/$MERCHANT_ID/payments \
  -H "Content-Type: application/json" \
  -d '{
    "amountInMinorUnits": 10000,
    "currency": "USD",
    "externalReference": "TEST-001",
    "description": "Test payment"
  }'
```

**Expected API Response:**
```json
{
  "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "merchantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "amountInMinorUnits": 10000,
  "currency": "USD",
  "status": "Pending",
  "externalReference": "TEST-001",
  "description": "Test payment",
  "createdAt": "2026-01-01T12:01:00Z"
}
```

**Expected Console Logs:**
```
[Information] Published event PaymentCreated with ID <guid> to topic payment-events at offset 0
[Information] Processing payment event: Type=PaymentCreated, Offset=0, Partition=0
[Information] Payment created: PaymentId=..., MerchantId=..., Amount=100.00 USD
```

Save the payment `id`.

**Step 3: Complete Payment (Publishes PaymentCompletedEvent)**
```bash
PAYMENT_ID="<paste-payment-id-here>"

curl -X POST http://localhost:5153/api/merchants/$MERCHANT_ID/payments/$PAYMENT_ID/complete
```

**Expected Console Logs:**
```
[Information] Completed payment <payment-id>, updated balance for merchant <merchant-id>
[Information] Published event PaymentCompleted with ID <guid> to topic payment-events at offset 1
[Information] Processing payment event: Type=PaymentCompleted, Offset=1, Partition=0
[Information] Payment completed: PaymentId=..., Amount=100.00 USD, NewBalance=100.00
```

## 3. Event Verification in Kafka UI

### View Published Events

1. Open http://localhost:8080
2. Click **Topics** → **payment-events**
3. Click **Messages** tab
4. You should see 2 messages:

**Message 1 - PaymentCreatedEvent:**
```json
{
  "eventId": "...",
  "occurredAt": "2026-01-01T12:01:00Z",
  "eventType": "PaymentCreated",
  "paymentId": "...",
  "merchantId": "...",
  "amountInMinorUnits": 10000,
  "currency": "USD",
  "externalReference": "TEST-001",
  "description": "Test payment"
}
```

**Message 2 - PaymentCompletedEvent:**
```json
{
  "eventId": "...",
  "occurredAt": "2026-01-01T12:02:00Z",
  "eventType": "PaymentCompleted",
  "paymentId": "...",
  "merchantId": "...",
  "amountInMinorUnits": 10000,
  "currency": "USD",
  "newBalanceInMinorUnits": 10000,
  "ledgerEntryId": "...",
  "completedAt": "2026-01-01T12:02:00Z"
}
```

### Inspect Message Headers

Click on a message in Kafka UI to view headers:
- `event-type`: PaymentCreated
- `occurred-at`: ISO 8601 timestamp

## 4. Consumer Testing

### Check Consumer Group Status

```bash
docker exec -it fintechplatform-kafka kafka-consumer-groups \
  --bootstrap-server localhost:9092 \
  --group fintechplatform-api \
  --describe
```

**Expected Output:**
```
GROUP                TOPIC           PARTITION  CURRENT-OFFSET  LOG-END-OFFSET  LAG
fintechplatform-api  payment-events  0          2               2               0
```

**LAG = 0** means all messages have been consumed.

### Test Consumer Resilience

**Scenario: Stop API, publish events, restart API**

1. **Stop the API** (Ctrl+C in terminal)

2. **Publish more events** (create + complete another payment via curl)

3. **Check consumer lag:**
```bash
docker exec -it fintechplatform-kafka kafka-consumer-groups \
  --bootstrap-server localhost:9092 \
  --group fintechplatform-api \
  --describe
```

You should see `LAG > 0` (unconsumed messages).

4. **Restart API:**
```bash
cd src/api
dotnet run
```

5. **Watch console** - you should see the consumer process the backlog:
```
[Information] Processing payment event: Type=PaymentCreated, Offset=2, Partition=0
[Information] Processing payment event: Type=PaymentCompleted, Offset=3, Partition=0
```

6. **Verify lag is back to 0:**
```bash
docker exec -it fintechplatform-kafka kafka-consumer-groups \
  --bootstrap-server localhost:9092 \
  --group fintechplatform-api \
  --describe
```

## 5. Event Schema Validation

### Manual Event Production (Testing)

You can manually produce events for testing:

```bash
docker exec -it fintechplatform-kafka kafka-console-producer \
  --bootstrap-server localhost:9092 \
  --topic payment-events
```

Then paste a test event:
```json
{"eventId":"123e4567-e89b-12d3-a456-426614174000","occurredAt":"2026-01-01T12:00:00Z","eventType":"PaymentCreated","paymentId":"223e4567-e89b-12d3-a456-426614174000","merchantId":"323e4567-e89b-12d3-a456-426614174000","amountInMinorUnits":5000,"currency":"USD","externalReference":"MANUAL-TEST","description":"Manual test"}
```

Press Ctrl+D to exit.

Check API logs to see consumer processing it.

## 6. Performance Testing

### Measure Event Publishing Latency

Create 10 payments rapidly:

```bash
for i in {1..10}; do
  curl -X POST http://localhost:5153/api/merchants/$MERCHANT_ID/payments \
    -H "Content-Type: application/json" \
    -d "{\"amountInMinorUnits\": 1000, \"currency\": \"USD\", \"externalReference\": \"PERF-$i\"}"
done
```

Watch API logs for publish latency:
```
[Information] Published event PaymentCreated with ID ... to topic payment-events at offset 4
```

Typical latency: **< 50ms** per event on local Kafka.

### Check Consumer Throughput

```bash
docker exec -it fintechplatform-kafka kafka-consumer-groups \
  --bootstrap-server localhost:9092 \
  --group fintechplatform-api \
  --describe
```

All messages should be consumed within seconds.

## 7. Error Handling Testing

### Simulate Kafka Outage

1. **Stop Kafka:**
```bash
docker-compose stop kafka
```

2. **Try to create payment:**
```bash
curl -X POST http://localhost:5153/api/merchants/$MERCHANT_ID/payments \
  -H "Content-Type: application/json" \
  -d '{"amountInMinorUnits": 1000, "currency": "USD"}'
```

**Expected:** API will return error:
```json
{
  "error": "Failed to publish event to Kafka: ..."
}
```

Payment **should NOT be saved** to database (transaction rollback).

3. **Restart Kafka:**
```bash
docker-compose start kafka
```

Wait 30 seconds for broker to be ready.

4. **Retry payment creation** - should succeed.

## 8. Troubleshooting

### API Not Connecting to Kafka

**Check bootstrap servers:**
```bash
cat src/api/appsettings.json | grep BootstrapServers
```

Should be: `"BootstrapServers": "localhost:9092"`

### Events Not Being Consumed

**Check consumer logs:**
```bash
# In API console, look for:
[Information] Payment event consumer starting - subscribing to payment-events topic
```

If missing, check DI registration in `Program.cs`.

### No Topics in Kafka UI

Topics are auto-created on first publish. If you don't see topics:

1. Publish at least one event (create a payment)
2. Refresh Kafka UI
3. Topics should appear

### Consumer Lag Increasing

If lag keeps increasing, consumer is falling behind. Check:
- Consumer logs for errors
- Database query performance
- Network latency

## 9. Success Criteria

✅ All docker services healthy
✅ API starts without errors
✅ Payment creation publishes `PaymentCreatedEvent`
✅ Payment completion publishes `PaymentCompletedEvent`
✅ Events visible in Kafka UI with correct schema
✅ Consumers process events and log details
✅ Consumer lag remains at 0
✅ Events persist after API restart
✅ Error handling gracefully handles Kafka outages

## Next Steps

Once basic event flow is working:

1. Implement transactional outbox pattern (Phase 4)
2. Add event versioning and schema registry
3. Implement DLQ (dead letter queue) for failed events
4. Add OpenTelemetry tracing across event flow (Phase 3)
5. Set up consumer lag alerts
6. Implement idempotency keys for exactly-once processing
