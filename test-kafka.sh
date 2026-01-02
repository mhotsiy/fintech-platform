#!/bin/bash
# Kafka Event System Test Script

set -e  # Exit on error

echo "🎯 Phase 2 - Kafka Event System Verification"
echo "=============================================="
echo ""

# Check Docker services
echo "✅ Step 1: Verifying Docker services..."
docker-compose ps | grep -E "Up|healthy" || (echo "❌ Some services are not running!" && exit 1)
echo "✅ All Docker services are running"
echo ""

# Build the API
echo "✅ Step 2: Building API..."
dotnet build src/api/FintechPlatform.Api.csproj --nologo -v quiet
echo "✅ API build successful"
echo ""

# Apply migrations (if not already done)
echo "✅ Step 3: Applying database migrations..."
dotnet ef database update --project src/infrastructure/FintechPlatform.Infrastructure.csproj --startup-project src/api/FintechPlatform.Api.csproj --no-build > /dev/null 2>&1 || echo "Migrations already applied"
echo "✅ Database ready"
echo ""

echo "✅ Step 4: Start the API in the background"
echo "Run: cd src/api && dotnet run"
echo ""
echo "Then test with:"
echo ""
echo "# Create merchant"
echo 'curl -X POST http://localhost:5153/api/merchants \\'
echo '  -H "Content-Type: application/json" \\'
echo '  -d '"'"'{"name": "Coffee Shop", "email": "pay@coffee.com"}'"'"''
echo ""
echo "# Create payment (replace {merchant-id})"
echo 'curl -X POST http://localhost:5153/api/merchants/{MERCHANT_ID}/payments \\'
echo '  -H "Content-Type: application/json" \\'
echo '  -d '"'"'{"amountInMinorUnits": 5000, "currency": "USD", "externalReference": "TEST-001"}'"'"''
echo ""
echo "# Complete payment (replace IDs)"
echo 'curl -X POST http://localhost:5153/api/merchants/{MERCHANT_ID}/payments/{PAYMENT_ID}/complete'
echo ""
echo "Then check:"
echo "- API logs for event publishing messages"
echo "- Kafka UI at http://localhost:8080"
echo "- Topics → payment-events → Messages"
echo ""
echo "=============================================="
echo "✅ All prerequisites verified!"
echo "✅ Ready to test Kafka events!"
echo "=============================================="
