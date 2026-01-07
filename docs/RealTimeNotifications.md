# Real-Time Notifications with SignalR

## Overview
Real-time notifications are now implemented using **SignalR WebSockets** to push payment approval notifications instantly to the admin UI.

## Architecture

```
Payment Worker approves payment
    ↓
Publishes PaymentCompletedEvent to Kafka
    ↓
NotificationConsumer (in API) receives event
    ↓
Broadcasts via SignalR Hub over WebSocket
    ↓
Admin UI receives notification
    ↓
Toast notification appears in real-time
```

## Technology Stack

### Backend (.NET)
- **SignalR** - Built into ASP.NET Core 8.0
- **WebSockets** - Primary transport (auto-fallback to SSE/Long Polling)
- **Kafka Consumer** - Listens to payment-events topic

### Frontend (React/TypeScript)
- **@microsoft/signalr** - Official SignalR JavaScript client
- **WebSocket connection** - Persistent real-time connection
- **Toast notifications** - Visual feedback

## Implementation Details

### Backend Components

**1. NotificationHub** (`src/api/Hubs/NotificationHub.cs`)
- SignalR hub that manages WebSocket connections
- Logs client connections/disconnections

**2. NotificationConsumer** (`src/api/BackgroundServices/NotificationConsumer.cs`)
- Background service running in the API
- Subscribes to Kafka `payment-events` topic
- Converts Kafka events to notification messages
- Broadcasts to all connected clients via SignalR

**3. NotificationMessage** (`src/api/Models/NotificationMessage.cs`)
- Standardized notification format
- Includes: title, message, severity, timestamp, custom data

**4. Program.cs Registration**
```csharp
builder.Services.AddSignalR();
builder.Services.AddHostedService<NotificationConsumer>();
app.MapHub<NotificationHub>("/hubs/notifications");
```

### Frontend Components

**1. SignalR Service** (`admin-ui/src/services/signalRService.ts`)
- Manages WebSocket connection lifecycle
- Automatic reconnection with exponential backoff
- Event handler registration

**2. App.tsx Integration**
```typescript
useEffect(() => {
  signalRService.connect((notification) => {
    addToast({
      message: notification.message,
      type: notification.severity,
      title: notification.title,
    });
  });
  return () => signalRService.disconnect();
}, []);
```

## Supported Notifications

### PaymentCompleted
- **Type**: `PaymentCompleted`
- **Severity**: `success`
- **Message**: "Payment of {amount} has been completed. New balance: {balance}"
- **Data**: paymentId, merchantId, amount, currency, newBalance

### WithdrawalCancelled (Future)
- **Type**: `WithdrawalCancelled`
- **Severity**: `warning`
- **Message**: "Withdrawal of {amount} was cancelled"
- **Data**: withdrawalId, merchantId, amount, currency

## Testing

### Manual Testing
1. Navigate to admin UI: http://localhost:5173
2. Open browser dev tools → Network → WS (WebSocket tab)
3. Verify connection to `ws://localhost:5153/hubs/notifications`
4. Create a payment via API or Swagger
5. Complete the payment (approve it)
6. Watch toast notification appear instantly in admin UI

### API Endpoints
```bash
# Create payment
POST http://localhost:5153/api/payments

# Complete payment (triggers notification)
PUT http://localhost:5153/api/payments/{id}/complete
```

### Check Logs
```bash
# API logs (SignalR connections)
docker logs -f fintechplatform-api | grep -i signalr

# Notification consumer logs
docker logs -f fintechplatform-api | grep -i notification

# Admin UI logs (browser console)
# Look for "SignalR connected successfully"
```

## Configuration

### Backend
No additional configuration needed - SignalR is built-in.

### Frontend
Connection URL configured via environment variable:
```env
VITE_API_URL=http://localhost:5153
```
SignalR hub URL: `${VITE_API_URL}/hubs/notifications`

## Security Considerations

### Current Implementation (Development)
- ✅ CORS allows all origins
- ✅ No authentication required
- ⚠️ All clients receive all notifications

### Production Recommendations
1. **Authentication**: Require JWT tokens for SignalR connections
2. **Authorization**: Filter notifications by merchant/user
3. **Groups**: Use SignalR groups to send targeted notifications
4. **Rate Limiting**: Prevent notification flooding
5. **CORS**: Restrict to specific origins

## Scaling Considerations

### Single Instance
Current setup works perfectly for single API instance.

### Multiple Instances (Future)
When scaling horizontally, add **backplane**:
- **Redis** - Recommended for SignalR backplane
- **Azure SignalR Service** - Managed solution
- **SQL Server** - Alternative backplane

```csharp
// Future multi-instance setup
builder.Services.AddSignalR()
    .AddStackExchangeRedis("localhost:6379");
```

## Troubleshooting

### WebSocket Connection Failed
- Check CORS configuration
- Verify API is running on correct port
- Check browser console for errors

### No Notifications Appearing
- Verify NotificationConsumer is running: `docker logs fintechplatform-api`
- Check Kafka topic has events: `docker exec fintechplatform-kafka kafka-console-consumer --topic payment-events --from-beginning`
- Verify SignalR connection in browser DevTools → Network → WS

### Connection Keeps Dropping
- Check network stability
- Review reconnection logs in browser console
- Increase reconnection attempts if needed

## Alternative Technologies

### Comparison
| Technology | Pros | Cons |
|-----------|------|------|
| **SignalR** | Native .NET, automatic fallback, reconnection | Requires backplane for scaling |
| **Server-Sent Events** | Simple, HTTP-based | One-way only, no binary support |
| **Raw WebSockets** | Full control, lightweight | Manual implementation required |
| **Polling** | Simple | Not real-time, inefficient |
| **Kafka in Browser** | Direct events | Security risk, complex |

### Why SignalR?
- ✅ Production-ready
- ✅ Automatic connection management
- ✅ Works with .NET ecosystem
- ✅ TypeScript client available
- ✅ Easy to implement

## Future Enhancements

1. **User-specific notifications** - Only show notifications for relevant merchants
2. **Notification history** - Store and retrieve past notifications
3. **Notification preferences** - Allow users to configure notification types
4. **Sound/desktop notifications** - Browser notification API integration
5. **Read/unread status** - Track which notifications have been seen
6. **Notification center** - Dedicated UI panel for all notifications
7. **Push notifications** - Mobile app support

## Metrics & Monitoring

### What to Monitor
- Active WebSocket connections count
- Notification delivery rate
- Connection failures
- Reconnection attempts
- Average notification latency

### Logging
All SignalR events are logged:
- Client connections: `INFO: Client connected: {ConnectionId}`
- Client disconnections: `INFO: Client disconnected: {ConnectionId}`
- Broadcast notifications: `INFO: Broadcast notification: {Type} - {Title}`
- Errors: `ERROR: Error processing notification event`
