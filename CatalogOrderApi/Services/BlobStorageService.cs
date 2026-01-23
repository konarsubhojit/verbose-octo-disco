using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace CatalogOrderApi.Services;

public interface IBlobStorageService
{
    Task<string> UploadImageAsync(Stream imageStream, string fileName, string contentType);
    Task<bool> DeleteImageAsync(string blobName);
    Task<Stream> DownloadImageAsync(string blobName);
    string GetBlobUrl(string blobName);
}

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(
        BlobServiceClient blobServiceClient,
        IConfiguration configuration,
        ILogger<BlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _containerName = configuration["BlobStorage:ContainerName"] ?? "design-variants";
        _logger = logger;
    }

    public async Task<string> UploadImageAsync(Stream imageStream, string fileName, string contentType)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            // Generate unique blob name with original file extension
            var fileExtension = Path.GetExtension(fileName);
            var blobName = $"{Guid.NewGuid()}{fileExtension}";
            var blobClient = containerClient.GetBlobClient(blobName);

            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            };

            await blobClient.UploadAsync(imageStream, new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders
            });

            _logger.LogInformation("Uploaded blob: {BlobName}", blobName);
            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image to blob storage");
            throw;
        }
    }

    public async Task<bool> DeleteImageAsync(string blobName)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            
            var result = await blobClient.DeleteIfExistsAsync();
            _logger.LogInformation("Deleted blob: {BlobName}, Success: {Success}", blobName, result.Value);
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image from blob storage");
            return false;
        }
    }

    public async Task<Stream> DownloadImageAsync(string blobName)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            
            var response = await blobClient.DownloadAsync();
            return response.Value.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading image from blob storage");
            throw;
        }
    }

    public string GetBlobUrl(string blobName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        return blobClient.Uri.ToString();
    }
}
