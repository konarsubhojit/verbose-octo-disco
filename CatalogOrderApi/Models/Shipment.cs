namespace CatalogOrderApi.Models;

public class Shipment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public string AwbNumber { get; set; } = string.Empty;
    public string DeliveryPartner { get; set; } = string.Empty;
    public ShipmentStatus Status { get; set; }
    public string? TrackingUrl { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum ShipmentStatus
{
    Pending,
    PickedUp,
    InTransit,
    OutForDelivery,
    Delivered,
    Failed,
    Returned
}
