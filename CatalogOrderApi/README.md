# Catalog Order API

A complete .NET 10 Web API for managing product catalog and orders with multi-currency support, Google OAuth authentication, image storage in Azure Blob Storage, and Redis caching.

## Features

- **Authentication**: Google OAuth 2.0 with JWT tokens
- **Multi-currency Support**: Prices stored in minor units (cents) for accuracy
- **Product Management**: Items with multiple design variants and images
- **Order Management**: Order tracking with auto-generated order numbers (ORD-YYYYMMDD-XXXX)
- **Shipment Tracking**: AWB numbers, delivery partners, status updates
- **Sales Reports**: Aggregated sales data by time period, customer, or source
- **Caching**: Redis for response caching with pattern-based invalidation
- **Image Storage**: Azure Blob Storage for product variant images
- **Soft Deletes**: Items can be deleted and restored without losing order history

## Tech Stack

- **.NET 10** - Latest .NET framework
- **PostgreSQL** - Primary database with Entity Framework Core
- **Redis** - Caching layer
- **Azure Blob Storage** - Image storage
- **JWT** - Token-based authentication
- **Swagger/OpenAPI** - API documentation

## Domain Models

### Item
- Multi-currency pricing (stored as minor units)
- Soft delete support
- One-to-many design variants

### DesignVariant
- Name and image URL
- Images stored in Azure Blob Storage
- Automatic cleanup on deletion

### Order
- Auto-generated order number (ORD-YYYYMMDD-XXXX format)
- Customer details (name, email, phone, address)
- Source tracking (Instagram, Facebook, Call, Offline, Website)
- Status tracking (PendingConfirmation, Confirmed, InProgress, Shipped, Delivered, Cancelled)
- Delivery date
- One-to-many order items
- Optional shipment information

### OrderItem
- Price and name snapshots for historical accuracy
- Quantity and line total
- Nullable ItemId (allows orders to retain history when items are deleted)

### Shipment
- AWB number
- Delivery partner
- Status tracking
- Tracking URL
- Last updated timestamp

### User
- Google OAuth subject ID
- Email, name, avatar
- Creation timestamp

## Prerequisites

- .NET 10 SDK
- PostgreSQL 12+
- Redis 6+
- Azure Storage Account (or Azurite for local development)
- Google OAuth 2.0 Client ID

## Setup Instructions

### 1. Clone the Repository

```bash
git clone https://github.com/konarsubhojit/verbose-octo-disco.git
cd verbose-octo-disco/CatalogOrderApi
```

### 2. Configure Database

Update `appsettings.json` with your PostgreSQL connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=catalogorderdb;Username=postgres;Password=yourpassword",
    "Redis": "localhost:6379"
  }
}
```

Create the database:

```bash
# Using psql
psql -U postgres
CREATE DATABASE catalogorderdb;
\q
```

### 3. Run Database Migrations

```bash
# Install EF Core tools if not already installed
dotnet tool install --global dotnet-ef

# Create initial migration
dotnet ef migrations add InitialCreate

# Apply migration to database
dotnet ef database update
```

### 4. Configure Azure Blob Storage

For local development, use Azurite (Azure Storage Emulator):

```bash
# Install Azurite
npm install -g azurite

# Run Azurite
azurite --silent --location ./azurite --debug ./azurite/debug.log
```

Or update `appsettings.json` with your Azure Storage connection string:

```json
{
  "BlobStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=youraccountname;AccountKey=youraccountkey;EndpointSuffix=core.windows.net",
    "ContainerName": "design-variants"
  }
}
```

### 5. Configure Redis

Start Redis server:

```bash
# On Linux/Mac
redis-server

# Or with Docker
docker run -d -p 6379:6379 redis:latest
```

### 6. Configure Google OAuth

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select existing one
3. Enable Google+ API
4. Create OAuth 2.0 credentials (Web application)
5. Add authorized redirect URIs
6. Copy the Client ID

Update `appsettings.json`:

```json
{
  "GoogleAuth": {
    "ClientId": "your-google-client-id.apps.googleusercontent.com"
  },
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-min-32-characters-long-for-security",
    "Issuer": "CatalogOrderApi",
    "Audience": "CatalogOrderApiClients",
    "ExpirationMinutes": 1440
  }
}
```

### 7. Run the Application

```bash
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `http://localhost:5000` (root)

## API Endpoints

### Authentication

```http
# Authenticate with Google
POST /api/auth/google
Content-Type: application/json

{
  "idToken": "google-id-token-here"
}

# Get current user
GET /api/auth/me
Authorization: Bearer {jwt-token}
```

### Health Check

```http
GET /api/health
```

### Items

