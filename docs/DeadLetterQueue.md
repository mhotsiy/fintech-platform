# Dead Letter Queue (DLQ) Implementation

## Overview

The Dead Letter Queue (DLQ) is a critical component for handling failed event processing in our event-driven architecture. It ensures that:
- No events are lost when processing failures occur
- Failed events can be analyzed and debugged
- The main event queue doesn't get blocked by poison messages
- Transient vs permanent failures are handled appropriately

## Architecture

```
┌─────────────────┐
│   payment-events│
│      Topic      │
└────────┬────────┘
         │
         ▼
┌────────────────────┐
│ FraudDetection     │
│    Worker          │
└────────┬───────────┘
         │
    ┌────┴────┐
    │ Success │ → Commit offset
    └─────────┘
         │
    ┌────┴────────┐
    │   Failure   │
    └────┬────────┘
         │
    ┌────▼────┐
    │  Retry  │ (up to 3 attempts with exponential backoff)
    └────┬────┘
         │
    ┌────▼────────────┐
    │ Max Retries     │
    │   Exceeded      │
    └────┬────────────┘
         │
         ▼
┌─────────────────────┐
│   dead-letter-queue │
│      Topic          │
└─────────────────────┘
         │
         ▼
   (Manual analysis
    & reprocessing)
```

## Error Categorization

### Permanent Failures (Immediate DLQ)
These errors indicate the event will never succeed, so retrying is pointless:

1. **JsonException** - Malformed JSON that can't be deserialized
   - Example: `{"amountInMinorUnits": "not a number"}`
   - Action: Send to DLQ immediately, fix producer

2. **Missing Event Headers** - Event without required `event-type` header
   - Example: Message with no headers
   - Action: Send to DLQ, fix event publisher

3. **Schema Violations** - Event doesn't match expected structure
   - Example: `PaymentCreatedEvent` missing `PaymentId` field
   - Action: Send to DLQ, version mismatch or producer bug

### Transient Failures (Retry with Backoff)
These errors might resolve on their own after a delay:

1. **InvalidOperationException** - Business logic errors
   - Example: "Balance not found for merchant" (merchant created but balance record pending)
   - Action: Retry 3 times with exponential backoff (2s, 4s, 8s)

2. **Database Connection Errors** - Temporary connectivity issues
   - Example: `Npgsql.NpgsqlException: connection refused`
   - Action: Retry with backoff

3. **Unknown Exceptions** - Unexpected errors
   - Example: Any exception not explicitly categorized
   - Action: Retry with backoff, then DLQ for investigation

## Retry Strategy

### Configuration
```csharp
private const int MAX_RETRY_ATTEMPTS = 3;
```

### Exponential Backoff
- **Attempt 1**: Immediate processing
- **Attempt 2**: Wait 2 seconds
- **Attempt 3**: Wait 4 seconds  
- **Attempt 4**: Wait 8 seconds
- **After Attempt 4**: Send to DLQ

### Retry Tracking
Each message is tracked by key: `"{topic}:{partition}:{offset}"`

```csharp
private class RetryInfo
{
    public DateTime FirstAttemptTime { get; set; }
    public DateTime LastAttemptTime { get; set; }
    public int RetryCount { get; set; }
}
```

## DLQ Message Format

### Headers
```json
{
  "original-topic": "payment-events",
  "original-event-type": "PaymentCreated",
  "failure-reason": "InvalidOperationException",
  "retry-count": "3",
  "consumer-group": "fintechplatform-fraud-detection",
  "dlq-timestamp": "2026-01-02T10:30:45.1234567Z"
}
```

### Message Body
```json
{
  "originalTopic": "payment-events",
  "eventType": "PaymentCreated",
  "eventPayload": "{\"eventId\":\"...\",\"paymentId\":\"...\"}",
  "failureReason": "InvalidOperationException",
  "exceptionDetails": "Balance not found for merchant...\n\nStack trace...",
  "retryCount": 3,
  "firstFailedAt": "2026-01-02T10:30:30Z",
  "lastFailedAt": "2026-01-02T10:30:45Z",
  "consumerGroup": "fintechplatform-fraud-detection",
  "originalPartition": 0,
  "originalOffset": 1234,
  "sentToDlqAt": "2026-01-02T10:30:45.5Z"
}
```

## Monitoring DLQ

### View DLQ Messages in Kafka UI
1. Open http://localhost:8080
2. Navigate to **Topics** → **dead-letter-queue**
3. Click **Messages** tab
4. Review failed events

