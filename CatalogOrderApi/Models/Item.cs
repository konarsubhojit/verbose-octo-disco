namespace CatalogOrderApi.Models;

public class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long Price { get; set; } // Store as minor units (cents)
    public string Currency { get; set; } = "USD"; // USD, EUR, GBP, INR
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public ICollection<DesignVariant> DesignVariants { get; set; } = new List<DesignVariant>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