```http
# List items with pagination
GET /api/items?page=1&pageSize=20&includeDeleted=false

# Get single item
GET /api/items/{id}

# Create item
POST /api/items
Authorization: Bearer {jwt-token}
Content-Type: application/json

{
  "name": "Product Name",
  "price": 2999,
  "currency": "USD"
}

# Add design variant with image
POST /api/items/{id}/variants
Authorization: Bearer {jwt-token}
Content-Type: multipart/form-data

{
  "name": "Color Variant",
  "image": <file>
}

# Update item
PUT /api/items/{id}
Authorization: Bearer {jwt-token}
Content-Type: application/json

{
  "name": "Updated Name",
  "price": 3499,
  "currency": "USD"
}

# Soft delete item
DELETE /api/items/{id}
Authorization: Bearer {jwt-token}

# Restore deleted item
POST /api/items/{id}/restore
Authorization: Bearer {jwt-token}

# Delete design variant
DELETE /api/items/{itemId}/variants/{variantId}
Authorization: Bearer {jwt-token}
```

### Orders

```http
# List orders with filters
GET /api/orders?page=1&pageSize=20&status=Confirmed&source=Instagram
Authorization: Bearer {jwt-token}

# Get single order
GET /api/orders/{id}
Authorization: Bearer {jwt-token}

# Create order
POST /api/orders
Authorization: Bearer {jwt-token}
Content-Type: application/json

{
  "customerName": "John Doe",
  "customerEmail": "john@example.com",
  "customerPhone": "+1234567890",
  "customerAddress": "123 Main St, City, State 12345",
  "currency": "USD",
  "source": "Instagram",
  "deliveryDate": "2026-01-15T00:00:00Z",
  "items": [
    {
      "itemId": 1,
      "quantity": 2
    }
  ]
}

# Update order status
PUT /api/orders/{id}/status
Authorization: Bearer {jwt-token}
Content-Type: application/json

{
  "status": "Confirmed"
}
```

### Shipments

```http
# Create or update shipment
POST /api/orders/{orderId}/shipment
Authorization: Bearer {jwt-token}
Content-Type: application/json

{
  "awbNumber": "AWB123456789",
  "deliveryPartner": "FedEx",
  "status": "InTransit",
  "trackingUrl": "https://tracking.example.com/AWB123456789"
}

# Update shipment status
PUT /api/shipments/{id}/status
Authorization: Bearer {jwt-token}
Content-Type: application/json

{
  "status": "Delivered"
}
```

### Reports

```http
# Get sales report
GET /api/reports/sales?startDate=2026-01-01&endDate=2026-01-31&groupBy=day&currency=USD
Authorization: Bearer {jwt-token}

# Group by options: day, week, month, customer, source
```

## Currency Support

The API supports multiple currencies: USD, EUR, GBP, INR

Prices are stored as **minor units** (e.g., cents for USD) to avoid floating-point precision issues:
- $29.99 → 2999
- €15.50 → 1550
- £99.00 → 9900

## Order Number Format

Order numbers are auto-generated in the format: **ORD-YYYYMMDD-XXXX**

Examples:
- `ORD-20260106-0001`
- `ORD-20260106-0002`
- `ORD-20260107-0001`

The sequence resets daily.

## Caching Strategy

- **Items**: Cached for 10 minutes
- **Orders**: Cached for 5 minutes
- Cache keys include pagination and filter parameters
- Pattern-based invalidation on updates

## Security

- **Authentication**: Required for all endpoints except `/api/health` and `/api/auth/**`
- **Authorization**: JWT Bearer tokens
- **CORS**: Configured for cross-origin requests
- **Passwords**: Not stored (Google OAuth only)

## Development

### Running Tests

```bash
dotnet test
```

### Code Style

```bash
dotnet format
```

### Database Migrations

```bash
# Create new migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Rollback migration
dotnet ef database update PreviousMigrationName

# Remove last migration
dotnet ef migrations remove
```

## Deployment

### Docker

```bash
# Build image
docker build -t catalog-order-api .

# Run container
docker run -d -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Host=db;Database=catalogorderdb;Username=postgres;Password=yourpassword" \
  -e ConnectionStrings__Redis="redis:6379" \
  -e BlobStorage__ConnectionString="your-connection-string" \
  -e GoogleAuth__ClientId="your-client-id" \
  -e JwtSettings__SecretKey="your-secret-key" \
  catalog-order-api
```

### Environment Variables

All configuration can be overridden with environment variables using double underscore notation:

```bash
export ConnectionStrings__DefaultConnection="Host=...;Database=...;"
export JwtSettings__SecretKey="your-secret-key"
export GoogleAuth__ClientId="your-client-id"
```

## Monitoring

### Logging

Logs are written to:
- Console (structured JSON in production)
- Application Insights (if configured)

### Health Checks

```bash
curl http://localhost:5000/api/health
```

## Troubleshooting

### Database Connection Issues

```bash
# Test PostgreSQL connection
psql -h localhost -U postgres -d catalogorderdb

# Check if database exists
psql -U postgres -l
```

### Redis Connection Issues

```bash
# Test Redis connection
redis-cli ping

# Check Redis info
redis-cli info
```

### Blob Storage Issues

```bash
# Test Azurite connection
curl http://localhost:10000/devstoreaccount1?comp=list

# Or check Azure Storage in portal
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

MIT License

## Support

For issues and questions, please open an issue on GitHub.
