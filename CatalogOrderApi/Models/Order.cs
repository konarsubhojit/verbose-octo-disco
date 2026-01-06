namespace CatalogOrderApi.Models;

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty; // Format: ORD-YYYYMMDD-XXXX
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public OrderSource Source { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public Shipment? Shipment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum OrderSource
{
    Instagram,
    Facebook,
    Call,
    Offline,
    Website
}

public enum OrderStatus
{
    PendingConfirmation,
    Confirmed,
    InProgress,
    Shipped,
    Delivered,
    Cancelled
}
