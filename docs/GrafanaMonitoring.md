# Grafana Monitoring System

## Overview

The fintech platform uses a modern observability stack with **Prometheus** for metrics collection and **Grafana** for visualization. This enables real-time monitoring of payment processing, fraud detection, and system health.

## Architecture

```
┌─────────────┐      ┌──────────────┐      ┌─────────────┐
│   Workers   │─────▶│  Prometheus  │─────▶│   Grafana   │
│ (Port 5002) │      │ (Port 9090)  │      │ (Port 3000) │
└─────────────┘      └──────────────┘      └─────────────┘
       │                     │                     │
    Metrics              Scraping             Dashboards
    Endpoint             Every 15s            Visualization
```

### Components

1. **Workers Service** - Exposes Prometheus metrics on `/metrics` endpoint (port 5002)
2. **Prometheus** - Scrapes metrics from Workers and stores time-series data
3. **Grafana** - Queries Prometheus and displays data in dashboards

## Implementation Details

### 1. Metrics in Workers (`FintechPlatform.Workers`)

#### Location
- [src/FintechPlatform.Workers/Workers/FraudDetectionWorker.cs](../src/FintechPlatform.Workers/Workers/FraudDetectionWorker.cs)

#### Metrics Defined

```csharp
// Counter metrics (always increasing)
private static readonly Counter PaymentsProcessedTotal = Metrics
    .CreateCounter("payments_processed_total", "Total number of payment events processed",
        new CounterConfiguration { LabelNames = new[] { "status" } });

private static readonly Counter PaymentsApprovedTotal = Metrics
    .CreateCounter("payments_approved_total", "Total number of payments approved by fraud detection");

private static readonly Counter PaymentsFlaggedTotal = Metrics
    .CreateCounter("payments_flagged_total", "Total number of payments flagged for manual review",
        new CounterConfiguration { LabelNames = new[] { "reason" } });

private static readonly Counter PaymentsAutoCompletedTotal = Metrics
    .CreateCounter("payments_auto_completed_total", "Total number of payments auto-completed");

// Histogram metrics (distribution of values)
private static readonly Histogram ProcessingDuration = Metrics
    .CreateHistogram("payment_processing_duration_seconds", "Time spent processing each payment event",
        new HistogramConfiguration
        {
            Buckets = Histogram.LinearBuckets(start: 0.1, width: 0.1, count: 10)
        });

// Gauge metrics (can go up and down)
private static readonly Gauge ActivePaymentsProcessing = Metrics
    .CreateGauge("active_payments_processing", "Number of payments currently being processed");

// Business metrics
private static readonly Counter PaymentAmountProcessedTotal = Metrics
    .CreateCounter("payment_amount_processed_total", "Total payment amount processed in minor units (cents)",
        new CounterConfiguration { LabelNames = new[] { "currency", "status" } });
```

#### Metric Types Explained

| Type | Description | Example Use Case |
|------|-------------|------------------|
| **Counter** | Only increases (or resets to zero on restart) | Total payments processed, total errors |
| **Gauge** | Can increase or decrease | Active connections, current queue size |
| **Histogram** | Samples observations and counts them in buckets | Request duration, payment amounts |

#### How Metrics are Incremented

```csharp
// In ProcessPaymentEventAsync method
PaymentsByCurrency.WithLabels(paymentCreatedEvent.Currency).Inc();
PaymentAmountDistribution.WithLabels(paymentCreatedEvent.Currency)
    .Observe(paymentCreatedEvent.AmountInMinorUnits);

if (riskAssessment.IsApproved)
{
    PaymentsApprovedTotal.Inc();
    PaymentAmountProcessedTotal.WithLabels(paymentCreatedEvent.Currency, "approved")
        .Inc(paymentCreatedEvent.AmountInMinorUnits);
    
    await AutoCompletePaymentAsync(paymentCreatedEvent.PaymentId, cancellationToken);
    PaymentsAutoCompletedTotal.Inc();
}
```

#### Metrics Server Setup

In [Program.cs](../src/FintechPlatform.Workers/Program.cs):

```csharp
// Configure Prometheus metrics server
builder.Services.AddMetricServer(options =>
{
    options.Port = 5002; // Expose metrics on http://localhost:5002/metrics
});
```

This creates an HTTP endpoint that Prometheus can scrape.

