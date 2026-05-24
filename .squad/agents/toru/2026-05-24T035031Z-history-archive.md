# Toru — History

## Core Context

- **Project:** Outdoors Shop
- **Owner:** Jorgito
- **Role:** Architect
- **Joined:** 2026-05-23
- **Repo:** https://github.com/Jorge2215/OutdoorsShop.git (dev = development, main = production)
- **Stack:** React + TypeScript | .NET 10 Web API (C#) | Azure SQL Database | Azure Functions | Azure Blob Storage | JWT auth (Administrator, Customer)
- **Domain entities:** Products, Categories (Camping/Trekking/Cycling/Climbing), Customers, Orders, Inventory
- **My scope:** System architecture, Azure infra design, deployment strategy, ADRs, API contracts, cross-cutting decisions, reviewing Cinnamon/Malta/Creta work
- **Team:** Cinnamon (Backend), Malta (Frontend), Creta (Tester), Scribe (Docs), Ralph (Monitor)
- **Purpose:** Proof of concept comparing GitHub Copilot + Squad vs traditional development

## Learnings

### 2026-05-24T00:12:30.732-03:00 — Dual CORS conflict broke the dev SPA

**Task:** Diagnose why the Blob-hosted frontend at `https://stoutdoorswebdev.z1.web.core.windows.net` showed only the loading screen and then failed in production.

**What was found:**
- Azure App Service platform CORS still had three allowed origins configured while the API also ran ASP.NET Core `UseCors("ReactDevPolicy")` from `src/OutdoorsShop.Api/Program.cs`
- The API code already had the correct dynamic `AllowedOrigins__*` settings, so the platform CORS layer was redundant and risked conflicting browser CORS behavior
- After clearing App Service platform CORS, the API continued returning a single valid `Access-Control-Allow-Origin: https://stoutdoorswebdev.z1.web.core.windows.net` header and `Access-Control-Allow-Credentials: true` from application middleware alone

**Operational learning:**
- Do not enable Azure App Service platform CORS when ASP.NET Core CORS middleware is already active for the same app; keep one source of truth only
- For OutdoorsShop, CORS policy must stay in application code/configuration (`AllowedOrigins__*` + `UseCors`) so environments can be managed consistently without Azure injecting a second CORS layer

---

### 2026-05-23T22:49:04.177-03:00 — Swagger deployment to dev App Service completed

**Task:** Deploy the latest `main` commit (`197ea1e` — `feat: enable swagger in all environments`) to the dev API App Service.

**What was verified:**
- Deployment source branch/worktree: `.copilot-main` on `main`, clean, ahead of `origin/main` by 1 commit
- Recent commits confirmed with `git log --oneline -3`
- Live endpoints after deployment: `https://app-outdoors-api-dev.azurewebsites.net/openapi/v1.json` → `200`, `https://app-outdoors-api-dev.azurewebsites.net/swagger` → `200`

**Operational learning:**
- The dev API App Service is effectively blob-backed via `WEBSITE_RUN_FROM_PACKAGE` pointing at `https://stoutdoorsdev.blob.core.windows.net/webapp-releases/api-dev.zip?...`
- `az webapp deploy` created a successful OneDeploy record, but the live site only updated after replacing the backing blob package and restarting the app
- For this Linux App Service, a Linux-targeted publish (`dotnet publish -c Release -r linux-x64 --self-contained false /p:UseAppHost=false`) was required; the generic publish briefly left the site unhealthy (`503`)

---

### 2026-05-23 — Azure Bicep Infrastructure Templates Created

**Task:** Create complete Azure Bicep IaC in `infra/` for the OutdoorsShop dev environment.

**Files created:**
- `infra/main.bicep` — orchestrator; 6 modules wired with implicit dependency chain
- `infra/parameters/dev.bicepparam` — non-sensitive dev params (`using '../main.bicep'`)
- `infra/modules/monitoring.bicep` — App Insights + Log Analytics workspace
- `infra/modules/sql.bicep` — SQL Server + Basic-tier Database; `@secure()` output for conn string
- `infra/modules/storage.bicep` — Storage Account (LRS/StorageV2) + 3 containers; `@secure()` output
- `infra/modules/appservice.bicep` — App Service Plan (B1/Linux) + Web App (.NET 10); KV refs in app settings
- `infra/modules/functions.bicep` — Consumption/Linux hosting plan + Functions App (.NET isolated 10); KV refs
- `infra/modules/keyvault.bicep` — Key Vault (standard, soft-delete 7d) + 4 secrets + access policies
- `infra/README.md` — full deployment instructions including ShopAdmin `db_ddladmin` note

**Key architectural patterns applied:**
- System-assigned managed identity on App Service + Functions; access policy `get`/`list` on Key Vault
- App settings use `@Microsoft.KeyVault(VaultName=...;SecretName=...)` references — zero plaintext secrets in config
- Key Vault deployed last in `main.bicep` so it receives `principalId` outputs from App Service and Functions in one pass
- `@secure()` on Bicep outputs for connection strings — prevents leaking key material to deployment logs
- `listKeys()` used to compute storage connection string only at deployment time; result stored in Key Vault
- `db_ddladmin` role requirement for ShopAdmin documented in README (EF Core migrations need DDL rights)

**Resource names confirmed (dev):**
`appi-outdoors-dev`, `law-outdoors-dev`, `sql-outdoors-dev`, `sqldb-outdoors-dev`, `stoutdoorsdev`,
`asp-outdoors-dev`, `app-outdoors-api-dev`, `asp-outdoors-func-dev`, `func-outdoors-dev`, `kv-outdoors-dev`

**Decision filed:** `.squad/decisions/inbox/toru-bicep-infra.md`

---

### 2026-05-23 — Architecture Design Document Produced (Full Run)

**Task:** Produce the full Architecture Design Document covering all 10 required sections.

**ADR files written to `.squad/decisions/inbox/`:**
- `toru-adr-001-monorepo-structure.md` — Monorepo with src/, frontend/, infra/
- `toru-adr-002-dotnet-clean-architecture.md` — Clean Architecture .NET layering
- `toru-adr-003-jwt-aspnet-identity.md` — JWT bearer + ASP.NET Core Identity
- `toru-adr-004-client-side-cart.md` — Client-side cart (Zustand), no Cart table in DB
- `toru-adr-005-ef-core-repository-pattern.md` — EF Core 10 + repository pattern + Mapster
- `toru-adr-006-keyvault-managed-identity.md` — Key Vault + managed identity, no secrets in config

**Architecture document produced:** docs/architecture/architecture.md (reference copy in chat output)

**Key decisions confirmed/reinforced:**
- Frontend lives in `/frontend/` (not `/src/OutdoorsShop.Web/`) — Vite + React TS, not a .NET project
- Tests split: `OutdoorsShop.Api.Tests`, `OutdoorsShop.Functions.Tests` in `/tests/`, `frontend/tests/` for Vitest
- `/api/v1/cart` is checkout-only — no server-side cart persistence
- All secrets via Key Vault + managed identity; `DefaultAzureCredential` in code
- OIDC federated credentials for GitHub Actions (no stored SP secrets)
- App Service SKU: B1 (dev), P2v3 (prod); SQL: Basic/S0 (dev), S2 (prod)
- Refresh token: hashed SHA-256, stored in `AspNetUserTokens`; cookie is HttpOnly+Secure+SameSite=Strict

---

### 2026-05-23 — Full Solution Architecture Designed

**Architectural choices made:**
- Monorepo: `src/` (.NET), `frontend/` (React/Vite), `infra/` (Bicep IaC), `.github/workflows/`
- .NET layering: Api → Core (no deps) → Infrastructure (EF Core + Azure SDKs) + Functions (isolated) + Tests (xUnit)
- Cart is client-side only (Zustand + localStorage) — no Cart table in the DB
- Soft delete for Products and Categories via `IsActive` flag + EF global query filters
- Six app tables: Categories, Products, Customers, Orders, OrderItems, Inventory + ASP.NET Identity tables
- ASP.NET Core Identity for user/role management; JWT bearer for stateless API auth
- Access token: 15 min in-memory; Refresh token: 7 days in HttpOnly cookie, hashed in `AspNetUserTokens`
- Custom JWT claim `customer_id` avoids DB round-trip to resolve user → customer on every request
- Three Azure Functions: SeasonalDiscountTimer (timer/daily), PaymentConfirmationQueue (queue), StockUpdateQueue (queue)
- Three Blob containers: `product-images` (public blob), `order-receipts` (private SAS), `exports` (private SAS)
- Azure resource naming: `{abbrev}-outdoors-{env}` (e.g., `app-outdoors-api-dev`)
- Key Vault + managed identity — no connection strings in app settings or code
- App Service B1 dev / P2v3 prod; SQL Basic/S0 dev / S2 prod; Functions Consumption both

**Technology decisions:**
- EF Core 10 with repository pattern (interfaces in Core, implementations in Infrastructure)
- Swashbuckle OpenAPI; Swagger UI dev-only at `/swagger`
- API versioning from day one: `/api/v1/` prefix via `Asp.Versioning` middleware
- GitHub Actions with OIDC federated credentials (no stored service principal secrets)
- Three workflows scoped by path filter: backend, frontend, functions
- `main` branch: PR + approval + status checks required; `dev`: status checks only

**Key file paths (once scaffolded):**
- Solution file: `OutdoorsShop.sln`
- Web API entry: `src/OutdoorsShop.Api/Program.cs`
- DbContext: `src/OutdoorsShop.Infrastructure/Data/OutdoorsShopDbContext.cs`
- Domain entities: `src/OutdoorsShop.Core/Entities/`
- Repository interfaces: `src/OutdoorsShop.Core/Interfaces/`
- Azure Functions: `src/OutdoorsShop.Functions/`
- React app root: `frontend/src/main.tsx`

---

### 2026-05-23 — Data Model Review Completed

**Reviewed entities:** Product, ProductCategory, Customer, SalesOrder, SalesOrderDetail, ProductInventory

**Approved as-is:** `SalesOrderDetail` (complete — no additions needed). Core fields of all other entities approved.

**Required additions confirmed:**
- `Product`: `IsActive` (bool, default true) — soft-delete; EF global query filter `WHERE IsActive = 1`
- `ProductCategory`: `IsActive` (bool, default true) — same rationale
- `Customer`: `UserId` (string) — FK to `AspNetUsers.Id`; required for JWT claim `customer_id` and role-based auth
- `SalesOrder`: `Status` (string/enum, e.g., Pending/Processing/Shipped/Delivered/Cancelled) — order management; `PaymentStatus` (string/enum, e.g., Pending/Confirmed/Failed) — payment simulation + PaymentConfirmationQueue Function
- `ProductInventory`: `LastUpdated` (DateTime) — StockUpdateQueue Function audit trail; `ReorderThreshold` (int) — stock alert logic in StockUpdateQueue

**Cart decision confirmed:** Client-side only via Zustand + localStorage (ADR-004). No `Cart` or `CartItem` entities in DB.

**Naming alignment required:** EF `ToTable()` maps C# class names to DB table names: `SalesOrder`→`Orders`, `SalesOrderDetail`→`OrderItems`, `ProductCategory`→`Categories`, `ProductInventory`→`Inventory`.

---

### 2026-05-23: Solution Architecture Designed
- Monorepo: src/ (.NET), frontend/ (React+Vite), infra/ (Bicep)
- Solution projects: OutdoorsShop.Api, OutdoorsShop.Core, OutdoorsShop.Infrastructure, OutdoorsShop.Functions, OutdoorsShop.Tests
- Azure: App Service (B1 dev, P2v3 prod), Azure SQL (Basic dev, S2 prod), Blob Storage (3 containers: product-images public, order-receipts private, exports private), Functions Consumption plan, Key Vault
- DB schema: 6 tables — Categories, Products, Customers, Orders, OrderItems, Inventory
- API: versioned at /api/v1/, 7 resource groups, JWT bearer auth
- Auth: ASP.NET Core Identity + JWT (15min access token in-memory, 7-day refresh in HttpOnly cookie)
- Functions: 3 functions — SeasonalDiscountTimer (cron), PaymentConfirmationQueue, StockUpdateQueue
- Frontend state: Zustand (auth+cart) + React Query (server state)
- ADR-001 through ADR-006 recorded
- Auth store: `frontend/src/store/authStore.ts`
- Bicep main: `infra/main.bicep`
- CI/CD workflows: `.github/workflows/backend.yml`, `frontend.yml`, `functions.yml`
- Decision inbox: `.squad/decisions/inbox/toru-*.md`
- Architecture document: `docs/architecture/architecture.md`

### 2026-05-23 — Bicep infra and decision recorded

- Created full Azure Bicep IaC under infra/ (main orchestrator + 6 modules + dev parameters + README).
- Decision filed: .squad/decisions/inbox/toru-bicep-infra.md (merged into .squad/decisions.md).

---

### 2026-05-23 — OutdoorsShop PoC v1.0.0 Released to Production

OutdoorsShop PoC v1.0.0 released to main on 2026-05-23. Tag: v1.0.0.

**Merge commit:** `feat: OutdoorsShop PoC v1.0 — full stack release` (7f66530)
**Branch:** dev → main (no-fast-forward merge, 21 commits)
**Tag pushed:** v1.0.0 → origin

**Release scope confirmed:**
- 7 REST controllers (Auth, Products, Categories, Customers, Orders, Inventory, Reports)
- 3 Azure Functions (SeasonalDiscount timer, PaymentConfirmation queue, StockUpdate queue)
- React + TypeScript frontend (oriental theme)
- Azure Bicep IaC (6 modules) + GitHub Actions CI/CD (3 workflows)
- 78 tests passing, 0 skipped, 0 failed

**Decision filed:** `.squad/decisions/inbox/toru-v1-release.md`

---

### 2026-05-23T21:32:34.383-03:00 — Azure dev deployment executed

**SQL strategy chosen:** Reused existing Azure SQL server `azure-sql-pampa.database.windows.net` and database `OutdoorsShopDB` instead of provisioning `sql-outdoors-dev`.

**What was deployed:**
- Resource group: `rg-outdoors-dev`
- Monitoring + storage in `eastus`: `appi-outdoors-dev`, `law-outdoors-dev`, `stoutdoorsdev`
- App tier in `westus3` (quota workaround): `asp-outdoors-dev`, `app-outdoors-api-dev`, `asp-outdoors-func-dev`, `func-outdoors-dev`, `kv-outdoors-dev`
- API package deployed successfully; verified `https://app-outdoors-api-dev.azurewebsites.net/api/v1/products` returned `200 OK` with `[]`
- Functions package deployed via blob-backed `WEBSITE_RUN_FROM_PACKAGE`

**Resource URLs:**
- App Service URL: `https://app-outdoors-api-dev.azurewebsites.net`
- Functions URL: `https://func-outdoors-dev.azurewebsites.net`

**Issues encountered:**
- Full `main.bicep` deployment failed in `eastus` because Microsoft.Web server farm quota was `0`; worked around by deploying app-facing modules in `westus3`.
- App Service initially failed because infra app settings used `Jwt__*` / `Azure__Storage__*` keys while the code expects `JwtSettings__*` / `AzureStorage__*`; module fixed and redeployed.
- API initially failed with missing `Microsoft.Data.SqlClient` at runtime; publishing Linux-specific packages and adding an explicit `Microsoft.Data.SqlClient` package reference resolved it.
- Functions host URL still returned `503 Site Unavailable` at the end of this run even though the app resource, package URL, and settings were deployed.

**Decision filed:** `.squad/decisions/inbox/toru-azure-deploy-strategy.md`

---

### 2026-05-23T23:49:31.687-03:00 — Frontend SPA deployed to Azure dev

**Decision:** Host the Vite SPA on Azure Blob Storage static website in a new westus3 storage account (`stoutdoorswebdev`) instead of Azure Static Web Apps or App Service.

**Why this path won:**
- Static Web Apps free tier was evaluated first, but `Microsoft.Web/staticSites` is not available in `westus3`.
- Blob static website kept the deployment CLI-only, low-cost, and aligned with the user's `westus3` preference.
- App Service remained unnecessary for a static Vite bundle.

**What was deployed:**
- Frontend build source: `frontend/`
- Build-time API origin: `https://app-outdoors-api-dev.azurewebsites.net`
- New storage account: `stoutdoorswebdev` (`westus3`, `StorageV2`, `Standard_LRS`, minimum TLS `TLS1_2`)
- Static website enabled with `index.html` as both index and 404 document
- Vite `dist/` uploaded to `$web`

**Live URL:**
- `https://stoutdoorswebdev.z1.web.core.windows.net`

**Cross-origin work:**
- Added `https://stoutdoorswebdev.z1.web.core.windows.net` to `app-outdoors-api-dev` App Service CORS and application `AllowedOrigins__*` settings
- Restarted the API so the updated origin list was applied
- Verified preflight `OPTIONS https://app-outdoors-api-dev.azurewebsites.net/api/v1/products` returned `200 OK` with `Access-Control-Allow-Origin: https://stoutdoorswebdev.z1.web.core.windows.net`

**Operational learning:**
- Blob static website is the fastest no-pipeline path for a Vite SPA when SWA region availability blocks the preferred option.
- Azure Blob static website deep-link fallback returns the SPA shell via the 404 document but keeps HTTP status `404`; acceptable for dev, but SWA remains the cleaner long-term SPA host if westus-region support is acceptable.

**Decision filed:** `.squad/decisions/inbox/toru-frontend-deploy.md`
