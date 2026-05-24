# Decisions

Archived: 2026-05-24T035031Z



---

## Merged from inbox: copilot-directive-20260523223344.md


### 2026-05-23T22:33:44-03:00: User directive
**By:** Jorgito (via Copilot)
**What:** Prefer `westus3` over `eastus` for Azure deployments. Going forward, default Azure region = `westus3`.
**Why:** User request â€” Toru's westus3 pivot confirmed as the preferred region; eastus has quota issues and westus3 is preferred.


---

## Merged from inbox: copilot-directive-frontend-swa.md


### 2026-05-23T23:51:06-03:00: User directive
**By:** Jorgito (via Copilot)
**What:** Deploy the React frontend as an Azure Static Web App in West US 3 (westus3)
**Why:** User request — captured for team memory


---

## Merged from inbox: toru-azure-deploy-strategy.md


# Toru â€” Azure deploy strategy

- **Date:** 2026-05-23T21:32:34.383-03:00
- **Decision:** Reuse the existing Azure SQL server `azure-sql-pampa.database.windows.net` and database `OutdoorsShopDB` for the dev deployment.
- **Why:** The database already exists in the subscription, EF Core migrations were already applied, and the live data path is known-good. Reusing it avoided provisioning a second empty SQL server (`sql-outdoors-dev`) and avoided rerunning migrations against a fresh database.
- **Implementation:** Updated `infra/main.bicep` to support `deploySql = false` plus an injected `existingSqlConnectionString`, and set `infra/parameters/dev.bicepparam` to default to the existing server FQDN.
- **Operational note:** The original full-stack deployment in `eastus` failed because the subscription had `0` Microsoft.Web server farm quota there, so the web-facing modules were deployed in `westus3` as a workaround while still pointing to the existing SQL server.
- **Result:** API infrastructure deployed successfully and `https://app-outdoors-api-dev.azurewebsites.net/api/v1/products` returned `200 OK`. The Functions app URL was provisioned but remained unhealthy (`503`) and needs follow-up investigation.


---

## Merged from inbox: toru-cors-fix.md


# Toru Decision â€” 2026-05-24T00:12:30.732-03:00 â€” Resolve dev API/frontend CORS conflict

## Context
The Blob-hosted frontend at `https://stoutdoorswebdev.z1.web.core.windows.net` was loading the shell and then failing during API access. The dev API at `https://app-outdoors-api-dev.azurewebsites.net` already had ASP.NET Core CORS middleware enabled via `UseCors("ReactDevPolicy")`, with origins sourced from `AllowedOrigins__*` application settings.

At the same time, Azure App Service platform CORS was also configured with:
- `https://stoutdoorswebdev.z1.web.core.windows.net`
- `http://localhost:5173`
- `http://localhost:3000`

This created two CORS enforcement layers for the same API.

## Decision
Remove all Azure App Service platform CORS allowed origins for `app-outdoors-api-dev` and keep CORS only in ASP.NET Core middleware.

## Changes applied
- Ran:
  - `az webapp cors remove --name app-outdoors-api-dev --resource-group rg-outdoors-dev --allowed-origins "https://stoutdoorswebdev.z1.web.core.windows.net" "http://localhost:5173" "http://localhost:3000"`
- Verified platform CORS is now empty with:
  - `az webapp cors show --name app-outdoors-api-dev --resource-group rg-outdoors-dev -o json`
  - Result: `"allowedOrigins": []`
- Verified application CORS configuration remains active through App Service settings:
  - `AllowedOrigins__0 = https://stoutdoorswebdev.z1.web.core.windows.net`
  - `AllowedOrigins__1 = http://localhost:5173`
  - `AllowedOrigins__2 = http://localhost:3000`

## Verification
- `GET /api/v1/products` with frontend `Origin` returns:
  - `200 OK`
  - `Access-Control-Allow-Origin: https://stoutdoorswebdev.z1.web.core.windows.net`
  - `Access-Control-Allow-Credentials: true`
  - body: `[]`
- `OPTIONS /api/v1/auth/refresh` with frontend `Origin` returns:
  - `200 OK`
  - single valid ACAO header for the frontend origin
- `POST /api/v1/auth/refresh` without cookie returns:
  - `401` (expected)
  - still includes correct ACAO/ACAC headers
- App log tail showed normal EF Core queries for `Products`; no API crash was observed during validation
- `$web` storage container contains current SPA assets dated `2026-05-24T03:00:33Z` to `03:00:35Z`

## Consequences
- CORS behavior now has one source of truth: the API application
- Future origin changes should be made in app settings / configuration, not Azure platform CORS
- The database is currently empty (`/api/v1/products` returns `[]`), but that is separate from the CORS failure and was not changed here


---

## Merged from inbox: toru-frontend-deploy.md


# Toru Decision â€” Frontend dev deployment

**Date:** 2026-05-23T23:49:31.687-03:00  
**By:** Toru (Architect)  
**Status:** Accepted

## Decision

Deploy the React + TypeScript (Vite) frontend SPA to an Azure Blob Storage static website hosted in a new westus3 storage account: `stoutdoorswebdev`.

**Live URL:** `https://stoutdoorswebdev.z1.web.core.windows.net`

## Why this option

- **Azure Static Web Apps** was evaluated first, but `Microsoft.Web/staticSites` was not available in `westus3`; the available regions were `centralus`, `eastus2`, `westus2`, `westeurope`, and `eastasia`.
- **Azure Blob Storage static website** works with plain `az` CLI, keeps the frontend as a static deploy, and let us stay in `rg-outdoors-dev` and `westus3`.
- **Azure App Service** was rejected as unnecessary runtime overhead for a compiled SPA.

