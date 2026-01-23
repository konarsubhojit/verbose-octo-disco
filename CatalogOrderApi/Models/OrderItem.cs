namespace CatalogOrderApi.Models;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int? ItemId { get; set; } // Nullable for soft-deleted items
    public Item? Item { get; set; }
    public string ItemName { get; set; } = string.Empty; // Snapshot
    public long UnitPrice { get; set; } // Snapshot in minor units
    public int Quantity { get; set; }
    public long LineTotal { get; set; } // Computed in minor units
    public string Currency { get; set; } = "USD";
}
