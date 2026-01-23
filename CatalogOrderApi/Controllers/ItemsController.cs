using System.ComponentModel.DataAnnotations;
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
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<ItemsController> _logger;
    private const string CacheKeyPrefix = "item:";
    private const string AllItemsCacheKey = "items:all";

    public ItemsController(
        AppDbContext context,
        ICacheService cacheService,
        IBlobStorageService blobStorageService,
        ILogger<ItemsController> logger)
    {
        _context = context;
        _cacheService = cacheService;
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    // GET: api/items?page=1&pageSize=20&includeDeleted=false
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ItemDto>>> GetItems(
        [FromQuery][Range(1, int.MaxValue)] int page = 1,
        [FromQuery][Range(1, 100)] int pageSize = 20,
        [FromQuery] bool includeDeleted = false)
    {
        _logger.LogInformation("Fetching items - Page: {Page}, PageSize: {PageSize}", page, pageSize);

        var cacheKey = $"{AllItemsCacheKey}:p{page}:ps{pageSize}:del{includeDeleted}";

        var cachedItems = await _cacheService.GetAsync<List<ItemDto>>(cacheKey);
        if (cachedItems != null)
        {
            return Ok(cachedItems);
        }

        var query = _context.Items
            .Include(i => i.DesignVariants)
            .AsQueryable();

        if (!includeDeleted)
        {
            query = query.Where(i => !i.IsDeleted);
        }

        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var itemDtos = items.Select(MapToDto).ToList();
        await _cacheService.SetAsync(cacheKey, itemDtos, TimeSpan.FromMinutes(10));

        return Ok(itemDtos);
    }

    // GET: api/items/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ItemDto>> GetItem(int id)
    {
        var cacheKey = $"{CacheKeyPrefix}{id}";
        var cachedItem = await _cacheService.GetAsync<ItemDto>(cacheKey);
        if (cachedItem != null)
        {
            return Ok(cachedItem);
        }

        var item = await _context.Items
            .Include(i => i.DesignVariants)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (item == null)
        {
            return NotFound();
        }

        var itemDto = MapToDto(item);
        await _cacheService.SetAsync(cacheKey, itemDto, TimeSpan.FromMinutes(10));

        return Ok(itemDto);
    }

    // POST: api/items
    [HttpPost]
    public async Task<ActionResult<ItemDto>> CreateItem([FromBody] CreateItemDto createItemDto)
    {
        var item = new Item
        {
            Name = createItemDto.Name,
            Price = createItemDto.Price,
            Currency = createItemDto.Currency,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        await _cacheService.RemoveByPatternAsync($"{AllItemsCacheKey}*");

        var itemDto = MapToDto(item);
        return CreatedAtAction(nameof(GetItem), new { id = item.Id }, itemDto);
    }

    // POST: api/items/5/variants (with image upload)
    [HttpPost("{itemId}/variants")]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5 MB limit
    public async Task<ActionResult<DesignVariantDto>> AddDesignVariant(
        int itemId,
        [FromForm] string name,
        [FromForm] IFormFile image)
    {
        const long MaxFileSize = 5 * 1024 * 1024; // 5 MB
        
        if (image == null || image.Length == 0)
        {
            return BadRequest("Image file is required");
        }

        if (image.Length > MaxFileSize)
        {
            return BadRequest($"File size exceeds maximum allowed size of {MaxFileSize / (1024 * 1024)} MB");
        }

        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(image.ContentType.ToLower()))
        {
            return BadRequest("Only image files are allowed");
        }

        var item = await _context.Items.FindAsync(itemId);
        if (item == null)
        {
            return NotFound($"Item with ID {itemId} not found");
        }

        string imageUrl;
        string blobName;
        using (var stream = image.OpenReadStream())
        {
            imageUrl = await _blobStorageService.UploadImageAsync(stream, image.FileName, image.ContentType);
            blobName = imageUrl.Split('/').Last();
        }

        var variant = new DesignVariant
        {
            Name = name,
            ImageUrl = imageUrl,
            BlobName = blobName,
            ItemId = itemId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.DesignVariants.Add(variant);
        item.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _cacheService.RemoveAsync($"{CacheKeyPrefix}{itemId}");
        await _cacheService.RemoveByPatternAsync($"{AllItemsCacheKey}*");

        return Ok(new DesignVariantDto
        {
            Id = variant.Id,
            Name = variant.Name,
            ImageUrl = variant.ImageUrl,
            CreatedAt = variant.CreatedAt,
            UpdatedAt = variant.UpdatedAt
        });
    }

    // DELETE: api/items/5/variants/3
    [HttpDelete("{itemId}/variants/{variantId}")]
    public async Task<IActionResult> DeleteDesignVariant(int itemId, int variantId)
    {
        var variant = await _context.DesignVariants
            .FirstOrDefaultAsync(v => v.Id == variantId && v.ItemId == itemId);

        if (variant == null)
        {
            return NotFound();
        }

        if (!string.IsNullOrEmpty(variant.BlobName))
        {
            await _blobStorageService.DeleteImageAsync(variant.BlobName);
        }

        _context.DesignVariants.Remove(variant);
        
        var item = await _context.Items.FindAsync(itemId);
        if (item != null)
        {
            item.UpdatedAt = DateTime.UtcNow;
        }
        
        await _context.SaveChangesAsync();

        await _cacheService.RemoveAsync($"{CacheKeyPrefix}{itemId}");
        await _cacheService.RemoveByPatternAsync($"{AllItemsCacheKey}*");

        return NoContent();
    }

    // PUT: api/items/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateItem(int id, [FromBody] UpdateItemDto updateItemDto)
    {
        var item = await _context.Items.FindAsync(id);
        if (item == null)
        {
            return NotFound();
        }

        item.Name = updateItemDto.Name;
        item.Price = updateItemDto.Price;
        item.Currency = updateItemDto.Currency;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _cacheService.RemoveAsync($"{CacheKeyPrefix}{id}");
        await _cacheService.RemoveByPatternAsync($"{AllItemsCacheKey}*");

        return NoContent();
    }

    // DELETE: api/items/5 (soft delete)
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        var item = await _context.Items.FindAsync(id);
        if (item == null)
        {
            return NotFound();
        }

        item.IsDeleted = true;
        item.DeletedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _cacheService.RemoveAsync($"{CacheKeyPrefix}{id}");
        await _cacheService.RemoveByPatternAsync($"{AllItemsCacheKey}*");

        return NoContent();
    }

    // POST: api/items/5/restore
    [HttpPost("{id}/restore")]
    public async Task<IActionResult> RestoreItem(int id)
    {
        var item = await _context.Items.FindAsync(id);
        if (item == null)
        {
            return NotFound();
        }

        item.IsDeleted = false;
        item.DeletedAt = null;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _cacheService.RemoveAsync($"{CacheKeyPrefix}{id}");
        await _cacheService.RemoveByPatternAsync($"{AllItemsCacheKey}*");

        return NoContent();
    }

    private static ItemDto MapToDto(Item item)
    {
        return new ItemDto
        {
            Id = item.Id,
            Name = item.Name,
            Price = item.Price,
            Currency = item.Currency,
            DesignVariants = item.DesignVariants.Select(v => new DesignVariantDto
            {
                Id = v.Id,
                Name = v.Name,
                ImageUrl = v.ImageUrl,
                CreatedAt = v.CreatedAt,
                UpdatedAt = v.UpdatedAt
            }).ToList(),
            IsDeleted = item.IsDeleted,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