## Implementation notes

- Built `frontend/` with `VITE_API_URL=https://app-outdoors-api-dev.azurewebsites.net`.
- Enabled static website hosting with `index.html` as both the index and 404 document.
- Uploaded the Vite `dist/` output to the `$web` container.
- Updated API CORS on `app-outdoors-api-dev` for `https://stoutdoorswebdev.z1.web.core.windows.net` and restarted the app so the new `AllowedOrigins` values were applied.
- Set the new storage account minimum TLS version to `TLS1_2`.

## Verification

- Frontend root returned `200 OK`.
- Deployed JS bundle references `https://app-outdoors-api-dev.azurewebsites.net`.
- API CORS preflight from the frontend origin returned `200 OK` with `Access-Control-Allow-Origin: https://stoutdoorswebdev.z1.web.core.windows.net` and `Access-Control-Allow-Credentials: true`.

## Follow-up

- Blob static website fallback serves `index.html` for unknown paths but returns HTTP 404 status on deep links; acceptable for dev, but Static Web Apps remains the better production-grade SPA host once region constraints are revisited.


---

## Merged from inbox: toru-swagger-deploy.md


# Toru â€” Swagger deploy outcome

- **Date:** 2026-05-23T22:49:04.177-03:00
- **Decision:** Keep `app-outdoors-api-dev` on the existing blob-backed package flow for now, but update the backing blob with a Linux-targeted publish when deploying API changes.
- **Why:** The App Service currently has `WEBSITE_RUN_FROM_PACKAGE` set to a SAS URL for `stoutdoorsdev/webapp-releases/api-dev.zip`. `az webapp deploy` created a successful OneDeploy record, but it did not update the running site until the backing blob package was replaced and the app was restarted.
- **Implementation:** Published `src/OutdoorsShop.Api/OutdoorsShop.Api.csproj` for Linux (`-c Release -r linux-x64 --self-contained false /p:UseAppHost=false`), uploaded the zip to `webapp-releases/api-dev.zip`, then restarted `app-outdoors-api-dev` in `rg-outdoors-dev`.
- **Result:** `https://app-outdoors-api-dev.azurewebsites.net/openapi/v1.json` returned `200 OK` and `https://app-outdoors-api-dev.azurewebsites.net/swagger` returned `200 OK` after restart.
- **Follow-up:** Align the documented backend deploy path with the actual run-from-package blob strategy, or remove the blob URL app setting so `az webapp deploy` can be the single source of truth.


---

## Merged from inbox: toru-v1-release.md


# Release Milestone: OutdoorsShop PoC v1.0.0

**Date:** 2026-05-23T21:12:05.666-03:00
**Author:** Toru (Architect)
**Type:** Release milestone

---

## Summary

OutdoorsShop PoC v1.0.0 has been released to `main`. This marks the completion of the first full-stack proof-of-concept benchmarking GitHub Copilot + Squad against traditional development.

## Release Details

| Field | Value |
|---|---|
| Tag | `v1.0.0` |
| Merge commit | `7f66530` |
| Strategy | `--no-ff` merge from `dev` â†’ `main` |
| Commits merged | 21 |
| Date | 2026-05-23T21:12:05.666-03:00 |

## What Shipped

### Backend â€” .NET 10 Web API
- **7 controllers:** Auth, Products, Categories, Customers, Orders, Inventory, Reports
- JWT bearer auth (ASP.NET Core Identity), 15-min access token, 7-day refresh in HttpOnly cookie
- EF Core 10 + repository pattern, Azure SQL, CSV/Excel exports
- API versioned at `/api/v1/`

### Azure Functions
- `SeasonalDiscountFunction` â€” timer-triggered daily discount recalculation
- `PaymentConfirmationFunction` â€” queue-triggered payment confirmation processor
- `StockUpdateFunction` â€” queue-triggered inventory adjustment with reorder alerts

### Frontend â€” React + TypeScript
- Oriental theme: crimson/gold/jade palette, Cinzel + Lato fonts
- Full customer flows (browse, cart, checkout) + admin dashboard
- Zustand stores (auth + cart), React Query for server state, typed API client with 401 auto-refresh

### Infrastructure
- Azure Bicep IaC: `infra/main.bicep` + 6 modules (monitoring, SQL, storage, appservice, functions, keyvault)
- GitHub Actions CI/CD: 3 path-filtered workflows (`backend.yml`, `frontend.yml`, `functions.yml`)
- OIDC federated credentials for GitHub Actions (no stored service principal secrets)

### Tests
- **78 passing, 0 skipped, 0 failed**
- xUnit unit tests (controllers, functions), SQLite in-memory integration tests

## Architecture Decisions Captured

- ADR-001: Monorepo structure (`src/`, `frontend/`, `infra/`)
- ADR-002: .NET Clean Architecture layering
- ADR-003: JWT + ASP.NET Core Identity
- ADR-004: Client-side cart (Zustand + localStorage, no Cart table in DB)
- ADR-005: EF Core 10 + repository pattern + Mapster
- ADR-006: Key Vault + managed identity (zero plaintext secrets)

## Branch Strategy Going Forward

- `main` â€” production; requires PR + approval + status checks
- `dev` â€” integration; status checks only
- Feature branches off `dev`, merged via PR

## Benchmark Notes

This PoC was built entirely using GitHub Copilot + Squad (Cinnamon/Backend, Malta/Frontend, Creta/Testing, Toru/Architecture, Scribe/Docs, Ralph/Monitoring). The release demonstrates the full end-to-end capability of the AI-assisted development workflow.

