# Monitoring Stack - Prometheus + Grafana

## Overview

Full production-grade monitoring for the Fintech Platform using Prometheus (metrics collection/storage) and Grafana (visualization/dashboards).

## Architecture

```
┌──────────────────┐
│  Workers         │
│  (Port 5000)     │  Exposes /metrics endpoint
│                  │
│  - Prometheus    │  Counter: payments_processed_total
│    metrics       │  Counter: payments_approved_total
│                  │  Counter: dlq_messages_total
└────────┬─────────┘  Histogram: payment_processing_duration_seconds
         │
         │ HTTP GET /metrics every 15s
         ▼
┌──────────────────┐
│  Prometheus      │
│  (Port 9090)     │  Time-series database
│                  │  Stores metrics history
│                  │  Evaluates alert rules
└────────┬─────────┘
         │
         │ PromQL queries
         ▼
┌──────────────────┐
│  Grafana         │
│  (Port 3000)     │  Beautiful dashboards
│                  │  Graphs, alerts, notifications
│  admin/admin     │
└──────────────────┘
```

## Services

### Prometheus (http://localhost:9090)
- Scrapes Workers `/metrics` endpoint every 15 seconds
- Stores all metrics in time-series database
- Provides PromQL query interface
- Evaluates alert rules

### Grafana (http://localhost:3000)
- **Username:** admin
- **Password:** admin
- Pre-configured dashboard: "Fintech Platform - Payment Processing"
- Auto-refreshes every 5 seconds

## Metrics Tracked

### Payment Processing
- `payments_processed_total{status}` - Total payments by status (success, retrying, json_error, etc.)
- `payments_approved_total` - Payments approved by fraud detection
- `payments_flagged_total{reason}` - Payments flagged for manual review
- `payments_auto_completed_total` - Payments automatically completed

### Dead Letter Queue
- `dlq_messages_total{failure_reason}` - Messages sent to DLQ by reason
- `retry_attempts_total` - Total retry attempts

### Performance
- `payment_processing_duration_seconds` - Histogram of processing time
  - p50 (median)
  - p95 (95th percentile)
  - p99 (99th percentile)

### Kafka Consumer
- `kafka_consumer_lag{topic,partition}` - How far behind the consumer is

## Getting Started

### 1. Start All Services
```bash
docker-compose up -d
```

Wait ~30 seconds for all services to be healthy.

### 2. Verify Prometheus
```bash
# Check Prometheus is scraping Workers
curl http://localhost:9090/api/v1/targets
```

You should see `workers` target with state `UP`.

### 3. View Raw Metrics
```bash
# See what metrics Workers are exposing
curl http://localhost:5000/metrics
```

Example output:
```
# HELP payments_processed_total Total number of payment events processed
# TYPE payments_processed_total counter
payments_processed_total{status="success"} 42
payments_processed_total{status="retrying"} 3

# HELP payments_approved_total Total number of payments approved by fraud detection
# TYPE payments_approved_total counter
payments_approved_total 40

# HELP payment_processing_duration_seconds Time spent processing each payment event
# TYPE payment_processing_duration_seconds histogram
payment_processing_duration_seconds_bucket{le="0.1"} 35
payment_processing_duration_seconds_bucket{le="0.2"} 40
payment_processing_duration_seconds_bucket{le="+Inf"} 42
payment_processing_duration_seconds_sum 12.5
payment_processing_duration_seconds_count 42
```

### 4. Open Grafana Dashboard
1. Open http://localhost:3000
2. Login: `admin` / `admin`
3. Go to **Dashboards** → **Fintech Platform - Payment Processing**
4. Watch real-time metrics!

## Dashboard Panels

### Top Row
- **Payments Processed (Total)** - Rate of payment processing by status
- **Approval Rate** - % of payments auto-approved (green = good)

### Middle Row
- **Processing Duration (p50, p95, p99)** - How fast payments are processed
- **Payments Flagged for Review** - Breakdown by flagging reason

### Bottom Row
- **Dead Letter Queue Messages** - Failed events going to DLQ (should be near zero!)
- **Retry Attempts** - How often we're retrying failed messages

### Stats Row
- **Total Payments Processed** - All-time count
- **Total Approved** - Auto-approved count (green)
- **Total Flagged** - Flagged for review (yellow)
- **Total DLQ Messages** - Sent to DLQ (red - investigate!)

## Testing the Monitoring

### 1. Start Workers
```bash
cd src/FintechPlatform.Workers
dotnet run
```

Watch console logs - you'll see:
```
info: Prometheus.HttpMetrics.HttpMetricsMiddleware[0]
      Metric server is listening on port 5000
```

