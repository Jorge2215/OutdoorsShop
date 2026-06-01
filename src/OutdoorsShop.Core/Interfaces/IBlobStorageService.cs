namespace OutdoorsShop.Core.Interfaces;

public interface IBlobStorageService
{
    Task<string> UploadAsync(string containerName, string blobName, Stream content, string contentType);
    Task<string> UploadPublicAsync(string containerName, string blobName, Stream content, string contentType);
    Task DeleteAsync(string containerName, string blobName);
    Task<bool> ExistsAsync(string containerName, string blobName);
    Task<string> GetSasUrlAsync(string containerName, string blobName, TimeSpan expiry);
    string GetBlobUrl(string containerName, string blobName);

    /// <summary>
    /// Uploads a product image to the product-images container with public read access.
    /// Blob name: products/{productId}/{newGuid}{ext}
    /// </summary>
    Task<string> UploadProductImageAsync(Stream imageStream, string fileName, string contentType, int productId);
}
