using System.ComponentModel.DataAnnotations;

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
    [Required]
    [StringLength(255, MinimumLength = 1)]
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
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
    
    [Range(0, long.MaxValue, ErrorMessage = "Price must be non-negative")]
    public long Price { get; set; } // Minor units (e.g., cents)
    
    [Required]
    [RegularExpression("^(USD|EUR|GBP|INR)$", ErrorMessage = "Currency must be one of: USD, EUR, GBP, INR")]
    public string Currency { get; set; } = "USD";
    // Design variants with images will be added separately via upload endpoint
}

public class UpdateItemDto
{
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
    
    [Range(0, long.MaxValue, ErrorMessage = "Price must be non-negative")]
    public long Price { get; set; }
    
    [Required]
    [RegularExpression("^(USD|EUR|GBP|INR)$", ErrorMessage = "Currency must be one of: USD, EUR, GBP, INR")]
    public string Currency { get; set; } = "USD";
}
