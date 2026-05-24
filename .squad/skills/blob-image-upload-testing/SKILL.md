# Skill: Azure Blob Storage Image Upload Testing

**Author:** Creta (Test Engineer)  
**Date:** 2026-05-24T16:52:12.609-03:00  
**Context:** OutdoorsShop — `POST /api/v1/products/{id}/image`

---

## Pattern: Testing Multipart Image Upload Endpoints

### Minimal test image files (PowerShell)

```powershell
# 1×1 pixel JPEG (valid, 0.02 KB)
[byte[]]$jpg = 0xFF,0xD8,0xFF,0xE0,0x00,0x10,0x4A,0x46,0x49,0x46,0x00,0x01,
               0x01,0x00,0x00,0x01,0x00,0x01,0x00,0x00,0xFF,0xD9
[IO.File]::WriteAllBytes("test_1x1.jpg", $jpg)

# 1×1 pixel PNG (valid, minimal)
[byte[]]$png = 0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A,
               0x00,0x00,0x00,0x0D,0x49,0x48,0x44,0x52,
               0x00,0x00,0x00,0x01,0x00,0x00,0x00,0x01,
               0x08,0x02,0x00,0x00,0x00,0x90,0x77,0x53,0xDE,
               0x00,0x00,0x00,0x0C,0x49,0x44,0x41,0x54,
               0x08,0xD7,0x63,0xF8,0xCF,0xC0,0x00,0x00,0x00,0x02,0x00,0x01,
               0xE2,0x21,0xBC,0x33,
               0x00,0x00,0x00,0x00,0x49,0x45,0x4E,0x44,0xAE,0x42,0x60,0x82
[IO.File]::WriteAllBytes("test_1x1.png", $png)

# Over-size file (>5 MB)
$sixMB = New-Object byte[] (6 * 1024 * 1024)
[IO.File]::WriteAllBytes("test_6mb.jpg", $sixMB)

# Empty file
[IO.File]::WriteAllBytes("test_empty.jpg", @())

# Wrong type
[IO.File]::WriteAllBytes("test_fake.exe", [Text.Encoding]::ASCII.GetBytes("MZ fake exe"))
[IO.File]::WriteAllBytes("test_fake.pdf", [Text.Encoding]::ASCII.GetBytes("%PDF-1.4 fake"))
```

### Upload via PowerShell (multipart/form-data)

```powershell
function Invoke-ImageUpload {
    param(
        [string]$ApiBase,
        [int]$ProductId,
        [string]$FilePath,
        [string]$BearerToken
    )
    $headers = @{ Authorization = "Bearer $BearerToken" }
    $form = @{ file = Get-Item $FilePath }
    return Invoke-RestMethod -Uri "$ApiBase/api/v1/products/$ProductId/image" `
        -Method POST -Headers $headers -Form $form
}

# Usage
$result = Invoke-ImageUpload -ApiBase "https://app-outdoors-api-dev.azurewebsites.net" `
    -ProductId 1 -FilePath "test_1x1.jpg" -BearerToken $env:ADMIN_TOKEN
Write-Host "imageUrl: $($result.imageUrl)"
```

### CORS preflight test (curl.exe)

```powershell
function Test-CORSPreflight {
    param([string]$ApiBase, [string]$Path, [string]$Origin)
    $headers = curl.exe -s -D - -o NUL `
        -X OPTIONS "$ApiBase$Path" `
        -H "Origin: $Origin" `
        -H "Access-Control-Request-Method: POST" `
        -H "Access-Control-Request-Headers: Authorization, Content-Type"
    
    $acao = $headers | Select-String "^access-control-allow-origin:" -CaseSensitive:$false
    $acam = $headers | Select-String "^access-control-allow-methods:" -CaseSensitive:$false
    $acac = $headers | Select-String "^access-control-allow-credentials:" -CaseSensitive:$false
    $status = $headers | Select-String "^HTTP/"

    Write-Host "Status: $status"
    Write-Host "ACAO: $acao"
    Write-Host "ACAM: $acam"
    Write-Host "ACAC: $acac"
    
    return ($acao -match [regex]::Escape($Origin))
}

# Usage
$pass = Test-CORSPreflight `
    -ApiBase "https://app-outdoors-api-dev.azurewebsites.net" `
    -Path "/api/v1/products/1/image" `
    -Origin "https://brave-beach-044d7c610.6.azurestaticapps.net"
Write-Host "CORS Test: $(if ($pass) { 'PASS' } else { 'FAIL' })"
```

---

## Key Gotchas

### BlobStorageService creates container with `PublicAccessType.None` — RESOLVED
Original `BlobStorageService.UploadAsync` called `CreateIfNotExistsAsync(PublicAccessType.None)`.
`UploadProductImageAsync` (added 2026-05-24) uses `PublicAccessType.Blob` — returned URLs are directly accessible without SAS tokens.

### CORS responds to OPTIONS even for 404 routes
ASP.NET Core CORS middleware runs before routing. An OPTIONS preflight to a non-existent route still returns 204 with CORS headers. This means you can verify CORS configuration before the endpoint is deployed.

### No default admin user — seeding pattern
Roles are seeded in `Program.cs` but no admin user is created. For integration tests requiring Administrator JWT:
1. Register a user via `POST /api/v1/auth/register`
2. Run SQL to assign Administrator role: `INSERT INTO AspNetUserRoles (UserId, RoleId) SELECT u.Id, r.Id FROM AspNetUsers u, AspNetRoles r WHERE u.Email = '{email}' AND r.Name = 'Administrator'`
3. Login to get admin JWT

### Blob name design for re-upload cleanup
When implementing re-upload, parse the existing `product.ImageUrl` to extract the blob name before overwriting:
```csharp
// Option A: predictable name (auto-overwrite, no explicit delete needed)
var blobName = $"products/{productId}{ext}";

// Option B: timestamp-based (requires explicit delete of old blob)
var blobName = $"products/{productId}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{ext}";
```
Option A is simpler and sufficient for this use case.

---

## Checklist: Blob Image Upload Endpoint

- [ ] Route requires `[Authorize(Roles = "Administrator")]`
- [ ] Accepts `IFormFile file` in multipart body
- [ ] Validates MIME type against allowlist (jpg, jpeg, png, gif, webp)
- [ ] Validates file size <= 5 MB
- [ ] Validates file is not empty (length > 0)
- [ ] Returns 404 if product not found
- [ ] Deletes old blob (or uses overwrite-by-name strategy)
- [ ] Uploads to container `product-images`
- [ ] Updates `product.ImageUrl` and saves to DB
- [ ] Returns `{ imageUrl: string }` on success
- [ ] Container has `PublicAccessType.Blob` (not None)
