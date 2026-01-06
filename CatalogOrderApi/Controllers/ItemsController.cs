using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CatalogOrderApi.Data;
using CatalogOrderApi.Models;
using CatalogOrderApi.DTOs;
using CatalogOrderApi.Services;

namespace CatalogOrderApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly IConcurrencyService _concurrencyService;
    private readonly ILogger<ItemsController> _logger;
    private const string CacheKeyPrefix = "item:";
    private const string AllItemsCacheKey = "items:all";

    public ItemsController(
        AppDbContext context,
        ICacheService cacheService,
        IConcurrencyService concurrencyService,
        ILogger<ItemsController> logger)
    {
        _context = context;
        _cacheService = cacheService;
        _concurrencyService = concurrencyService;
        _logger = logger;
    }

    // GET: api/items
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ItemDto>>> GetItems()
    {
        _logger.LogInformation("Fetching all items");

        return await _concurrencyService.ExecuteWithSemaphoreAsync("items:read", async () =>
        {
            // Try to get from cache
            var cachedItems = await _cacheService.GetAsync<List<ItemDto>>(AllItemsCacheKey);
            if (cachedItems != null)
            {
                _logger.LogInformation("Returning items from cache");
                return Ok(cachedItems) as ActionResult<IEnumerable<ItemDto>>;
            }

            // Get from database
            var items = await _context.Items
                .Include(i => i.DesignVariants)
                .ToListAsync();

            var itemDtos = items.Select(MapToDto).ToList();

            // Cache the results
            await _cacheService.SetAsync(AllItemsCacheKey, itemDtos, TimeSpan.FromMinutes(10));

            return Ok(itemDtos) as ActionResult<IEnumerable<ItemDto>>;
        });
    }

    // GET: api/items/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ItemDto>> GetItem(int id)
    {
        _logger.LogInformation("Fetching item with ID: {ItemId}", id);

        return await _concurrencyService.ExecuteWithSemaphoreAsync($"item:{id}:read", async () =>
        {
            var cacheKey = $"{CacheKeyPrefix}{id}";
            
            // Try to get from cache
            var cachedItem = await _cacheService.GetAsync<ItemDto>(cacheKey);
            if (cachedItem != null)
            {
                _logger.LogInformation("Returning item from cache");
                return Ok(cachedItem) as ActionResult<ItemDto>;
            }

            // Get from database
            var item = await _context.Items
                .Include(i => i.DesignVariants)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null)
            {
                return NotFound() as ActionResult<ItemDto>;
            }

            var itemDto = MapToDto(item);

            // Cache the result
            await _cacheService.SetAsync(cacheKey, itemDto, TimeSpan.FromMinutes(10));

            return Ok(itemDto) as ActionResult<ItemDto>;
        });
    }

    // POST: api/items
    [HttpPost]
    public async Task<ActionResult<ItemDto>> CreateItem(CreateItemDto createItemDto)
    {
        _logger.LogInformation("Creating new item: {ItemName}", createItemDto.Name);

        return await _concurrencyService.ExecuteWithSemaphoreAsync("items:write", async () =>
        {
            var item = new Item
            {
                Name = createItemDto.Name,
                Price = createItemDto.Price,
                Features = createItemDto.Features,
                DesignVariants = createItemDto.DesignVariants.Select(dv => new DesignVariant
                {
                    Name = dv.Name,
                    PhotoUrl = dv.PhotoUrl
                }).ToList()
            };

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            // Invalidate cache
            await _cacheService.RemoveAsync(AllItemsCacheKey);

            var itemDto = MapToDto(item);
            return CreatedAtAction(nameof(GetItem), new { id = item.Id }, itemDto) as ActionResult<ItemDto>;
        });
    }

    // PUT: api/items/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateItem(int id, UpdateItemDto updateItemDto)
    {
        _logger.LogInformation("Updating item with ID: {ItemId}", id);

        return await _concurrencyService.ExecuteWithSemaphoreAsync($"item:{id}:write", async () =>
        {
            var item = await _context.Items
                .Include(i => i.DesignVariants)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null)
            {
                return NotFound() as IActionResult;
            }

            item.Name = updateItemDto.Name;
            item.Price = updateItemDto.Price;
            item.Features = updateItemDto.Features;

            // Update design variants
            _context.DesignVariants.RemoveRange(item.DesignVariants);
            item.DesignVariants = updateItemDto.DesignVariants.Select(dv => new DesignVariant
            {
                Name = dv.Name,
                PhotoUrl = dv.PhotoUrl,
                ItemId = id
            }).ToList();

            await _context.SaveChangesAsync();

            // Invalidate cache
            await _cacheService.RemoveAsync($"{CacheKeyPrefix}{id}");
            await _cacheService.RemoveAsync(AllItemsCacheKey);

            return NoContent() as IActionResult;
        });
    }

    // DELETE: api/items/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        _logger.LogInformation("Deleting item with ID: {ItemId}", id);

        return await _concurrencyService.ExecuteWithSemaphoreAsync($"item:{id}:delete", async () =>
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null)
            {
                return NotFound() as IActionResult;
            }

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();

            // Invalidate cache
            await _cacheService.RemoveAsync($"{CacheKeyPrefix}{id}");
            await _cacheService.RemoveAsync(AllItemsCacheKey);

            return NoContent() as IActionResult;
        });
    }

    private static ItemDto MapToDto(Item item)
    {
        return new ItemDto
        {
            Id = item.Id,
            Name = item.Name,
            Price = item.Price,
            Features = item.Features,
            DesignVariants = item.DesignVariants.Select(dv => new DesignVariantDto
            {
                Id = dv.Id,
                Name = dv.Name,
                PhotoUrl = dv.PhotoUrl
            }).ToList()
        };
    }
}