### 2. Prometheus Configuration

#### Location
- [monitoring/prometheus.yml](../monitoring/prometheus.yml)

#### Configuration

```yaml
global:
  scrape_interval: 15s     # Scrape metrics every 15 seconds
  evaluation_interval: 15s # Evaluate rules every 15 seconds

scrape_configs:
  - job_name: 'workers'
    scrape_interval: 15s
    static_configs:
      - targets: ['workers:5002']  # Docker service name + port
        labels:
          service: 'fraud-detection-worker'
```

#### How Scraping Works

1. Every 15 seconds, Prometheus sends HTTP GET request to `http://workers:5002/metrics`
2. Workers responds with metrics in Prometheus text format:
   ```
   # HELP payments_processed_total Total number of payment events processed
   # TYPE payments_processed_total counter
   payments_processed_total{status="success"} 42
   ```
3. Prometheus stores this time-series data in its database

#### Docker Networking

Since both Prometheus and Workers run in Docker Compose:
- They share the same Docker network (`fintech-platform_default`)
- Services can communicate using container names as hostnames
- `workers:5002` resolves to the Workers container's internal IP
- This bypasses WSL/Windows firewall issues

### 3. Grafana Dashboards

#### Datasource Configuration

Location: [monitoring/grafana/datasources/prometheus.yml](../monitoring/grafana/datasources/prometheus.yml)

```yaml
apiVersion: 1

datasources:
  - name: Prometheus
    type: prometheus
    access: proxy
    url: http://prometheus:9090  # Grafana queries Prometheus internally
    isDefault: true
    editable: true
```

#### Dashboard Provisioning

Location: [monitoring/grafana/dashboards/](../monitoring/grafana/dashboards/)

Dashboards are automatically loaded from JSON files:
- `payment-processing.json` - Payment processing metrics
- `system-health.json` - System health metrics

#### Dashboard Structure

Each panel in a dashboard contains:

```json
{
  "datasource": {
    "type": "prometheus",
    "uid": "PBFA97CFB590B2093"  // References the Prometheus datasource
  },
  "targets": [
    {
      "expr": "sum(payments_processed_total)",  // PromQL query
      "refId": "A"
    }
  ],
  "title": "Total Payments",
  "type": "stat"  // Panel type: stat, graph, table, etc.
}
```

#### PromQL Queries Used

| Panel | Query | Description |
|-------|-------|-------------|
| Total Payments | `sum(payments_processed_total)` | Sum all processed payments across all statuses |
| Approved Payments | `sum(payments_approved_total)` | Total approved payments |
| Flagged Payments | `sum(payments_flagged_total)` | Total flagged payments |
| Processing Rate | `rate(payments_processed_total[5m])` | Payments processed per second (5min average) |
| Active Processing | `active_payments_processing` | Current number of payments being processed |
| Processing Duration | `histogram_quantile(0.95, payment_processing_duration_seconds_bucket)` | 95th percentile of processing time |

## Data Flow

### End-to-End Example

1. **Payment Created**
   ```
   User → API → Kafka → Workers (FraudDetectionWorker)
   ```

2. **Metrics Incremented**
   ```csharp
   // In FraudDetectionWorker.cs
   PaymentsByCurrency.WithLabels("USD").Inc();
   PaymentsApprovedTotal.Inc();
   PaymentsAutoCompletedTotal.Inc();
   ```

3. **Prometheus Scrapes Metrics** (every 15s)
   ```
   Prometheus → HTTP GET http://workers:5002/metrics
   Workers → Returns text metrics:
       payments_processed_total{status="success"} 1
       payments_approved_total 1
       payments_auto_completed_total 1
   ```

4. **Grafana Queries Prometheus**
   ```
   Grafana → PromQL: sum(payments_processed_total)
   Prometheus → Returns: {"value": [timestamp, "1"]}
   Grafana → Renders: Panel shows "1"
   ```

5. **Dashboard Updates** (auto-refresh every 10s)
   - Grafana re-runs all panel queries
   - Updates visualizations in real-time

## Docker Compose Integration

### Services Configuration

