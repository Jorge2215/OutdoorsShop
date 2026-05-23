namespace OutdoorsShop.Core.Interfaces;

public interface IBlobStorageService
{
    Task<string> UploadAsync(string containerName, string blobName, Stream content, string contentType);
    Task DeleteAsync(string containerName, string blobName);
    Task<string> GetSasUrlAsync(string containerName, string blobName, TimeSpan expiry);
}
