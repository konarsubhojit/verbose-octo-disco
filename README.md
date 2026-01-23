# verbose-octo-disco

A complete .NET 10 Web API for catalog and order management with Google OAuth authentication, multi-currency support, and Azure Blob Storage integration.

## Features

- 🔐 **Google OAuth 2.0** authentication with JWT tokens
- 💰 **Multi-currency support** (USD, EUR, GBP, INR) with minor unit storage
- 📦 **Product catalog** with design variants and image uploads
- 📋 **Order management** with auto-generated order numbers (ORD-YYYYMMDD-XXXX)
- 🚚 **Shipment tracking** with AWB numbers and delivery status
- 📊 **Sales reports** with flexible grouping and filtering
- ⚡ **Redis caching** for improved performance
- 🖼️ **Azure Blob Storage** for product images
- 🗄️ **PostgreSQL** database with Entity Framework Core
- 🐳 **Docker & Aspire** for containerization and orchestration

## Quick Start with .NET Aspire (Recommended)

.NET Aspire provides the best development experience with built-in orchestration, service discovery, and observability.

```bash
# Clone the repository
git clone https://github.com/konarsubhojit/verbose-octo-disco.git
cd verbose-octo-disco

# Install .NET Aspire templates (if not already installed)
dotnet workload install aspire

# Run the Aspire AppHost
cd CatalogOrder.AppHost
dotnet run
```

The Aspire dashboard will open automatically at `http://localhost:15888` where you can:
- View all services (API, PostgreSQL, Redis)
- Monitor logs in real-time
- Access health checks and metrics
- View distributed traces
- Access the API at the dynamically assigned port

### What Aspire Provides

- **Automatic Service Orchestration**: Starts PostgreSQL and Redis containers automatically
- **Service Discovery**: Services can reference each other by name
- **Health Checks**: Built-in health monitoring for all services
- **Distributed Tracing**: OpenTelemetry integration out of the box
- **Metrics & Logging**: Centralized observability dashboard
- **Hot Reload**: Fast development iteration

## Quick Start with Docker Compose

For traditional Docker deployment without Aspire:

```bash
# Clone the repository
git clone https://github.com/konarsubhojit/verbose-octo-disco.git
cd verbose-octo-disco

# Update appsettings.json with your configuration
# Note: Connection strings for PostgreSQL and Redis are configured in docker-compose.yml

# Build and run with Docker Compose
docker-compose up -d

# The API will be available at http://localhost:5233
# PostgreSQL at localhost:5432
# Redis at localhost:6379

# View logs
docker-compose logs -f api

# Stop services
docker-compose down
```

## Manual Setup (Without Containers)

```bash
# Clone the repository
git clone https://github.com/konarsubhojit/verbose-octo-disco.git
cd verbose-octo-disco/CatalogOrderApi

# Install dependencies
dotnet restore

# Update appsettings.json with your configuration
# - PostgreSQL connection string
# - Redis connection string
# - Azure Blob Storage connection string
# - Google OAuth Client ID
# - JWT secret key

# Run database migrations
dotnet ef database update

# Run the application
dotnet run
```

The API will be available at `http://localhost:5233` with Swagger UI at the root.

## Docker Images

### Building the Docker Image

```bash
docker build -t catalogorder-api .
```

### Running the API Container

```bash
docker run -p 5233:8080 \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Database=catalogorderdb;Username=postgres;Password=postgres" \
  -e ConnectionStrings__Redis="host.docker.internal:6379" \
  catalogorder-api
```

## Documentation

For detailed setup instructions, API documentation, and troubleshooting, see [CatalogOrderApi/README.md](CatalogOrderApi/README.md).

## API Highlights

### Authentication
```http
POST /api/auth/google - Authenticate with Google
GET /api/auth/me      - Get current user
```

### Items & Variants
```http
GET    /api/items                      - List items with pagination
POST   /api/items                      - Create item
POST   /api/items/{id}/variants        - Upload design variant image
DELETE /api/items/{id}                 - Soft delete
POST   /api/items/{id}/restore         - Restore deleted item
```

### Orders
```http
GET  /api/orders                   - List orders with filters
POST /api/orders                   - Create order
PUT  /api/orders/{id}/status       - Update order status
```

### Shipments
```http
POST /api/orders/{id}/shipment     - Create/update shipment
PUT  /api/shipments/{id}/status    - Update shipment status
```

### Reports
```http
GET /api/reports/sales             - Sales analytics
```

## Tech Stack

- .NET 10
- .NET Aspire for orchestration
- PostgreSQL + Entity Framework Core
- Redis
- Azure Blob Storage
- JWT Authentication
- Swagger/OpenAPI
- Docker

## Project Structure

```
verbose-octo-disco/
├── CatalogOrderApi/              # Main Web API project
├── CatalogOrder.AppHost/         # Aspire orchestration host
├── CatalogOrder.ServiceDefaults/ # Aspire service configuration
├── Dockerfile                    # Docker image definition
├── docker-compose.yml           # Docker Compose orchestration
└── .dockerignore                # Docker build exclusions
```

## License

MIT
