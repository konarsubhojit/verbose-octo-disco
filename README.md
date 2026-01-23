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

## Quick Start

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
- PostgreSQL + Entity Framework Core
- Redis
- Azure Blob Storage
- JWT Authentication
- Swagger/OpenAPI

## License

MIT