### 2. Process Some Payments
```bash
# Create merchant
MERCHANT_ID=$(curl -s -X POST http://localhost:5153/api/merchants \
  -H "Content-Type: application/json" \
  -d '{"name": "Test Shop", "email": "test@shop.com"}' | jq -r '.id')

# Create 10 payments
for i in {1..10}; do
  curl -X POST http://localhost:5153/api/merchants/$MERCHANT_ID/payments \
    -H "Content-Type: application/json" \
    -d "{\"amountInMinorUnits\": $((RANDOM % 10000)), \"currency\": \"USD\", \"externalReference\": \"TEST-$i\"}"
  sleep 1
done
```

### 3. Watch Metrics Update
- Refresh Grafana dashboard
- See "Payments Processed" graph spike
- See "Approval Rate" update
- See processing duration histogram

## PromQL Queries

### Useful Queries (Run in Prometheus)

**Payment processing rate (last 5 minutes):**
```promql
rate(payments_processed_total[5m])
```

**Approval rate percentage:**
```promql
rate(payments_approved_total[5m]) / rate(payments_processed_total{status="success"}[5m]) * 100
```

**95th percentile processing time:**
```promql
histogram_quantile(0.95, rate(payment_processing_duration_seconds_bucket[5m]))
```

**DLQ rate (should be ~0):**
```promql
rate(dlq_messages_total[5m])
```

**Payments flagged in last hour:**
```promql
increase(payments_flagged_total[1h])
```

## Alerts (Future Enhancement)

### Recommended Alerts

**High DLQ Rate:**
```yaml
- alert: HighDLQRate
  expr: rate(dlq_messages_total[5m]) > 0.1
  for: 5m
  annotations:
    summary: "High Dead Letter Queue rate detected"
    description: "{{ $value }} messages/sec going to DLQ"
```

**Low Approval Rate:**
```yaml
- alert: LowApprovalRate
  expr: (rate(payments_approved_total[5m]) / rate(payments_processed_total[5m])) < 0.8
  for: 10m
  annotations:
    summary: "Approval rate below 80%"
```

**Slow Processing:**
```yaml
- alert: SlowProcessing
  expr: histogram_quantile(0.95, rate(payment_processing_duration_seconds_bucket[5m])) > 1
  for: 5m
  annotations:
    summary: "p95 processing time above 1 second"
```

## Troubleshooting

### Prometheus Can't Scrape Workers
```bash
# Check Workers are exposing metrics
curl http://localhost:5000/metrics

# Check Prometheus targets
curl http://localhost:9090/api/v1/targets | jq
```

**Fix:** Make sure Workers are running and port 5000 is accessible.

### Grafana Shows "No Data"
1. Check Prometheus datasource: Configuration → Data Sources → Prometheus
2. Click "Test" - should show "Data source is working"
3. Check time range (default: last 1 hour)
4. Make sure you've processed some payments (metrics need data!)

### Metrics Not Updating
- Wait 15 seconds (scrape interval)
- Check Prometheus is running: `docker ps | grep prometheus`
- Check for errors: `docker logs fintechplatform-prometheus`

## Production Considerations

### Retention
By default, Prometheus keeps 15 days of data. For production:
```yaml
# In docker-compose.yml
command:
  - '--storage.tsdb.retention.time=30d'
  - '--storage.tsdb.retention.size=10GB'
```

### Security
- Change Grafana admin password
- Enable authentication in Prometheus
- Use HTTPS for public endpoints

### Scaling
- Use Prometheus federation for multiple Workers
- Consider Thanos for long-term storage
- Use Grafana Cloud for hosted solution

### Alerting
- Configure Alertmanager (Prometheus component)
- Integrate with PagerDuty, Slack, email
- Set up on-call rotations

## Interview Talking Points

**When showing this to recruiters:**

✅ "I implemented production-grade monitoring with Prometheus and Grafana"
✅ "Tracks key business metrics: approval rate, processing time, DLQ rate"
✅ "Uses histograms for latency percentiles (p50, p95, p99)"
✅ "Dashboard auto-refreshes every 5 seconds for real-time visibility"
✅ "Labeled metrics allow drilling down by failure reason, status, etc."
✅ "Designed for alerting - can trigger on-call for SLA violations"

## See Also
- [Prometheus Documentation](https://prometheus.io/docs/)
- [Grafana Documentation](https://grafana.com/docs/)
- [PromQL Basics](https://prometheus.io/docs/prometheus/latest/querying/basics/)
- [../docs/DeadLetterQueue.md](../docs/DeadLetterQueue.md)
