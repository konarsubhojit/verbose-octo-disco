using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CatalogOrderApi.Data;
using CatalogOrderApi.Models;
using CatalogOrderApi.DTOs;
using CatalogOrderApi.Services;

namespace CatalogOrderApi.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class ShipmentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ShipmentsController> _logger;

    public ShipmentsController(
        AppDbContext context,
        ICacheService cacheService,
        ILogger<ShipmentsController> logger)
    {
        _context = context;
        _cacheService = cacheService;
        _logger = logger;
    }

    // POST: api/orders/5/shipment
    [HttpPost("orders/{orderId}/shipment")]
    public async Task<ActionResult<ShipmentDto>> CreateOrUpdateShipment(
        int orderId,
        [FromBody] CreateShipmentDto createShipmentDto)
    {
        var order = await _context.Orders
            .Include(o => o.Shipment)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return NotFound($"Order with ID {orderId} not found");
        }

        if (order.Shipment == null)
        {
            // Create new shipment
            var shipment = new Shipment
            {
                OrderId = orderId,
                AwbNumber = createShipmentDto.AwbNumber,
                DeliveryPartner = createShipmentDto.DeliveryPartner,
                Status = createShipmentDto.Status,
                TrackingUrl = createShipmentDto.TrackingUrl,
                LastUpdatedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.Shipments.Add(shipment);
            order.Status = OrderStatus.Shipped;
        }
        else
        {
            // Update existing shipment
            order.Shipment.AwbNumber = createShipmentDto.AwbNumber;
            order.Shipment.DeliveryPartner = createShipmentDto.DeliveryPartner;
            order.Shipment.Status = createShipmentDto.Status;
            order.Shipment.TrackingUrl = createShipmentDto.TrackingUrl;
            order.Shipment.LastUpdatedAt = DateTime.UtcNow;
        }

        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Invalidate cache
        await _cacheService.RemoveAsync($"order:{orderId}");
        await _cacheService.RemoveByPatternAsync("orders:all*");

        var shipmentDto = new ShipmentDto
        {
            Id = order.Shipment!.Id,
            AwbNumber = order.Shipment.AwbNumber,
            DeliveryPartner = order.Shipment.DeliveryPartner,
            Status = order.Shipment.Status.ToString(),
            TrackingUrl = order.Shipment.TrackingUrl,
            LastUpdatedAt = order.Shipment.LastUpdatedAt,
            CreatedAt = order.Shipment.CreatedAt
        };

        return Ok(shipmentDto);
    }

    // PUT: api/shipments/5/status
    [HttpPut("shipments/{id}/status")]
    public async Task<IActionResult> UpdateShipmentStatus(
        int id,
        [FromBody] UpdateShipmentStatusDto updateDto)
    {
        var shipment = await _context.Shipments
            .Include(s => s.Order)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (shipment == null)
        {
            return NotFound();
        }

        shipment.Status = updateDto.Status;
        shipment.LastUpdatedAt = DateTime.UtcNow;
        shipment.Order.UpdatedAt = DateTime.UtcNow;

        // Update order status based on shipment status
        if (updateDto.Status == ShipmentStatus.Delivered)
        {
            shipment.Order.Status = OrderStatus.Delivered;
        }

        await _context.SaveChangesAsync();

        // Invalidate cache
        await _cacheService.RemoveAsync($"order:{shipment.OrderId}");
        await _cacheService.RemoveByPatternAsync("orders:all*");

        return NoContent();
    }
}
