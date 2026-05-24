namespace OutdoorsShop.Core.Interfaces;

public interface IBlobStorageService
{
    Task<string> UploadAsync(string containerName, string blobName, Stream content, string contentType);
    Task DeleteAsync(string containerName, string blobName);
    Task<string> GetSasUrlAsync(string containerName, string blobName, TimeSpan expiry);

    /// <summary>
    /// Uploads a product image to the product-images container with public read access.
    /// Blob name: products/{productId}/{newGuid}{ext}
    /// </summary>
    Task<string> UploadProductImageAsync(Stream imageStream, string fileName, string contentType, int productId);
}
