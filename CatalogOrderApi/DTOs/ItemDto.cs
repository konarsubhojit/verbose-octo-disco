namespace CatalogOrderApi.DTOs;

public class ItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long Price { get; set; } // Minor units
    public string Currency { get; set; } = "USD";
    public string? ImageUrl { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateItemDto
{
    public string Name { get; set; } = string.Empty;
    public long Price { get; set; } // Minor units (e.g., cents)
    public string Currency { get; set; } = "USD";
    public string? ImageUrl { get; set; }
}

public class UpdateItemDto
{
    public string Name { get; set; } = string.Empty;
    public long Price { get; set; }
    public string Currency { get; set; } = "USD";
    public string? ImageUrl { get; set; }
}
