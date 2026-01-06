namespace CatalogOrderApi.DTOs;

public class DesignVariantDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateDesignVariantDto
{
    public string Name { get; set; } = string.Empty;
    // Image will be uploaded as IFormFile in the controller
}

public class ItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long Price { get; set; } // Minor units
    public string Currency { get; set; } = "USD";
    public List<DesignVariantDto> DesignVariants { get; set; } = new();
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateItemDto
{
    public string Name { get; set; } = string.Empty;
    public long Price { get; set; } // Minor units (e.g., cents)
    public string Currency { get; set; } = "USD";
    // Design variants with images will be added separately via upload endpoint
}

public class UpdateItemDto
{
    public string Name { get; set; } = string.Empty;
    public long Price { get; set; }
    public string Currency { get; set; } = "USD";
}
