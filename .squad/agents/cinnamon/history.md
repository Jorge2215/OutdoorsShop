# history — Cinnamon (summarized)

Recent highlights (summary):

- Implemented async order receipts: added ReceiptGenerationMessage contract, IReceiptQueuePublisher, PaymentConfirmationFunction as producer, and ReceiptGenerationFunction writing deterministic HTML receipts to `order-receipts` container.
- Exposed `GET /api/v1/orders/{id}/receipt` returning availability and short-lived SAS URL when present.
- CI validation: API and Functions tests added/updated for receipt endpoint and HTML encoding; build/tests reported green in recent runs.

Full chronological history archived to: history-archive-20260527T195134Z.md

2026-05-27T20:27:02Z - scribe: merged inbox entries into .squad/decisions.md (
  - cinnamon-azure-deploy-readiness.md
  - toru-azure-deploy-readiness.md
)

## 2026-05-27T20:47:27Z — scribe update
- Merged 1 inbox items into decisions.md
- Archived 0 entries (none older than cutoff)

- `ProductsController` public reads stay filtered by the global `Product.IsActive` query filter by default, but admin callers can now pass `includeInactive=true` on `GET /api/v1/products` and `GET /api/v1/products/{id}`.
- The bypass lives in `IProductRepository`/`ProductRepository` via `GetAllIncludingInactiveAsync` and `GetByIdIncludingInactiveAsync`, both using `IgnoreQueryFilters()` so soft-deleted products remain reviewable/reactivatable without exposing them to anonymous users.
- `ProductDto` already surfaced `IsActive`; the controller mapping was already correct, so admin reads now return `isActive=false` for soft-deleted products instead of a misleading 404.

### 2026-05-27T20:45:20.123-03:00 — Added workflow_dispatch to backend workflow

- Added `workflow_dispatch` trigger to `.github/workflows/backend.yml` so the backend workflow can now be manually triggered from the GitHub Actions UI.
- Kept existing `push` and `pull_request` triggers and all job logic unchanged.
- YAML validated; no logic changes to jobs or steps.

### 2026-05-24T19:41:04.973-03:00 — Selective dev commit + dev → main merge

- Selective source commit on `dev`: `56aaffc` — committed only real source/config files (`.copilot-main`, workflow files, infra files, `BasePrompt.md`, `frontend/public/staticwebapp.config.json`, `src/OutdoorsShop.Functions/Functions/HealthFunction.cs`) and left build artifacts/untracked packages out.
- Pushed `dev` first, then merged from the `.copilot-main` worktree into `main` as `914e9af` and pushed `origin/main` successfully.
- `git log --oneline -3` on `main` now shows the merge commit plus newer `dev` commits, so `68c2509` is not visible in the top 3 lines even though `git merge-base --is-ancestor 68c2509 main` confirms the CORS fix is included in `main`.
- Regression check: `dotnet test .\\OutdoorsShop.slnx --verbosity minimal` still fails with the pre-existing SQLite integration-test issue (`AspNetRoles` table missing); Functions tests pass.

### 2026-05-24T19:19:19.460-03:00 — Live bug fix: CORS origin mismatch

- **Both bugs (registration + catalog "Failed to fetch") had ONE root cause**: `AllowedOrigins__0` in the App Service env var pointed to `brave-beach-044d7c610.6.azurestaticapps.net`, but the actual live SWA is `wonderful-plant-0a1ca5f0f.7.azurestaticapps.net`. The `wonderful-plant` SWA is the one in `rg-outdoors-dev` with the React app deployed; `brave-beach` returns an Azure 404.
- **Diagnostic pattern**: CORS preflight returning 204 with NO `Access-Control-Allow-Origin` header = origin rejected by CORS policy. vs 204 WITH `Access-Control-Allow-Origin` = origin accepted.
- **How to identify the live SWA**: Use `az staticwebapp list --query "[].{name:name,url:defaultHostname}"` to see all SWAs and their URLs. Cross-check against CORS config.
- **How to identify what's deployed on a SWA**: Fetch the root HTML, extract the JS bundle URL, then grep the bundle for `app-outdoors` or `localhost` to confirm which API URL is baked in.
- **App Service env vars override appsettings.json**: Updating `AllowedOrigins__0` in the App Service config takes effect within ~30 seconds (no redeploy needed). Still update appsettings.json + commit so code matches reality.
- **Fix**: Updated `AllowedOrigins__0` App Service env var + `appsettings.json`, committed as `68c2509` to dev.
- **Verified**: Both `OPTIONS /api/v1/products` and `OPTIONS /api/v1/auth/register` from `wonderful-plant` origin now return 204 with proper ACAO headers.

