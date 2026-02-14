# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Install and update CA certificates to fix SSL issues
RUN apt-get update && \
    apt-get install -y --no-install-recommends ca-certificates && \
    update-ca-certificates && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /src

# Copy csproj and restore dependencies  
COPY ["CatalogOrderApi/CatalogOrderApi.csproj", "CatalogOrderApi/"]
RUN dotnet restore "CatalogOrderApi/CatalogOrderApi.csproj"

# Copy the rest of the code and build
COPY CatalogOrderApi/ CatalogOrderApi/
WORKDIR /src/CatalogOrderApi
RUN dotnet build "CatalogOrderApi.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "CatalogOrderApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage  
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CatalogOrderApi.dll"]
