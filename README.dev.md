# MoneyBee - Development Guide

## Quick Start (Local Development)

### 1. Start Infrastructure Services
```bash
# Start only infrastructure (PostgreSQL, Redis, RabbitMQ, External Services)
docker compose -f docker-compose.dev.yml up -d

# Check status
docker compose -f docker-compose.dev.yml ps
```

### 2. Run MoneyBee Services Locally
Open 3 separate terminals:

**Terminal 1 - Auth Service:**
```bash
dotnet run --project src/Services/MoneyBee.Auth.Service
```

**Terminal 2 - Customer Service:**
```bash
dotnet run --project src/Services/MoneyBee.Customer.Service
```

**Terminal 3 - Transfer Service:**
```bash
dotnet run --project src/Services/MoneyBee.Transfer.Service
```

### 3. Access Services
- **Auth Service**: http://localhost:5001/swagger
- **Customer Service**: http://localhost:5002/swagger
- **Transfer Service**: http://localhost:5003/swagger
- **RabbitMQ Management**: http://localhost:15672 (moneybee/moneybee123)
- **Seq Logs**: http://localhost:5341 (Admin123!)

## Benefits of Local Development

✅ **Hot Reload** - Code changes apply immediately  
✅ **Fast Debugging** - Set breakpoints, inspect variables  
✅ **Quick Iterations** - No Docker rebuild required  
✅ **Better Logs** - See console output directly  
✅ **Resource Efficient** - Only infrastructure in Docker  

## Production Deployment

```bash
# Build and run everything in Docker
docker compose up -d

# Or build specific services
docker compose build auth-service customer-service transfer-service
docker compose up -d
```

## Stop Services

```bash
# Stop infrastructure
docker compose -f docker-compose.dev.yml down

# Stop all (if using full docker-compose.yml)
docker compose down
```

## Database Migrations

Run migrations for all services:
```bash
dotnet ef database update --project src/Services/MoneyBee.Auth.Service
dotnet ef database update --project src/Services/MoneyBee.Customer.Service
dotnet ef database update --project src/Services/MoneyBee.Transfer.Service
```

## Testing with Postman

1. Import `MoneyBee.postman_collection.json`
2. Run "Auth Service → Create API Key"
3. API key will be saved automatically to collection variables
4. Test other endpoints

## Common Issues

**Issue**: Service can't connect to database  
**Fix**: Ensure infrastructure is running: `docker compose -f docker-compose.dev.yml ps`

**Issue**: Port already in use  
**Fix**: Check running processes: `lsof -i :5001` and kill if needed

**Issue**: RabbitMQ connection failed  
**Fix**: Wait for RabbitMQ to be healthy: `docker logs moneybee-rabbitmq`
