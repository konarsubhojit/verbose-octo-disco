using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CatalogOrderApi.Data;
using CatalogOrderApi.Models;
using CatalogOrderApi.DTOs;
using CatalogOrderApi.Services;

namespace CatalogOrderApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly IConcurrencyService _concurrencyService;
    private readonly IOrderNumberGenerator _orderNumberGenerator;
    private readonly ILogger<OrdersController> _logger;
    private const string CacheKeyPrefix = "order:";
    private const string AllOrdersCacheKey = "orders:all";

    public OrdersController(
        AppDbContext context,
        ICacheService cacheService,
        IConcurrencyService concurrencyService,
        IOrderNumberGenerator orderNumberGenerator,
        ILogger<OrdersController> logger)
    {
        _context = context;
        _cacheService = cacheService;
        _concurrencyService = concurrencyService;
        _orderNumberGenerator = orderNumberGenerator;
        _logger = logger;
    }

    // GET: api/orders
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders()
    {
        _logger.LogInformation("Fetching all orders");

        return await _concurrencyService.ExecuteWithSemaphoreAsync("orders:read", async () =>
        {
            // Try to get from cache
            var cachedOrders = await _cacheService.GetAsync<List<OrderDto>>(AllOrdersCacheKey);
            if (cachedOrders != null)
            {
                _logger.LogInformation("Returning orders from cache");
                return Ok(cachedOrders) as ActionResult<IEnumerable<OrderDto>>;
            }

            // Get from database
            var orders = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Item)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var orderDtos = orders.Select(MapToDto).ToList();

            // Cache the results
            await _cacheService.SetAsync(AllOrdersCacheKey, orderDtos, TimeSpan.FromMinutes(5));

            return Ok(orderDtos) as ActionResult<IEnumerable<OrderDto>>;
        });
    }

    // GET: api/orders/5
    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetOrder(int id)
    {
        _logger.LogInformation("Fetching order with ID: {OrderId}", id);

        return await _concurrencyService.ExecuteWithSemaphoreAsync($"order:{id}:read", async () =>
        {
            var cacheKey = $"{CacheKeyPrefix}{id}";

            // Try to get from cache
            var cachedOrder = await _cacheService.GetAsync<OrderDto>(cacheKey);
            if (cachedOrder != null)
            {
                _logger.LogInformation("Returning order from cache");
                return Ok(cachedOrder) as ActionResult<OrderDto>;
            }

            // Get from database
            var order = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Item)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound() as ActionResult<OrderDto>;
            }

            var orderDto = MapToDto(order);

            // Cache the result
            await _cacheService.SetAsync(cacheKey, orderDto, TimeSpan.FromMinutes(5));

            return Ok(orderDto) as ActionResult<OrderDto>;
        });
    }

    // GET: api/orders/by-number/OD0001
    [HttpGet("by-number/{orderNumber}")]
    public async Task<ActionResult<OrderDto>> GetOrderByNumber(string orderNumber)
    {
        _logger.LogInformation("Fetching order with number: {OrderNumber}", orderNumber);

        return await _concurrencyService.ExecuteWithSemaphoreAsync($"order:number:{orderNumber}:read", async () =>
        {
            var cacheKey = $"{CacheKeyPrefix}number:{orderNumber}";

            // Try to get from cache
            var cachedOrder = await _cacheService.GetAsync<OrderDto>(cacheKey);
            if (cachedOrder != null)
            {
                _logger.LogInformation("Returning order from cache");
                return Ok(cachedOrder) as ActionResult<OrderDto>;
            }

            // Get from database
            var order = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Item)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

            if (order == null)
            {
                return NotFound() as ActionResult<OrderDto>;
            }

            var orderDto = MapToDto(order);

            // Cache the result
            await _cacheService.SetAsync(cacheKey, orderDto, TimeSpan.FromMinutes(5));

            return Ok(orderDto) as ActionResult<OrderDto>;
        });
    }

    // POST: api/orders
    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderDto createOrderDto)
    {
        _logger.LogInformation("Creating new order for customer: {CustomerName}", createOrderDto.CustomerName);

        return await _concurrencyService.ExecuteWithSemaphoreAsync("orders:write", async () =>
        {
            // Calculate order total
            decimal orderTotal = 0;
            var orderItems = new List<OrderItem>();

            foreach (var itemDto in createOrderDto.Items)
            {
                var item = await _context.Items.FindAsync(itemDto.ItemId);
                if (item == null)
                {
                    return BadRequest($"Item with ID {itemDto.ItemId} not found") as ActionResult<OrderDto>;
                }

                var orderItem = new OrderItem
                {
                    ItemId = itemDto.ItemId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = item.Price
                };
                orderItems.Add(orderItem);
                orderTotal += item.Price * itemDto.Quantity;
            }

            // Generate order number
            var orderNumber = await _orderNumberGenerator.GenerateOrderNumberAsync();

            var order = new Order
            {
                OrderNumber = orderNumber,
                CustomerName = createOrderDto.CustomerName,
                ReceivedFrom = createOrderDto.ReceivedFrom,
                ExpectedShippingDate = createOrderDto.ExpectedShippingDate,
                Status = OrderStatus.PendingConfirmation,
                Items = orderItems,
                OrderTotal = orderTotal,
                CustomerNotes = createOrderDto.CustomerNotes,
                Priority = createOrderDto.Priority,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Invalidate cache
            await _cacheService.RemoveAsync(AllOrdersCacheKey);

            var orderDto = MapToDto(order);
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, orderDto) as ActionResult<OrderDto>;
        });
    }

    // PUT: api/orders/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrder(int id, UpdateOrderDto updateOrderDto)
    {
        _logger.LogInformation("Updating order with ID: {OrderId}", id);

        return await _concurrencyService.ExecuteWithSemaphoreAsync($"order:{id}:write", async () =>
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound() as IActionResult;
            }

            order.CustomerName = updateOrderDto.CustomerName;
            order.ReceivedFrom = updateOrderDto.ReceivedFrom;
            order.ExpectedShippingDate = updateOrderDto.ExpectedShippingDate;
            order.Status = updateOrderDto.Status;
            order.CustomerNotes = updateOrderDto.CustomerNotes;
            order.Priority = updateOrderDto.Priority;
            order.UpdatedAt = DateTime.UtcNow;

            // Update order items
            _context.OrderItems.RemoveRange(order.Items);
            
            decimal orderTotal = 0;
            var newOrderItems = new List<OrderItem>();

            foreach (var itemDto in updateOrderDto.Items)
            {
                var item = await _context.Items.FindAsync(itemDto.ItemId);
                if (item == null)
                {
                    return BadRequest($"Item with ID {itemDto.ItemId} not found") as IActionResult;
                }

                var orderItem = new OrderItem
                {
                    OrderId = id,
                    ItemId = itemDto.ItemId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = item.Price
                };
                newOrderItems.Add(orderItem);
                orderTotal += item.Price * itemDto.Quantity;
            }

            order.Items = newOrderItems;
            order.OrderTotal = orderTotal;

            await _context.SaveChangesAsync();

            // Invalidate cache
            await _cacheService.RemoveAsync($"{CacheKeyPrefix}{id}");
            await _cacheService.RemoveAsync($"{CacheKeyPrefix}number:{order.OrderNumber}");
            await _cacheService.RemoveAsync(AllOrdersCacheKey);

            return NoContent() as IActionResult;
        });
    }

    // DELETE: api/orders/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        _logger.LogInformation("Deleting order with ID: {OrderId}", id);

        return await _concurrencyService.ExecuteWithSemaphoreAsync($"order:{id}:delete", async () =>
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound() as IActionResult;
            }

            var orderNumber = order.OrderNumber;
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            // Invalidate cache
            await _cacheService.RemoveAsync($"{CacheKeyPrefix}{id}");
            await _cacheService.RemoveAsync($"{CacheKeyPrefix}number:{orderNumber}");
            await _cacheService.RemoveAsync(AllOrdersCacheKey);

            return NoContent() as IActionResult;
        });
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerName = order.CustomerName,
            ReceivedFrom = order.ReceivedFrom.ToString(),
            ExpectedShippingDate = order.ExpectedShippingDate,
            Status = order.Status.ToString(),
            Items = order.Items.Select(oi => new OrderItemDto
            {
                Id = oi.Id,
                ItemId = oi.ItemId,
                ItemName = oi.Item?.Name ?? "",
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice
            }).ToList(),
            OrderTotal = order.OrderTotal,
            CustomerNotes = order.CustomerNotes,
            Priority = order.Priority.ToString(),
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        };
    }
}
