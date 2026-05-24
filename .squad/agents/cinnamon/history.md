# history — History (summary)

- Full history archived to history-archive-20260524T145749Z.md

- Recent highlights:
- # Cinnamon — History
- 
- ## Core Context
- 
- - **Project:** Outdoors Shop
- - **Owner:** Jorgito
- - **Role:** Backend Developer
- - **Joined:** 2026-05-23
- - **Repo:** https://github.com/Jorge2215/OutdoorsShop.git (dev = development, main = production)
- - **Stack:** .NET 10 Web API (C#) | ASP.NET Core | EF Core | Azure SQL Database | Azure Functions (.NET isolated) | Azure Blob Storage | JWT auth
- - **Domain entities:** Products, Categories (Camping/Trekking/Cycling/Climbing), Customers, Orders, OrderItems, Inventory
- - **My scope:** .NET 10 Web API, EF Core + Azure SQL, Azure Functions (seasonal discounts/payment confirmation/stock updates), Azure Blob Storage (product images/receipts/exports), JWT auth backend, CSV/Excel report generation
- - **Team:** Toru (Architect), Malta (Frontend), Creta (Tester), Scribe (Docs), Ralph (Monitor)
- - **Purpose:** Proof of concept comparing GitHub Copilot + Squad vs traditional development
- 
- ## Learnings
- 
- ### 2026-05-24T16:24:29.079-03:00 — Swagger enabled in all environments
- 
- - Switched the API from development-only OpenAPI mapping to Swashbuckle middleware so `/swagger` and `/swagger/v1/swagger.json` stay available in every environment.
- - Build validation: `dotnet build` succeeded from `src/OutdoorsShop.Api` after adding `Swashbuckle.AspNetCore` and wiring `UseSwagger()` / `UseSwaggerUI()`.
- - Deployment validation: published linux-x64, zipped with `System.IO.Compression.ZipFile`, uploaded `webapp-releases/api-dev.zip`, refreshed `WEBSITE_RUN_FROM_PACKAGE`, restarted `app-outdoors-api-dev`, and verified both Swagger endpoints returned `200` in production.

## 2026-05-24 — Swagger in Production (cinnamon-6)
- Removed IsDevelopment() guard from Swagger/SwaggerUI in Program.cs
- Enabled Swagger in ALL environments (dev, staging, production)
- Deployed commits 9076954 + 943db2e to dev
- Verified: /swagger and /swagger/v1/swagger.json → 200 on app-outdoors-api-dev.azurewebsites.net
- IMPORTANT: Correct API hostname is app-outdoors-api-dev.azurewebsites.net (NOT outdoors-shop-api-dev)
- Backlog item #1 completed
- 
- ### 2026-05-24T15:30:00-03:00 — CORS fix: SWA URL + platform CORS trap

## 2026-05-24 — CORS Fix (cinnamon-5)
- Fixed CORS AllowedOrigins: added SWA URL brave-beach-044d7c610.6.azurestaticapps.net
- Removed stale blob storage origin stoutdoorswebdev.z1.web.core.windows.net
- Cleared rogue Azure platform CORS entry wonderful-plant-0a1ca5f0f.7.azurestaticapps.net that was overriding ASP.NET Core CORS entirely
- Hardened Program.cs to use GetChildren() for reliable env-var array reading
- Deployed commit cada3b2 to dev — smoke test confirmed ACAO header on health, preflight, products
- Creta independently verified: 8/8 cross-origin tests passed
- 
- - **Root cause was Azure platform CORS, not code:** `WEBSITE_CORS_ALLOWED_ORIGINS` was set to a stale SWA URL (`wonderful-plant-0a1ca5f0f.7.azurestaticapps.net`), which caused Azure App Service to intercept and reject all CORS preflight requests before ASP.NET Core middleware ran. The fix was `az webapp cors remove` to clear it. Platform CORS must remain empty — CORS is owned entirely by ASP.NET Core middleware.
- - **AllowedOrigins config reading:** `GetSection("AllowedOrigins").Get<string[]>()` can silently return an empty array when env vars and appsettings.json both define the same indexed keys — use `GetChildren().Select(c => c.Value ?? "").Where(v => v.Length > 0).ToArray()` instead.
- - **WEBSITE_RUN_FROM_PACKAGE caching:** When the blob URL is unchanged, Azure App Service may serve a cached local zip from `/home/data/SitePackages/`. Force a fresh pickup by either (a) uploading to a new blob name and updating the SAS URL, or (b) uploading directly to `/home/data/SitePackages/` via Kudu VFS and updating `packagename.txt`.
- - **SAS URL via az CLI in PowerShell:** `az storage blob generate-sas --full-uri` has ampersands in the URL that break as command separators. Build the URL manually: `$sas = az storage blob generate-sas ... -o tsv; $url = "https://...blob.../$name?$sas"`. Use a JSON file (`@file.json`) when passing the SAS URL to `az webapp config appsettings set`.
- - **Deployment pattern confirmed:** Publish linux-x64 zip → upload to `stoutdoorsdev/webapp-releases/` → update `WEBSITE_RUN_FROM_PACKAGE` SAS URL → app auto-restarts and picks up new code.
- - **Smoke test:** After deploy, confirmed `Access-Control-Allow-Origin: https://brave-beach-044d7c610.6.azurestaticapps.net` returned for GET `/api/health`, GET `/api/v1/products`, and OPTIONS preflight on `/api/v1/products`.
- 
- ### 2026-05-24T15:11:02.555-03:00 — Auth refresh cookie cross-origin fix
- 
- - **Refresh cookie policy:** `src/OutdoorsShop.Api/Controllers/AuthController.cs` must use `SameSite=None` with `Secure=true` for both the refresh-token set cookie and the logout clear-cookie path; `SameSite=Strict` breaks cross-origin refresh when the frontend origin differs from the API origin.
- - **JWT display name claim:** `GenerateTokenAsync` should populate `given_name` from `customer.Name` instead of `user.UserName`, because registration stores the email in `UserName`.
- - **Deployment/verification:** Published `src/OutdoorsShop.Api/OutdoorsShop.Api.csproj` for Linux, zipped via `ZipFile.CreateFromDirectory`, uploaded to `stoutdoorsdev/webapp-releases/api-dev.zip`, restarted `app-outdoors-api-dev`, then verified `POST /api/v1/auth/register` and `POST /api/v1/auth/refresh` both returned `200` and the live `Set-Cookie` header now shows `samesite=none`.
- 
- ### 2026-05-23 — EF Core migration + entity key convention fix
- 
- - **Migration file location:** `src/OutdoorsShop.Infrastructure/Data/Migrations/20260523162304_InitialCreate.cs`

