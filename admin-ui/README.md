# FintechPlatform Admin UI

Modern React admin dashboard for the FintechPlatform payment system.

## Tech Stack

- **React 18.3** - UI framework with hooks
- **TypeScript 5.7** - Type safety
- **Vite 6.0** - Fast build tool with HMR
- **Tailwind CSS 3.4** - Utility-first styling
- **TanStack Query 5.6** - Server state management
- **React Router 7.1** - Client-side routing
- **Axios** - HTTP client
- **Recharts** - Data visualization
- **Lucide React** - Modern icons
- **date-fns** - Date formatting

## Features

### Dashboard
- System overview with key metrics
- Total merchants, payments, completed/pending counts
- Recent payment activity
- Quick action cards

### Merchants Management
- View all merchants in a grid
- Create new merchants
- View merchant details
- Check balances (available, pending, total)

### Payments
- View payments by merchant
- Create new payments with idempotency keys
- Complete pending payments
- Refund completed payments
- Real-time balance updates

### Withdrawals
- Request withdrawals
- View pending/processed withdrawals
- Cancel pending withdrawals
- Balance reservation system

### Ledger
- Complete transaction history
- All balance changes tracked
- Audit trail for compliance

### System Health
- Monitor all services (API, Workers, Postgres, Kafka)
- Links to Grafana dashboards
- Links to Swagger API docs
- Links to Kafka UI

## Architecture

### Component Structure
```
src/
├── api/              # API client with typed endpoints
├── components/       # Reusable UI components
│   ├── Badge.tsx
│   ├── Button.tsx
│   ├── Card.tsx
│   ├── ErrorMessage.tsx
│   ├── Loading.tsx
│   └── Navigation.tsx
├── lib/              # Utility functions
│   └── utils.ts      # formatCurrency, formatDate, etc.
├── pages/            # Route components
│   ├── Dashboard.tsx
│   ├── MerchantDetail.tsx
│   ├── Merchants.tsx
│   └── SystemHealth.tsx
├── types/            # TypeScript definitions
│   └── index.ts
├── App.tsx           # Main app with routing
└── main.tsx          # Entry point
```

### State Management
- **TanStack Query** handles all server state
- Automatic caching and refetching
- Optimistic updates for mutations
- Automatic query invalidation

### API Integration
- Axios client with base URL configuration
- Typed request/response interfaces
- Automatic idempotency key generation
- Error handling with retry logic

## Development

### Prerequisites
- Node.js 20+
- Docker and Docker Compose
- Running backend services

### Local Development

1. **Start backend services:**
```bash
docker-compose up -d postgres kafka api workers
```

2. **Install dependencies:**
```bash
cd admin-ui
npm install
```

3. **Start dev server:**
```bash
npm run dev
```

4. **Access UI:**
- Admin UI: http://localhost:5173
- API: http://localhost:5153
- Swagger: http://localhost:5153/swagger
- Grafana: http://localhost:3000
- Kafka UI: http://localhost:8080

### Docker Development

Run everything with Docker Compose:
```bash
docker-compose up
```

The admin-ui service will:
- Install npm dependencies automatically
- Start Vite dev server with HMR
- Expose on port 5173
- Proxy API requests to backend

## Environment Variables

Create `.env` file:
```env
VITE_API_URL=http://localhost:5153
```

For Docker, this is configured in `docker-compose.yml`.

## Build for Production

```bash
npm run build
```

Output in `dist/` directory. Serve with any static file server.

## Key Features Explained

### Idempotency
Payment creation generates unique idempotency keys to prevent duplicate payments. Keys are displayed in the UI for transparency.

### Balance Display
All amounts shown in major units (e.g., $10.00) but stored as minor units (1000 cents) in the backend.

### Real-time Updates
TanStack Query automatically refetches data when:
- Mutations complete
- Window regains focus
- Network reconnects
- Manual refresh

### Error Handling
- Network errors show retry buttons
- Validation errors display inline
- Loading states for all async operations

### Responsive Design
- Mobile-first approach
- Responsive navigation
- Grid layouts adapt to screen size

## Monitoring Integration

Links to external tools:
- **Grafana**: System metrics and dashboards
- **Swagger**: Interactive API documentation
- **Kafka UI**: Message queue inspection

## API Endpoints Used

### Merchants
- `GET /api/merchants` - List all
- `GET /api/merchants/{id}` - Get details
- `POST /api/merchants` - Create
- `GET /api/merchants/{id}/balances` - Get all balances
- `GET /api/merchants/{id}/balances/{currency}` - Get specific balance

### Payments
- `GET /api/merchants/{id}/payments` - List by merchant
- `GET /api/payments/{id}` - Get details
- `POST /api/merchants/{id}/payments` - Create
- `POST /api/payments/{id}/complete` - Complete payment
- `POST /api/payments/{id}/refund` - Refund payment

### Withdrawals
- `GET /api/merchants/{id}/withdrawals` - List by merchant
- `GET /api/withdrawals/{id}` - Get details
- `POST /api/merchants/{id}/withdrawals` - Request
- `POST /api/withdrawals/{id}/cancel` - Cancel
- `POST /api/withdrawals/{id}/process` - Process (admin)

### Admin
- `GET /api/admin/merchants/{id}/ledger` - Transaction history
- `GET /api/admin/merchants/{id}/verify-balance` - Audit

### Health
- `GET /health` - System health check

## Troubleshooting

### API Connection Failed
- Check backend is running: `docker-compose ps`
- Verify API URL in `.env`
- Check CORS configuration in API

### Stale Data
- TanStack Query caches for 30 seconds
- Manual refresh: Click refresh buttons
- Force refetch: Reload page

### Slow Performance
- Check network tab for slow requests
- Verify database performance
- Check Kafka queue backlog

## Code Standards

- Functional components with hooks
- TypeScript strict mode
- Proper error boundaries
- Async/await for all IO
- Descriptive naming
- Comments for complex logic

## Future Enhancements

- [ ] Toast notifications
- [ ] Dark mode
- [ ] Export to CSV
- [ ] Advanced filtering
- [ ] Batch operations
- [ ] WebSocket for real-time updates
- [ ] E2E tests with Playwright
- [ ] Accessibility improvements
- [ ] PWA support
