# Skill: E2E Journey Testing via HTTP (OutdoorsShop)

## When to use
Use this pattern to run a full user journey test against the live OutdoorsShop API using HTTP calls only (no browser). Useful for smoke testing after deployments, verifying auth flows, and checking product/order endpoints.

## Pre-requisites
- Valid `curl.exe` (bundled with Windows) or PowerShell `Invoke-WebRequest`
- Access to production/staging API URL
- ⚠️ On Windows: **always use `Invoke-WebRequest`** or `Invoke-RestMethod` for JSON POST bodies. `curl.exe --data` / `--data-binary` with single-quoted JSON breaks on Windows PowerShell (misinterprets `{` as invalid JSON start).

## Journey Checklist

### 1. Verify catalog (unauthenticated)
```powershell
# All products - confirm count and imageUrl completeness
$products = Invoke-RestMethod -Uri "$apiBase/api/v1/products"
$products.Count  # expect 16
($products | Where-Object { [string]::IsNullOrEmpty($_.imageUrl) }).Count  # expect 0

# Category filter
Invoke-RestMethod -Uri "$apiBase/api/v1/products?categoryId=1"  # expect 4 (Camping)

# Category list
Invoke-RestMethod -Uri "$apiBase/api/v1/categories"  # expect 4 categories
```

### 2. Auth flow
```powershell
# Register — use correct RegisterDto shape
$reg = Invoke-RestMethod -Uri "$apiBase/api/v1/auth/register" -Method POST `
    -ContentType "application/json" `
    -Body (@{name="E2E Tester"; email="testuser_e2e@outdoorsshop.test";
             password="Test@1234!"; confirmPassword="Test@1234!"} | ConvertTo-Json)

# Login — captures JWT
$login = Invoke-RestMethod -Uri "$apiBase/api/v1/auth/login" -Method POST `
    -ContentType "application/json" `
    -Body (@{email="testuser_e2e@outdoorsshop.test"; password="Test@1234!"} | ConvertTo-Json)
$token = $login.accessToken

# Auth header for all subsequent calls
$headers = @{ Authorization = "Bearer $token" }
```

### 3. Place order (requires auth)
```powershell
# Order schema: shippingAddress, paymentMethod, items[]
$order = Invoke-RestMethod -Uri "$apiBase/api/v1/Orders" -Method POST `
    -Headers $headers -ContentType "application/json" `
    -Body (@{
        shippingAddress = "123 Test Street, Test City"
        paymentMethod   = "CreditCard"
        items = @(@{ productID = 1; quantity = 1; unitPrice = 149.99 })
    } | ConvertTo-Json -Depth 3)
$orderId = $order.orderID

# View order history
$orders = Invoke-RestMethod -Uri "$apiBase/api/v1/Orders" -Headers $headers
```

### 4. Verify images
```powershell
$products | Select-Object -First 4 | ForEach-Object {
    $status = curl.exe -s -o NUL -w "%{http_code}" -I $_.imageUrl
    Write-Host "$($_.name): $status"
}
```

## Known API contract details (verified 2026-05-24)

| Endpoint | Auth | Notes |
|----------|------|-------|
| `GET /api/v1/products` | None | Returns 16 products with imageUrl |
| `GET /api/v1/products?categoryId={n}` | None | Filter by category |
| `GET /api/v1/products/{id}` | None | Single product with imageUrl |
| `GET /api/v1/categories` | None | 4 categories |
| `POST /api/v1/auth/register` | None | Body: `{name, email, password, confirmPassword}` |
| `POST /api/v1/auth/login` | None | Body: `{email, password}` → returns `{accessToken, ...}` |
| `GET /api/v1/Orders` | Bearer | Paginated; customer sees own orders only |
| `POST /api/v1/Orders` | Bearer | Body: `{shippingAddress, paymentMethod, items[{productID, quantity, unitPrice}]}` |
| `GET /api/v1/cart` | — | **404 — No server-side cart (ADR-004: client-side only)** |
| `GET /api/health` | — | **200 `{"status":"ok"}` — health endpoint live (as of 2026-05-24 Cinnamon deploy)** |

## Auth token field name
`login.accessToken` (not `token` or `jwt`). Refresh token is in an `HttpOnly` cookie `refreshToken`.  
**Note:** `POST /api/v1/auth/register` also returns `accessToken` directly — no second login needed after signup.

## Orders list response shape
`GET /api/v1/Orders` returns a **paginated object**, not a plain array:
```json
{ "items": [...], "pageNumber": 1, "pageSize": 20, "totalCount": 1, "totalPages": 1 }
```
Access orders via `.items[]`.

## Known production issues (as of 2026-05-24 — RESOLVED)

### ✅ ~~Missing roles in production DB~~ — FIXED (2026-05-24)
~~`POST /api/v1/auth/register` → 500 `"Role CUSTOMER does not exist."`~~

Cinnamon added startup role seeding to `Program.cs`. Both `Administrator` and `Customer` roles are now seeded on app startup. **Full auth flow is operational.**

## Steps covered by Playwright (not HTTP)
Cart flow (add item → view cart → checkout) is **client-side only** (Zustand + localStorage per ADR-004). These steps require Playwright browser automation, not raw HTTP tests.

## Windows curl pitfall
On Windows PowerShell, `curl.exe --data` and `--data-binary` with single-quoted JSON strings will fail with a JSON parse error (`'e' is an invalid start of a property name`). Always use `Invoke-RestMethod` / `Invoke-WebRequest` for POST bodies with JSON on Windows.

## Scoring reference (12-step journey)
| Step | What | Pass condition |
|------|------|---------------|
| 1 | API health | 200 at `/health` |
| 2 | Product list | 200, 16 items, 0 null imageUrls |
| 3 | Category browse | 200 with filter, 200 category list |
| 4 | Register | 200/201, no 500 |
| 5 | Login | 200, `accessToken` in response |
| 6 | Product detail | 200, imageUrl present |
| 7-8 | Cart | N/A (client-side) |
| 9 | Place order | 201, orderID in response |
| 10 | Order history | 200, includes placed order |
| 11 | Images | 200 HEAD on Unsplash URLs |
| 12 | Functions health | 200 `{"status":"ok"}` |
