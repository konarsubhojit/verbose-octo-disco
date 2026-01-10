using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CatalogOrderApi.Data;
using CatalogOrderApi.Models;
using CatalogOrderApi.DTOs;
using CatalogOrderApi.Services;

namespace CatalogOrderApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly IOrderNumberGenerator _orderNumberGenerator;
    private readonly ILogger<OrdersController> _logger;
    private const string CacheKeyPrefix = "order:";
    private const string AllOrdersCacheKey = "orders:all";

    public OrdersController(
        AppDbContext context,
        ICacheService cacheService,
        IOrderNumberGenerator orderNumberGenerator,
        ILogger<OrdersController> logger)
    {
        _context = context;
        _cacheService = cacheService;
        _orderNumberGenerator = orderNumberGenerator;
        _logger = logger;
    }

    // GET: api/orders?page=1&pageSize=20&status=&source=&customerName=
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders(
        [FromQuery][Range(1, int.MaxValue)] int page = 1,
        [FromQuery][Range(1, 100)] int pageSize = 20,
        [FromQuery] OrderStatus? status = null,
        [FromQuery] OrderSource? source = null,
        [FromQuery] string? customerName = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        _logger.LogInformation("Fetching orders - Page: {Page}, PageSize: {PageSize}", page, pageSize);

        var query = _context.Orders
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Item)
            .Include(o => o.Shipment)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        if (source.HasValue)
        {
            query = query.Where(o => o.Source == source.Value);
        }

        if (!string.IsNullOrEmpty(customerName))
        {
            query = query.Where(o => o.CustomerName.Contains(customerName));
        }

        if (startDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= endDate.Value);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var orderDtos = orders.Select(MapToDto).ToList();

        return Ok(orderDtos);
    }

    // GET: api/orders/5
    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetOrder(int id)
    {
        var cacheKey = $"{CacheKeyPrefix}{id}";
        var cachedOrder = await _cacheService.GetAsync<OrderDto>(cacheKey);
        if (cachedOrder != null)
        {
            return Ok(cachedOrder);
        }

        var order = await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Item)
            .Include(o => o.Shipment)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        var orderDto = MapToDto(order);
        await _cacheService.SetAsync(cacheKey, orderDto, TimeSpan.FromMinutes(5));

        return Ok(orderDto);
    }

    // POST: api/orders
    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto createOrderDto)
    {
        _logger.LogInformation("Creating new order for customer: {CustomerName}", createOrderDto.CustomerName);

        // Calculate order total
        long orderTotal = 0;
        var orderItems = new List<OrderItem>();
        var orderCurrency = createOrderDto.Currency;

        foreach (var itemDto in createOrderDto.Items)
        {
            var item = await _context.Items.FindAsync(itemDto.ItemId);
            if (item == null)
            {
                return BadRequest($"Item with ID {itemDto.ItemId} not found");
            }

            if (item.IsDeleted)
            {
                return BadRequest($"Item with ID {itemDto.ItemId} is deleted");
            }

            // Validate currency consistency
            if (item.Currency != orderCurrency)
            {
                return BadRequest($"Item with ID {itemDto.ItemId} has currency {item.Currency} but order currency is {orderCurrency}. All items must have the same currency.");
            }

            var lineTotal = item.Price * itemDto.Quantity;
            var orderItem = new OrderItem
            {
                ItemId = itemDto.ItemId,
                ItemName = item.Name,
                Quantity = itemDto.Quantity,
                UnitPrice = item.Price,
                LineTotal = lineTotal,
                Currency = item.Currency
            };
            orderItems.Add(orderItem);
            orderTotal += lineTotal;
        }

        // Generate order number
        var orderNumber = await _orderNumberGenerator.GenerateOrderNumberAsync();

        var order = new Order
        {
            OrderNumber = orderNumber,
            CustomerName = createOrderDto.CustomerName,
            CustomerEmail = createOrderDto.CustomerEmail,
            CustomerPhone = createOrderDto.CustomerPhone,
            CustomerAddress = createOrderDto.CustomerAddress,
            Currency = createOrderDto.Currency,
            Source = createOrderDto.Source,
            Status = OrderStatus.PendingConfirmation,
            DeliveryDate = createOrderDto.DeliveryDate,
            Items = orderItems,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Invalidate cache
        await _cacheService.RemoveByPatternAsync($"{AllOrdersCacheKey}*");

        var orderDto = MapToDto(order);
        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, orderDto);
    }

    // PUT: api/orders/5/status
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto updateDto)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        order.Status = updateDto.Status;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _cacheService.RemoveAsync($"{CacheKeyPrefix}{id}");
        await _cacheService.RemoveByPatternAsync($"{AllOrdersCacheKey}*");

        return NoContent();
    }

    private static OrderDto MapToDto(Order order)
    {
        var total = order.Items.Sum(oi => oi.LineTotal);
        
        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerName = order.CustomerName,
            CustomerEmail = order.CustomerEmail,
            CustomerPhone = order.CustomerPhone,
            CustomerAddress = order.CustomerAddress,
            Currency = order.Currency,
            Source = order.Source.ToString(),
            Status = order.Status.ToString(),
            DeliveryDate = order.DeliveryDate,
            Items = order.Items.Select(oi => new OrderItemDto
            {
                Id = oi.Id,
                ItemId = oi.ItemId,
                ItemName = oi.ItemName,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                LineTotal = oi.LineTotal,
                Currency = oi.Currency
            }).ToList(),
            Total = total,
            Shipment = order.Shipment != null ? new ShipmentDto
            {
                Id = order.Shipment.Id,
                AwbNumber = order.Shipment.AwbNumber,
                DeliveryPartner = order.Shipment.DeliveryPartner,
                Status = order.Shipment.Status.ToString(),
                TrackingUrl = order.Shipment.TrackingUrl,
                LastUpdatedAt = order.Shipment.LastUpdatedAt,
                CreatedAt = order.Shipment.CreatedAt
            } : null,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        };
    }
}
