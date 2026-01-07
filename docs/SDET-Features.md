# FintechPlatform - SDET Testing Features

## ✅ Implemented Features (Ready for Testing)

### 1. Refund API
**Endpoint:** `POST /api/merchants/{merchantId}/payments/{paymentId}/refund`

**Request Body:**
```json
{
  "refundAmountInMinorUnits": 50000,  // Optional - defaults to full amount
  "reason": "Customer request"          // Optional
}
```

**Use Cases:**
- Full refund (omit amount)
- Partial refund (specify amount < payment amount)
- Refund with/without reason
- Test balance deduction
- Test refund of already refunded payment (should fail)
- Test negative balance scenarios

**Test Command:**
```bash
curl -X POST http://localhost:5153/api/merchants/{merchantId}/payments/{paymentId}/refund \
  -H "Content-Type: application/json" \
  -d '{"refundAmountInMinorUnits": 50000, "reason": "Test refund"}'
```

---

### 2. Bulk Payment Creation API
**Endpoint:** `POST /api/merchants/{merchantId}/payments/bulk`

**Request Body:**
```json
{
  "payments": [
    {
      "amountInMinorUnits": 10000,
      "currency": "USD",
      "description": "Bulk payment 1"
    },
    {
      "amountInMinorUnits": 20000,
      "currency": "USD",
      "description": "Bulk payment 2"
    }
  ]
}
```

**Limits:** 1-1000 payments per request

**Use Cases for SDET:**
- **Concurrency Testing:** Create 100+ payments simultaneously
- **Race Condition Testing:** Multiple bulk requests in parallel
- **Transaction Testing:** Verify all-or-nothing behavior
- **Performance Testing:** Measure time for 100/500/1000 payments
- **Event Publishing:** Verify all payments publish Kafka events
- **Worker Testing:** Verify workers process all payments

**Test Commands:**
```bash
# Create 100 payments for load testing
curl -X POST http://localhost:5153/api/merchants/{merchantId}/payments/bulk \
  -H "Content-Type: application/json" \
  -d '{
    "payments": [
      # Generate 100 payment objects...
    ]
  }'

# Test concurrent requests
for i in {1..10}; do
  curl -X POST http://localhost:5153/api/merchants/{merchantId}/payments/bulk \
    -H "Content-Type: application/json" \
    -d '{"payments":[{"amountInMinorUnits":10000,"currency":"USD"}]}' &
done
wait
```

---

### 3. Real-Time Notifications (Already Working)
**WebSocket Connection:** `ws://localhost:5153/hubs/notifications`

**Events:**
- PaymentCompleted
- PaymentRefunded
- WithdrawalCancelled

**Test:** Create payment → Worker completes → Toast notification appears

---

## 📋 Ready to Implement Next

### 4. Payment Filtering API
Enhance `GET /api/merchants/{id}/payments` with query parameters:
- `dateFrom` / `dateTo` - Date range
- `minAmount` / `maxAmount` - Amount range
- `status` - Filter by status (comma-separated)
- `search` - Full-text search on description

### 5. CSV Export API
`GET /api/merchants/{id}/payments/export` - Download all filtered payments as CSV

### 6. Analytics API  
`GET /api/merchants/{id}/analytics` - Revenue charts, success rates, metrics

---

##  🎯 SDET Test Scenarios

### Concurrency Tests
1. Create 100 bulk payments → Verify balance accuracy
2. Create 10 payments + 10 refunds concurrently → Check for race conditions
3. Complete payment + Refund payment simultaneously → Verify transaction isolation

### Edge Cases
1. Refund amount > payment amount → Should fail
2. Refund pending payment → Should fail
3. Refund already refunded payment → Should fail
4. Bulk request with 1001 payments → Should fail (validation)
5. Bulk request with invalid currency → Should rollback all

### Data Integrity
1. Create 100 payments → Verify all appear in database
2. Refund payment → Verify balance decreased correctly
3. Check ledger entries match payments + refunds
4. Verify PaymentRefundedEvent published to Kafka

### Performance
1. Measure time to create 100 payments (bulk)
2. Measure time to create 100 payments (individual)
3. Monitor worker processing time for bulk payments
4. Check database query performance with 10K+ payments

---

## 🚀 Quick Test Script

```bash
# Set merchant ID
MERCHANT_ID="87ee0952-6957-4f94-89ad-27b17cd975cc"

# 1. Create bulk payments
curl -X POST "http://localhost:5153/api/merchants/$MERCHANT_ID/payments/bulk" \
  -H "Content-Type: application/json" \
  -d '{"payments":[
    {"amountInMinorUnits":10000,"currency":"USD","description":"Test 1"},
    {"amountInMinorUnits":20000,"currency":"USD","description":"Test 2"},
    {"amountInMinorUnits":30000,"currency":"USD","description":"Test 3"}
  ]}'

# 2. Wait for workers to complete
sleep 5

# 3. Get all payments
curl "http://localhost:5153/api/merchants/$MERCHANT_ID/payments"

# 4. Refund a payment (use actual payment ID from step 3)
PAYMENT_ID="<from-step-3>"
curl -X POST "http://localhost:5153/api/merchants/$MERCHANT_ID/payments/$PAYMENT_ID/refund" \
  -H "Content-Type: application/json" \
  -d '{"refundAmountInMinorUnits":5000,"reason":"Partial refund test"}'

# 5. Check balance
curl "http://localhost:5153/api/merchants/$MERCHANT_ID/balances"
```

---

## 📊 Features Comparison

| Feature | Status | Test Value |
|---------|--------|------------|
| Refund (Full/Partial) | ✅ DONE | Balance accuracy, state transitions |
| Bulk Payment Creation | ✅ DONE | Concurrency, performance, race conditions |
| Real-Time Notifications | ✅ DONE | WebSocket testing, event delivery |
| Payment Filtering | 🔄 TODO | Query optimization, edge cases |
| CSV Export | 🔄 TODO | File generation, encoding |
| Analytics Dashboard | 🔄 TODO | Data aggregation, charting |
| Audit Logs | 🔄 TODO | Data integrity, compliance |
| Error Simulation | 🔄 TODO | Resilience, retry logic |

---

Would you like me to continue implementing the remaining features? The next priorities would be:
1. ✅ Filtering API (for testing query performance)
2. ✅ CSV Export (for data validation)
3. ✅ Analytics (for aggregation testing)

Let me know which features you'd like next!
