# Skill: ASP.NET Core Blob Storage File Upload Endpoint

**Author:** Cinnamon (Backend Dev)  
**Date:** 2026-05-24T16:52:12.609-03:00  
**Context:** OutdoorsShop — `POST /api/v1/products/{id}/image`

---

## Pattern: IFormFile upload → Azure Blob Storage → persist URL

### Interface (Core layer — no ASP.NET dependency)

```csharp
// Keep IFormFile out of Core. Use Stream + metadata.
Task<string> UploadProductImageAsync(Stream imageStream, string fileName, string contentType, int productId);
```

### Service implementation (Infrastructure layer)

```csharp
public async Task<string> UploadProductImageAsync(Stream imageStream, string fileName, string contentType, int productId)
{
    var ext = Path.GetExtension(fileName);
    var blobName = $"products/{productId}/{Guid.NewGuid()}{ext}";
    const string containerName = "product-images";

    var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
    // PublicAccessType.Blob = URLs are publicly readable without SAS tokens
    await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

    var blobClient = containerClient.GetBlobClient(blobName);
    await blobClient.UploadAsync(imageStream, new BlobUploadOptions
    {
        HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
    });

    return blobClient.Uri.ToString();
}
```

### Controller endpoint

```csharp
[HttpPost("{id:int}/image")]
[Authorize(Roles = "Administrator")]
[Consumes("multipart/form-data")]
[ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> UploadImage(int id, IFormFile file)
{
    var product = await _productRepo.GetByIdAsync(id);
    if (product is null)
        return NotFound(new { message = $"Product {id} not found." });

    if (file is null || file.Length == 0)
        return BadRequest(new { message = "No file uploaded." });

    var allowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp"
    };
    if (!allowedContentTypes.Contains(file.ContentType))
        return BadRequest(new { message = "Invalid file type. Allowed types: jpg, jpeg, png, gif, webp." });

    const long MaxFileSize = 5 * 1024 * 1024; // 5 MB
    if (file.Length > MaxFileSize)
        return BadRequest(new { message = "File size exceeds the 5 MB limit." });

    using var stream = file.OpenReadStream();
    var imageUrl = await _blobStorage.UploadProductImageAsync(stream, file.FileName, file.ContentType, id);

    product.ImageUrl = imageUrl;
    await _productRepo.UpdateAsync(product);
    await _productRepo.SaveChangesAsync();

    return Ok(new { imageUrl });
}
```

### DI configuration

```csharp
// ServiceCollectionExtensions.cs
public static IServiceCollection AddBlobStorage(this IServiceCollection services, IConfiguration configuration)
{
    var connectionString = configuration["AzureStorage:ConnectionString"];
    if (!string.IsNullOrEmpty(connectionString))
        services.AddSingleton(new BlobServiceClient(connectionString));
    else
        services.AddSingleton(new BlobServiceClient("UseDevelopmentStorage=true"));

    services.AddScoped<IBlobStorageService, BlobStorageService>();
    return services;
}
```

### appsettings.json placeholder

```json
"AzureStorage": {
    "ConnectionString": "REPLACE_WITH_STORAGE_CONNECTION",
    "ProductImagesContainer": "product-images"
}
```

Inject real value via Azure App Service Application Setting: `AzureStorage__ConnectionString`

---

## Azure setup

```powershell
# Create container with public blob access
az storage container create `
  --name product-images `
  --account-name <storage-account> `
  --account-key $key `
  --public-access blob

# Set connection string in App Service
$connStr = az storage account show-connection-string --name <storage-account> --resource-group <rg> --query connectionString -o tsv
$settings = @(@{ name = "AzureStorage__ConnectionString"; value = $connStr }) | ConvertTo-Json
$settings | Out-File azure_settings.json -Encoding utf8
az webapp config appsettings set --name <app> --resource-group <rg> --settings "@azure_settings.json" --output none
Remove-Item azure_settings.json
```

---

## Testing mock (xUnit + Moq)

```csharp
// Unit test: add to controller constructor mock
private readonly Mock<IBlobStorageService> _blobStorage = new();

var controller = new ProductsController(
    _productRepo.Object, _inventoryRepo.Object, _categoryRepo.Object, _blobStorage.Object);

// TestWebAppFactory: replace real service
blobMock.Setup(b => b.UploadProductImageAsync(
    It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
    .ReturnsAsync("https://test.blob.core.windows.net/product-images/products/1/test.jpg");
```

---

## Key gotchas

- **`[Consumes("multipart/form-data")]` returns 415 before auth** — when smoke-testing auth (401/403), include the correct content type header, or the route won't match and you'll get 415.
- **Blob name with GUID = no auto-overwrite** — uploading a new image for the same product creates a new blob; the old blob remains. If you want auto-overwrite, use a deterministic name like `products/{productId}{ext}`.
- **`PublicAccessType.Blob` vs `.None`** — product images need `.Blob` for anonymous frontend access; use `.None` for receipts/reports that require SAS tokens.