### 2026-05-24T18:57:46.744-03:00 — dev → main merge

- `main` branch is checked out in the `.copilot-main` linked worktree; `git checkout main` from the repo root is blocked. All main-branch operations must run from `.copilot-main`.
- Merge conflict in `Program.cs` arose because main had the basic role-seeding block while dev had the extended version with `ILogger<Program>` and admin user seed. Resolution: always prefer the dev (fuller) version.
- Merge commit: `56f6dec` — "Merge dev into main: auth fixes, CORS, Swagger, blob image upload, admin seed". Push to `origin/main` succeeded.
- Commits synced: `22e971e` (cookie/JWT fix), `cada3b2` (CORS hardening), `9076954`+`943db2e` (Swagger all envs), `708af75` (admin seed), `526b8fa` (blob upload endpoint), `164a8e7` (frontend upload UI), plus squad/docs commits.
- 
- ### 2026-05-24T16:52:12.609-03:00 — Admin user seed
- 
- - Added idempotent admin user seed to `Program.cs` after role seeding — uses `FindByEmailAsync` guard to prevent duplicates.
- - Admin user: `admin@outdoorsshop.dev` / `Admin@123456`, role `Administrator`, Customer.Name `"Admin User"`.
- - `given_name` claim in JWT comes from `Customer.Name`; must create Customer record alongside the Identity user or the claim will fall back to UserName (which is email).
- - **Oryx double `.runtimeconfig.json` trap:** If a previous `publish_output` folder exists inside the project directory, dotnet publish will include it in the new output zip, causing Oryx to find 2 `.runtimeconfig.json` files and fall back to `hostingstart.dll`. Fix: set `WEBSITE_STARTUP_FILE = dotnet OutdoorsShop.Api.dll` via `az webapp config set --startup-file`.
- - Commit: `708af75`; deployed to `app-outdoors-api-dev`; login smoke test confirmed 200 + JWT with `given_name: "Admin User"` and role `Administrator`.
- 
- ### 2026-05-24T16:52:12.609-03:00 — Azure Blob Storage product image upload
- 
- - Most blob infrastructure was already in place (interface, service, NuGet, config placeholder, DI wiring); only the upload method, endpoint, and Azure container setup were missing.
- - Added `UploadProductImageAsync(Stream, string, string, int)` to `IBlobStorageService` — no `IFormFile` in Core to keep ASP.NET out of the domain layer; controller handles `IFormFile` extraction.
- - `BlobStorageService.UploadProductImageAsync` creates `product-images` container with `PublicAccessType.Blob`; blob name: `products/{productId}/{guid}{ext}`.
- - `[Consumes("multipart/form-data")]` causes 415 before auth runs — always include correct content type when testing auth.
- - Real connection string injected via `AzureStorage__ConnectionString` App Service env var; placeholder remains in appsettings.json.
- - Commit: `526b8fa`; deployed to `app-outdoors-api-dev`; 401/403/health smoke tests confirmed.
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

\n\n## 2026-05-25T14:05:01Z — Scribe\nMerged cinnamon-soft-delete-fix.md into decisions.md; backend add: includeInactive flag and IgnoreQueryFilters for admin reads; commit c034239.

### 2026-05-25T19:08:32.516-03:00 — Change password endpoint

- Added `ChangePasswordDto` and wired `PUT /api/v1/users/change-password` through `ICustomerService`/`CustomerService` so authenticated users can change their own password without bypassing the existing service layer.
- Password changes use ASP.NET Core Identity built-ins: `CheckPasswordAsync` for the current password check and `ChangePasswordAsync` for validation, hashing, and persistence.
- Swagger now includes XML comments from the API assembly, so the new password endpoint summary/remarks show up in generated docs.
- Validation and regression check passed with `dotnet build .\\src\\OutdoorsShop.Api\\OutdoorsShop.Api.csproj` and `dotnet test .\\OutdoorsShop.slnx`.

### 2026-05-27T20:45:20.123-03:00 — Backend CI manual trigger

- Updated `.github\\workflows\\backend.yml` to add `workflow_dispatch` without changing the existing `push`/`pull_request` branch or path filters, so backend CI can still auto-run on `src/**` changes and also be started manually from GitHub Actions.
- Structural validation passed by parsing the workflow with `npx --yes js-yaml .github\\workflows\\backend.yml`.

---
### 2026-05-25T22:39:03Z — Change-password feature
- Merged inbox decision(s) related to change-password into decisions.md.
- Noted implementation and UI work by Cinnamon/Malta/Creta in team records.
