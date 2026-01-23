namespace CatalogOrderApi.Models;

public class DesignVariant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty; // Blob storage URL
    public string? BlobName { get; set; } // Original blob name for deletion
    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
