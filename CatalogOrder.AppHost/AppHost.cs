var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .AddDatabase("catalogorderdb");

// Add Redis cache
var redis = builder.AddRedis("redis");

// Add the API service
var api = builder.AddProject<Projects.CatalogOrderApi>("catalogorderapi")
    .WithReference(postgres)
    .WithReference(redis)
    .WithExternalHttpEndpoints();

builder.Build().Run();