### CLI Commands

**List DLQ messages:**
```bash
docker exec -it fintechplatform-kafka kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  --topic dead-letter-queue \
  --from-beginning \
  --property print.headers=true
```

**Count DLQ messages:**
```bash
docker exec -it fintechplatform-kafka kafka-run-class \
  kafka.tools.GetOffsetShell \
  --broker-list localhost:9092 \
  --topic dead-letter-queue \
  --time -1
```

## Reprocessing Failed Events

### Manual Reprocessing
1. Analyze the failed event in DLQ
2. Fix the underlying issue (e.g., create missing merchant, fix balance)
3. Extract the `eventPayload` from DLQ message
4. Republish to original topic:

```bash
echo '{"eventId":"...","paymentId":"..."}' | \
  docker exec -i fintechplatform-kafka kafka-console-producer \
    --bootstrap-server localhost:9092 \
    --topic payment-events \
    --property "parse.headers=true" \
    --property "headers.delimiter=|" \
    --property "header.event-type=PaymentCreated"
```

### Automated Reprocessing (Future Enhancement)
Create a DLQ replay worker that:
1. Consumes from `dead-letter-queue`
2. Applies fixes based on `failureReason`
3. Republishes to original topic
4. Tracks replay attempts to prevent infinite loops

## Testing DLQ

### Test 1: Malformed JSON (Immediate DLQ)
```bash
# Publish invalid JSON
echo '{"malformed json' | \
  docker exec -i fintechplatform-kafka kafka-console-producer \
    --bootstrap-server localhost:9092 \
    --topic payment-events
```

**Expected**: Message sent to DLQ immediately with `JsonException`

### Test 2: Missing Balance (Retry Then DLQ)
```bash
# Create payment for merchant without balance
curl -X POST http://localhost:5153/api/merchants/{merchant-id}/payments \
  -H "Content-Type: application/json" \
  -d '{"amountInMinorUnits": 1000, "currency": "EUR"}'
```

**Expected**: 
- Worker retries 3 times (2s, 4s, 8s delays)
- After attempt 4, sent to DLQ with `InvalidOperationException`

### Test 3: Successful Retry
```bash
# 1. Create merchant without balance
# 2. Create payment (will start retrying)
# 3. Quickly create EUR balance for merchant
# Expected: Worker retries succeed before max attempts
```

## Production Considerations

### Alerts
Configure alerts for:
- **High DLQ rate**: More than 1% of messages going to DLQ
- **DLQ growth**: DLQ topic size increasing rapidly
- **DLQ publish failures**: CRITICAL - DLQ itself is failing

### Retention
```yaml
# kafka topic config
retention.ms: 604800000  # 7 days
retention.bytes: 10737418240  # 10 GB
```

### Security
- Restrict DLQ topic access (read-only for most services)
- Log all DLQ publications for audit trail
- Encrypt sensitive data in DLQ messages

### Metrics to Track
- `dlq_messages_total` - Total messages sent to DLQ
- `dlq_messages_by_reason` - Breakdown by failure reason
- `retry_attempts_total` - Total retry attempts
- `retry_success_rate` - % of retries that succeed

## Common Scenarios

### Scenario 1: Database Down
- **Symptom**: All workers sending messages to DLQ with DB connection errors
- **Action**: Fix database, replay DLQ messages
- **Prevention**: Health checks, circuit breakers

### Scenario 2: Schema Change
- **Symptom**: Sudden spike in `JsonException` DLQ messages
- **Action**: Roll back producer, fix schema compatibility
- **Prevention**: Schema registry, versioned events

### Scenario 3: Missing Reference Data
- **Symptom**: `InvalidOperationException: Balance not found` in DLQ
- **Action**: Investigate merchant creation process, add missing balances
- **Prevention**: Atomic merchant+balance creation (already implemented!)

## Best Practices

1. **Monitor DLQ size daily** - should be near-zero in healthy systems
2. **Review DLQ weekly** - look for patterns in failures
3. **Never ignore DLQ** - every message represents lost business logic
4. **Document failure reasons** - update this doc when new patterns emerge
5. **Test DLQ flows** - include DLQ tests in CI/CD pipeline

## See Also
- [Kafka Testing Guide](./KafkaTestingGuide.md)
- [Event System Summary](./Phase2-EventSystem-Summary.md)
- [FraudDetectionWorker.cs](../src/FintechPlatform.Workers/Workers/FraudDetectionWorker.cs)