```yaml
# Workers - Exposes metrics
workers:
  build:
    context: ./src
    dockerfile: FintechPlatform.Workers/Dockerfile
  ports:
    - "5002:5002"  # Metrics port
  environment:
    - ConnectionStrings__DefaultConnection=Host=postgres;...
    - Kafka__BootstrapServers=kafka:29092

# Prometheus - Scrapes metrics
prometheus:
  image: prom/prometheus:latest
  ports:
    - "9090:9090"
  volumes:
    - ./monitoring/prometheus.yml:/etc/prometheus/prometheus.yml
    - prometheus_data:/prometheus

# Grafana - Visualizes metrics
grafana:
  image: grafana/grafana:latest
  ports:
    - "3000:3000"
  volumes:
    - ./monitoring/grafana/dashboards:/etc/grafana/provisioning/dashboards
    - ./monitoring/grafana/datasources:/etc/grafana/provisioning/datasources
  depends_on:
    - prometheus
```

### Why Docker for Everything?

Running all services in Docker solves:
1. **WSL Networking Issues** - Containers in same network communicate directly
2. **Consistent Environment** - Same setup on any machine
3. **Easy Deployment** - Single `docker-compose up` command
4. **Service Discovery** - Container names resolve as hostnames

## Accessing the Stack

| Service | URL | Credentials |
|---------|-----|-------------|
| **Grafana** | http://localhost:3000 | admin/admin |
| **Prometheus** | http://localhost:9090 | None |
| **Workers Metrics** | http://localhost:5002/metrics | None |
| **API** | http://localhost:5153/swagger | None |

## Creating New Metrics

### Step 1: Define Metric in Workers

```csharp
private static readonly Counter MyCustomMetric = Metrics
    .CreateCounter("my_custom_metric", "Description of metric",
        new CounterConfiguration { LabelNames = new[] { "label1", "label2" } });
```

### Step 2: Increment Metric in Code

```csharp
MyCustomMetric.WithLabels("value1", "value2").Inc();
```

### Step 3: Query in Prometheus

Test query: `http://localhost:9090/graph?g0.expr=my_custom_metric`

### Step 4: Add to Grafana Dashboard

1. Open Grafana → Dashboard → Edit Panel
2. Add query: `sum(my_custom_metric)`
3. Configure visualization
4. Export dashboard JSON
5. Save to `monitoring/grafana/dashboards/`

## Troubleshooting

### No Data in Grafana

**Check Prometheus Targets:**
```bash
curl http://localhost:9090/api/v1/targets | jq '.data.activeTargets[] | {job, health}'
```

Should show: `"health": "up"`

**Check Metrics Endpoint:**
```bash
curl http://localhost:5002/metrics | grep payments_
```

Should return metric data.

**Check Datasource UID:**
```bash
# Get actual UID
curl -u admin:admin http://localhost:3000/api/datasources | jq '.[].uid'

# Update dashboards if needed
sed -i 's/"uid": "prometheus"/"uid": "ACTUAL_UID"/g' monitoring/grafana/dashboards/*.json
docker restart fintechplatform-grafana
```

### Metrics Not Updating

**Verify Prometheus Scraping:**
```bash
docker logs fintechplatform-prometheus --tail 50 | grep workers
```

**Check Workers Logs:**
```bash
docker logs fintechplatform-workers --tail 50 | grep -i metric
```

### Dashboard Shows Old Data

Grafana caches data. Solutions:
1. Refresh dashboard (Ctrl+R)
2. Clear time range and re-select
3. Restart Grafana: `docker restart fintechplatform-grafana`

## Best Practices

### Metric Naming

- Use snake_case: `payment_processing_duration`
- Include unit suffix: `_seconds`, `_bytes`, `_total`
- Counter names should end with `_total`
- Use descriptive labels instead of many metrics

### Performance

- **Don't create high-cardinality metrics** (e.g., don't use payment ID as label)
- **Use histograms for percentiles**, not individual gauges
- **Limit label values** - Each unique label combination creates a new time series

### Security

Current setup is for development. In production:
1. Enable authentication on Prometheus
2. Use TLS for Grafana
3. Restrict metrics endpoint access
4. Use Grafana API keys instead of admin credentials

## References

- [Prometheus Documentation](https://prometheus.io/docs/)
- [Grafana Documentation](https://grafana.com/docs/)
- [Prometheus .NET Client](https://github.com/prometheus-net/prometheus-net)
- [PromQL Tutorial](https://prometheus.io/docs/prometheus/latest/querying/basics/)
