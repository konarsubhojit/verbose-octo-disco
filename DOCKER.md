# Docker & Aspire Guide

This guide covers different ways to containerize and run the Catalog Order API.

## Table of Contents

- [.NET Aspire (Recommended)](#net-aspire-recommended)
- [Docker Compose](#docker-compose)
- [Docker Manual](#docker-manual)
- [Troubleshooting](#troubleshooting)

## .NET Aspire (Recommended)

.NET Aspire is Microsoft's opinionated stack for building observable, production-ready cloud-native applications. It provides the best development experience.

### Prerequisites

- .NET 10 SDK
- Docker Desktop
- Visual Studio 2022 17.13+ or Visual Studio Code with C# Dev Kit

### Installation

```bash
# Install Aspire workload
dotnet workload install aspire

# Or update if already installed
dotnet workload update
```

### Running with Aspire

```bash
# Navigate to the AppHost project
cd CatalogOrder.AppHost

# Run the orchestrator
dotnet run
```

The Aspire dashboard will automatically open in your browser at `https://localhost:15888` (or similar).

### What You Get

The Aspire dashboard provides:

1. **Services Overview**: All running services (API, PostgreSQL, Redis)
2. **Logs**: Real-time log streaming from all services
3. **Traces**: Distributed tracing with OpenTelemetry
4. **Metrics**: Performance metrics and dashboards
5. **Environment**: Environment variables and configuration
6. **Console**: Direct access to service consoles

### Service Endpoints

Aspire dynamically assigns ports. Check the dashboard for exact URLs, but typically:

- **API**: `http://localhost:5xxx` or `https://localhost:7xxx`
- **PostgreSQL**: `localhost:5432`
- **Redis**: `localhost:6379`
- **pgAdmin**: `http://localhost:8xxx` (PostgreSQL web UI)

### Customizing Aspire Configuration

Edit `CatalogOrder.AppHost/AppHost.cs` to modify service configuration:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Customize PostgreSQL
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()  // Add pgAdmin web interface
    .WithDataVolume()  // Persist data between runs
    .AddDatabase("catalogorderdb");

// Customize Redis
var redis = builder.AddRedis("redis")
    .WithRedisCommander();  // Add Redis Commander web UI

// Customize API
var api = builder.AddProject<Projects.CatalogOrderApi>("catalogorderapi")
    .WithReference(postgres)
    .WithReference(redis)
    .WithExternalHttpEndpoints()
    .WithReplicas(2);  // Run 2 instances for load balancing

builder.Build().Run();
```

### Environment-Specific Configuration

Aspire uses standard ASP.NET Core configuration:

- `appsettings.json` - Base configuration
- `appsettings.Development.json` - Development overrides
- Environment variables - Runtime overrides

## Docker Compose

Traditional Docker Compose orchestration for production deployments.

### Prerequisites

- Docker
- Docker Compose

### Running

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down

# Stop and remove volumes (⚠️ deletes data)
docker-compose down -v
```

### Service URLs

- **API**: `http://localhost:5233`
- **Swagger UI**: `http://localhost:5233`
- **PostgreSQL**: `localhost:5432`
- **Redis**: `localhost:6379`

### Customizing docker-compose.yml

Edit `docker-compose.yml` to customize:

```yaml
services:
  api:
    environment:
      # Update these for production
      - JwtSettings__SecretKey=your-production-secret-key-here
      - GoogleAuth__ClientId=your-google-client-id.apps.googleusercontent.com
      - BlobStorage__ConnectionString=your-azure-storage-connection-string
      - Cors__AllowedOrigins__0=https://yourdomain.com
```

### Production Considerations

1. **Change default passwords**: Update PostgreSQL password
2. **Use secrets**: Don't commit secrets to git
3. **Configure CORS**: Set specific allowed origins
4. **Use HTTPS**: Configure SSL certificates
5. **Resource limits**: Add CPU/memory limits
6. **Health checks**: Already configured
7. **Logging**: Configure centralized logging

Example with resource limits:

```yaml
api:
  deploy:
    resources:
      limits:
        cpus: '1.0'
        memory: 512M
      reservations:
        cpus: '0.5'
        memory: 256M
```

## Docker Manual

Build and run containers manually.

### Build the Image

```bash
docker build -t catalogorder-api:latest .
```

### Run with External Services

If you have PostgreSQL and Redis running elsewhere:

```bash
docker run -d \
  --name catalogorder-api \
  -p 5233:8080 \
  -e ConnectionStrings__DefaultConnection="Host=your-db-host;Database=catalogorderdb;Username=postgres;Password=yourpassword" \
  -e ConnectionStrings__Redis="your-redis-host:6379" \
  -e JwtSettings__SecretKey="your-secret-key" \
  -e GoogleAuth__ClientId="your-google-client-id" \
  catalogorder-api:latest
```

### Run with Docker Network

Create a custom network for service communication:

```bash
# Create network
docker network create catalogorder-net

# Run PostgreSQL
docker run -d \
  --name postgres \
  --network catalogorder-net \
  -e POSTGRES_DB=catalogorderdb \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 \
  postgres:17-alpine

# Run Redis
docker run -d \
  --name redis \
  --network catalogorder-net \
  -p 6379:6379 \
  redis:7-alpine

# Run API
docker run -d \
  --name api \
  --network catalogorder-net \
  -p 5233:8080 \
  -e ConnectionStrings__DefaultConnection="Host=postgres;Database=catalogorderdb;Username=postgres;Password=postgres" \
  -e ConnectionStrings__Redis="redis:6379" \
  catalogorder-api:latest
```

## Troubleshooting

### Aspire Issues

**Problem**: Aspire dashboard doesn't open

```bash
# Check if port 15888 is in use
netstat -an | grep 15888

# Try accessing manually
open https://localhost:15888
```

**Problem**: Services fail to start

```bash
# Check logs in the Aspire dashboard Console tab
# Or run with verbose logging
dotnet run --verbosity detailed
```

### Docker Build Issues

**Problem**: Docker build fails with SSL/certificate errors

This is a known issue with .NET 10 SDK in some Docker environments. Solutions:

1. Use .NET Aspire instead (recommended)
2. Build locally and push to registry
3. Use multi-stage build with certificate fixes (see Dockerfile)

**Problem**: Container can't connect to PostgreSQL

```bash
# Check if PostgreSQL is running
docker ps | grep postgres

# Check logs
docker logs postgres

# Test connection from container
docker exec -it catalogorder-api ping postgres
```

**Problem**: Out of disk space

```bash
# Clean up Docker resources
docker system prune -a --volumes

# This removes:
# - All stopped containers
# - All networks not used by containers
# - All images without containers
# - All build cache
```

### Common Errors

**Error**: `Assets file 'project.assets.json' not found`

Solution: Run `dotnet restore` before building

**Error**: `Unable to load the service index for source https://api.nuget.org/v3/index.json`

Solution: This is the Docker build SSL issue. Use Aspire or build locally.

**Error**: `Connection refused` when API tries to connect to PostgreSQL

Solution: Ensure services are on the same Docker network or use correct host names.

### Health Checks

Check service health:

```bash
# API health
curl http://localhost:5233/health

# PostgreSQL health
docker exec postgres pg_isready -U postgres

# Redis health
docker exec redis redis-cli ping
```

### Viewing Logs

```bash
# Docker Compose
docker-compose logs -f api

# Docker
docker logs -f catalogorder-api

# Aspire
# Use the dashboard Console tab for real-time logs
```

### Database Migrations

When using Docker, run migrations:

```bash
# If API container has EF tools
docker exec catalogorder-api dotnet ef database update

# Or from host with connection string
cd CatalogOrderApi
dotnet ef database update --connection "Host=localhost;Database=catalogorderdb;Username=postgres;Password=postgres"
```

### Performance Tuning

For production deployments:

1. **Use Release build**: Dockerfile uses Release by default
2. **Optimize Docker image**: Multi-stage build reduces image size
3. **Configure pooling**: PostgreSQL and Redis connection pooling
4. **Add caching**: Already configured with Redis
5. **Monitor resources**: Use Aspire dashboard or Docker stats

```bash
# View container resource usage
docker stats catalogorder-api
```

## Production Deployment

### Azure Container Apps (with Aspire)

Aspire has first-class support for Azure Container Apps:

```bash
# Install Azure Developer CLI
azd init
azd up
```

### Kubernetes

Generate Kubernetes manifests:

```bash
# Using Docker Compose
docker-compose config > k8s-base.yaml
# Then convert to Kubernetes format

# Or use Aspire deployment
# Aspire can generate deployment manifests
```

### Cloud Run / ECS / AKS

1. Build and push image to registry
2. Configure environment variables
3. Set up managed PostgreSQL and Redis
4. Deploy container

## Additional Resources

- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Docker Documentation](https://docs.docker.com/)
- [PostgreSQL Docker Hub](https://hub.docker.com/_/postgres)
- [Redis Docker Hub](https://hub.docker.com/_/redis)
