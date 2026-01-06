using CatalogOrderApi.Models;

namespace CatalogOrderApi.DTOs;

public class OrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? DeliveryDate { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public long Total { get; set; } // Computed in minor units
    public ShipmentDto? Shipment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class OrderItemDto
{
    public int Id { get; set; }
    public int? ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public long UnitPrice { get; set; }
    public int Quantity { get; set; }
    public long LineTotal { get; set; }
    public string Currency { get; set; } = "USD";
}

public class CreateOrderDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public OrderSource Source { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public List<CreateOrderItemDto> Items { get; set; } = new();
}

public class CreateOrderItemDto
{
    public int ItemId { get; set; }
    public int Quantity { get; set; }
}

public class UpdateOrderStatusDto
{
    public OrderStatus Status { get; set; }
}

public class ShipmentDto
{
    public int Id { get; set; }
    public string AwbNumber { get; set; } = string.Empty;
    public string DeliveryPartner { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? TrackingUrl { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateShipmentDto
{
    public string AwbNumber { get; set; } = string.Empty;
    public string DeliveryPartner { get; set; } = string.Empty;
    public ShipmentStatus Status { get; set; }
    public string? TrackingUrl { get; set; }
}

public class UpdateShipmentStatusDto
{
    public ShipmentStatus Status { get; set; }
}
