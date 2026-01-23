using Microsoft.EntityFrameworkCore;
using CatalogOrderApi.Models;

namespace CatalogOrderApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<DesignVariant> DesignVariants { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Shipment> Shipments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GoogleSub).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.GoogleSub).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Email);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
        });

        // Item configuration
        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.HasIndex(e => e.IsDeleted);
            
            entity.HasMany(e => e.DesignVariants)
                .WithOne(e => e.Item)
                .HasForeignKey(e => e.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(e => e.OrderItems)
                .WithOne(e => e.Item)
                .HasForeignKey(e => e.ItemId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // DesignVariant configuration
        modelBuilder.Entity<DesignVariant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ImageUrl).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.BlobName).HasMaxLength(500);
        });

        // Order configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CustomerEmail).HasMaxLength(255);
            entity.Property(e => e.CustomerPhone).HasMaxLength(50);
            entity.Property(e => e.CustomerAddress).HasMaxLength(1000);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            
            entity.HasMany(e => e.Items)
                .WithOne(e => e.Order)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Shipment)
                .WithOne(e => e.Order)
                .HasForeignKey<Shipment>(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // OrderItem configuration
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ItemName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
        });

        // Shipment configuration
        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AwbNumber).HasMaxLength(100);
            entity.Property(e => e.DeliveryPartner).HasMaxLength(100);
            entity.Property(e => e.TrackingUrl).HasMaxLength(500);
            entity.HasIndex(e => e.Status);
        });
    }
}
