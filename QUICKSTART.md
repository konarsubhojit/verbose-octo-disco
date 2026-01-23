# Quick Reference Guide

## Running the Application

### Option 1: .NET Aspire (Recommended for Development)

```bash
cd CatalogOrder.AppHost
dotnet run
```

Opens dashboard at `https://localhost:15888` with all services orchestrated.

**Pros:** 
- Best developer experience
- Built-in observability  
- Automatic service discovery
- Hot reload support

**Cons:**
- Requires .NET 10 SDK and Docker Desktop

---

### Option 2: Docker Compose (Good for Production)

```bash
docker-compose up -d
```

API available at `http://localhost:5233`

**Pros:**
- Production-ready
- Works without .NET SDK
- Standard deployment approach

**Cons:**
- Less dev tooling
- Manual configuration

---

### Option 3: Manual (Good for Debugging)

```bash
# Terminal 1: Start PostgreSQL
docker run -d -p 5432:5432 -e POSTGRES_PASSWORD=postgres postgres:17-alpine

# Terminal 2: Start Redis  
docker run -d -p 6379:6379 redis:7-alpine

# Terminal 3: Run API
cd CatalogOrderApi
dotnet run
```

API available at `http://localhost:5233`

**Pros:**
- Full control
- Easy debugging
- No orchestrator needed

**Cons:**
- Manual service management
- Most complex setup

---

## Common Commands

### Aspire
```bash
# Start
cd CatalogOrder.AppHost && dotnet run

# View logs in dashboard (opens automatically)

# Stop: Ctrl+C
```

### Docker Compose
```bash
# Start services
docker-compose up -d

# View logs
docker-compose logs -f api

# Stop services
docker-compose down

# Restart
docker-compose restart api

# Clean everything
docker-compose down -v
```

### Docker Manual
```bash
# Build image
docker build -t catalogorder-api .

# Run container
docker run -p 5233:8080 catalogorder-api

# View logs
docker logs catalogorder-api

# Stop container
docker stop catalogorder-api

# Remove container
docker rm catalogorder-api
```

---

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | `Host=localhost;Database=catalogorderdb;Username=postgres;Password=postgres` |
| `ConnectionStrings__Redis` | Redis connection string | `localhost:6379` |
| `JwtSettings__SecretKey` | JWT signing key | (see appsettings.json) |
| `GoogleAuth__ClientId` | Google OAuth Client ID | (required) |
| `BlobStorage__ConnectionString` | Azure Blob Storage | `UseDevelopmentStorage=true` |

### Ports

| Service | Port | Protocol |
|---------|------|----------|
| API (HTTP) | 5233 | HTTP |
| API (HTTPS) | 7233 | HTTPS  |
| PostgreSQL | 5432 | TCP |
| Redis | 6379 | TCP |
| Aspire Dashboard | 15888 | HTTPS |

---

## Troubleshooting

### Services Won't Start

```bash
# Check if ports are in use
netstat -an | grep 5233
netstat -an | grep 5432
netstat -an | grep 6379

# Kill processes on port
lsof -ti:5233 | xargs kill -9  # macOS/Linux
```

### Docker Build Fails

Known issue with .NET 10 in some environments. Use Aspire instead:
```bash
cd CatalogOrder.AppHost && dotnet run
```

### Can't Connect to Database

```bash
# Check PostgreSQL is running
docker ps | grep postgres

# Test connection
docker exec postgres pg_isready -U postgres

# For Aspire: Check dashboard for actual connection string
```

### Performance Issues

```bash
# Check resource usage
docker stats

# View logs for errors
docker-compose logs -f | grep -i error

# Restart services
docker-compose restart
```

---

## Development Workflow

### With Aspire (Recommended)

1. Make code changes
2. Hot reload applies automatically  
3. View logs/traces in dashboard
4. Test endpoints from Swagger UI

### With Docker Compose

1. Make code changes
2. Rebuild: `docker-compose up -d --build api`
3. View logs: `docker-compose logs -f api`
4. Test endpoints from Swagger UI

---

## Testing

### API Endpoints

```bash
# Health check
curl http://localhost:5233/health

# Swagger UI
open http://localhost:5233

# Test auth (requires Google Client ID)
curl -X POST http://localhost:5233/api/auth/google \
  -H "Content-Type: application/json" \
  -d '{"idToken":"your-google-id-token"}'
```

### Database

```bash
# Connect to PostgreSQL
docker exec -it postgres psql -U postgres -d catalogorderdb

# List tables
\dt

# Exit
\q
```

### Redis

```bash
# Connect to Redis
docker exec -it redis redis-cli

# Test
PING
# Should return PONG

# View keys
KEYS *

# Exit
exit
```

---

## Production Checklist

- [ ] Change default PostgreSQL password
- [ ] Set strong JWT secret key  
- [ ] Configure actual Google OAuth Client ID
- [ ] Set up Azure Blob Storage
- [ ] Configure specific CORS origins (not `*`)
- [ ] Enable HTTPS with valid certificates
- [ ] Set up centralized logging
- [ ] Configure resource limits
- [ ] Set up monitoring/alerting
- [ ] Review security settings
- [ ] Test backup/restore procedures

---

## Getting Help

1. Check [DOCKER.md](DOCKER.md) for detailed documentation
2. Check Aspire dashboard logs for errors
3. View container logs: `docker logs <container>`
4. Check [.NET Aspire docs](https://learn.microsoft.com/en-us/dotnet/aspire/)
5. Check [API documentation](CatalogOrderApi/README.md)

---

## Quick Links

- **Swagger UI**: http://localhost:5233
- **Aspire Dashboard**: https://localhost:15888
- **Health Check**: http://localhost:5233/health
- **GitHub Repo**: https://github.com/konarsubhojit/verbose-octo-disco
