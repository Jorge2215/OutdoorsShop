# Cinnamon — History (summarized)

- Full chronological history archived to history-archive.md.
- Recent highlights: async order receipts landed, stock-update queue publishing stayed observational, and async report export shipped with queue + blob + status endpoints.

## Learnings

### 2026-05-28T01:36:33.323-03:00 — Backend deploy reference comparison
- Push run `26551166403` on commit `a5868e2` is not a true backend deploy reference: that version of `.github\workflows\backend.yml` only restored, built, and tested, so its success never exercised publish or Azure deployment.
- Push run `26553116007` on commit `94aed83` proves the repo-side publish path is healthy: restore, build, test, linux-x64 publish, packaging, and artifact upload all succeeded before the deploy job failed at `azure/login@v2`.
- When backend deployment is blocked only by missing Azure OIDC secrets, add an explicit workflow preflight that names the missing secret keys and states that build/publish already passed; this keeps the failure accurate without exposing or weakening secrets.

### 2026-05-28T01:00:14.073-03:00 — Backend deploy blocker analysis
- `src\OutdoorsShop.Api\Controllers\ReportsController.cs` still contains the async export request actions, but live dev Swagger at `https://app-outdoors-api-dev.azurewebsites.net/swagger/v1/swagger.json` exposes only `/api/v1/Reports/orders` and `/api/v1/Reports/inventory`, so the App Service is still on a stale API package.
- GitHub Actions run `26553116007` for commit `94aed83` proved the repo-side workflow fix is good: restore, build, test, and linux-x64 publish all succeeded; the only failure was `azure/login@v2` in the `deploy` job because `client-id` and `tenant-id` were not supplied.
- `backend.yml` only publishes/deploys on `push`, so a green `workflow_dispatch` run validates the code path but does not update `app-outdoors-api-dev`; recovery now depends on fixing the Azure GitHub Actions credentials and then rerunning the failed push or sending a fresh deploy-triggering push.

### 2026-05-28T00:35:13.293-03:00 — Workflow repair push handling
- Before pushing `dev`, inspect `origin/dev..HEAD`; this branch was already ahead with two unrelated Scribe commits, so staging only `.github/workflows/backend.yml` still meant the push would carry those existing branch commits along with the new workflow repair commit.
- For isolated operational pushes, keep unrelated working-tree edits unstaged and commit only the target file, but report any unavoidable pre-existing branch commits that ride along because they are already part of the current branch history.

### 2026-05-28T00:30:27.267-03:00 — Backend workflow publish repair
- The rebuilt backend workflow's API publish step failed because it combined `-r linux-x64` with `--no-restore`, but the earlier solution restore only produced generic `net10.0` assets; `dotnet publish` then stopped with `NETSDK1047` because `src\OutdoorsShop.Api\obj\project.assets.json` lacked a `net10.0/linux-x64` target.
- Removing `--no-restore` from `.github\workflows\backend.yml` keeps CI/test behavior intact while letting the publish step restore the runtime-specific graph it needs before packaging `publish/api.zip` for App Service deployment.
- Validation for this repair used the repo's existing commands: `dotnet build OutdoorsShop.slnx --no-restore`, `dotnet test OutdoorsShop.slnx --no-build --verbosity normal`, and the corrected API publish command with `-r linux-x64`.

### 2026-05-28T00:25:21.638-03:00 — Source route verification
- The async export request API surface is present on `dev` in `src\OutdoorsShop.Api\Controllers\ReportsController.cs`: `CreateRequest` (`[HttpPost("requests")]`), `GetRequestById` (`[HttpGet("requests/{id:guid}")]`), and `Download` (`[HttpGet("requests/{id:guid}/download")]`).
- Supporting source also exists on this branch: `src\OutdoorsShop.Core\Interfaces\IReportExportRequestService.cs`, `src\OutdoorsShop.Infrastructure\Services\ReportExportRequestService.cs`, `src\OutdoorsShop.Api\Extensions\ServiceCollectionExtensions.cs`, and migration `src\OutdoorsShop.Infrastructure\Data\Migrations\20260528003127_AddReportExportRequests.cs`.
- Commit `0809095` (`Add async report export workflow`) contains those files on `dev`, so a live dev API that still lacks the routes is running stale code or missed this deploy, not missing source on the current branch.

### 2026-05-28T00:21:12.394-03:00 — Live dev API async export diagnosis
- Live dev App Service at `https://app-outdoors-api-dev.azurewebsites.net` is healthy on `/api/health`, and live Swagger still exposes only `/api/v1/Reports/orders` and `/api/v1/Reports/inventory` under `Reports`.
- The async export request routes from local source (`POST /api/v1/reports/requests`, `GET /api/v1/reports/requests/{id}`, `GET /api/v1/reports/requests/{id}/download`) are not present in live Swagger and return `404 Not Found`, while existing report routes return `401 Unauthorized` without a bearer token.
- Most likely backend cause: the dev API is running an older build that predates the async report-request actions, so this is a missing-route deployment/version issue rather than a method mismatch, auth failure on the request endpoints, or a migration/Function runtime problem.

### 2026-05-27T23:58:19.829-03:00 — Backend workflow deploy rebuild
- Rebuilt `.github/workflows/backend.yml` as the single backend CI/CD workflow: PRs still restore/build/test only, while pushes now publish `src/OutdoorsShop.Api/OutdoorsShop.Api.csproj`, deploy to the branch-selected App Service, and smoke test `/api/health`.
- Followed the existing Functions workflow pattern for OIDC Azure login and branch-based `dev`/`prod` environment selection, using `app-outdoors-api-dev` / `rg-outdoors-dev` from repo docs and the established `{abbreviation}-outdoors-{environment}` convention for prod names.

### 2026-05-28T00:09:41.836-03:00 — Workflow fix pushed
- Pushed `dev` with commit `d01e899` after staging only `.github/workflows/backend.yml`, leaving unrelated `.squad` history edits untouched in the working tree.
- Commit message used: `fix: rebuild API deploy workflow`, matching the rebuilt API publish/deploy pipeline now tracked in the backend workflow.

### 2026-05-27T22:24:02.039-03:00 — Deployment & migration attempt
- Deployed `app-outdoors-api-dev` (resource group `rg-outdoors-dev`) via ZIP publish and confirmed the app responded to an HTTPS probe at its default host.
- Attempted to apply EF migration `20260528003127_AddReportExportRequests` but `ConnectionStrings__DefaultConnection` is a Key Vault reference and the current Azure CLI identity does not have GET access to the referenced secret; migration was not executed from this session.
- Recommendation: grant Key Vault secret GET permission to the deployment principal or run the migration from a CI/CD/service principal that has Key Vault access.


### 2026-05-28T01:15:13Z — Current operating notes
- For the current POC, do **not** make `stock-updates` the authoritative stock writer yet; keep inventory writes synchronous and use queues/Functions for report exports or low-stock alerts first.
- Async report export dev rollout is a two-surface backend deploy: deploy both `app-outdoors-api-dev` and `func-outdoors-dev`, apply `20260528003127_AddReportExportRequests`, and keep both apps pointed at the same Azure SQL database and storage account.
- Backend API deployment is still manual. Publish/deploy the API to App Service, verify `ConnectionStrings__DefaultConnection`, `AzureStorage__ConnectionString`, `JwtSettings__Secret`, `AzureWebJobsStorage`, and `AllowedOrigins`, then smoke test `/api/health`.

## 2026-05-28T01:15:13Z — Scribe update
- Merged 10 inbox decisions into `decisions.md`, cleared the decision inbox, and summarized this history because it exceeded 15 KB.
- Manual deployment guidance from Cinnamon was recorded for the dev API rollout, including `dotnet ef database update` before traffic and zip/App Service deployment verification.
## 2026-05-28T01:15:13Z — Additional Cinnamon update
- The API deploy reached `app-outdoors-api-dev`, but the `20260528003127_AddReportExportRequests` EF migration could not be applied from the current session because the active Azure identity could not read the Key Vault-referenced `ConnectionStrings__DefaultConnection` secret.
- Follow-up: grant the deployment identity Key Vault secret read access or run the migration through a CI/CD/service principal path that already has that permission.

## 2026-05-28T01:24:02Z — Orchestration
- Orchestration log written: `.squad/orchestration-log/2026-05-28T01-24-02Z-cinnamon.md`.
- Session log recorded: `.squad/log/2026-05-28T01-24-02Z-scribe-session.md`.

## 2026-05-28T03:01:40Z — Scribe: inbox merge & orchestration
- Merged remaining decision inbox files into `.squad/decisions/decisions.md` (3 files: cinnamon-api-deploy-workflow.md, cinnamon-design-time-ef-config.md, toru-api-deploy-workflow.md).
- Orchestration log written: `.squad/orchestration-log/2026-05-28T03-01-40Z-cinnamon.md`.
- Session log recorded: `.squad/log/2026-05-28T03-01-40Z-api-deploy-workflow.md`.

### 2026-05-28T11:23:06.621-03:00 — Architecture doc review (async report export)
- `docs/architecture.md` was accurate on queue name (`report-export-requests`), blob container (`report-exports`), function app (`func-outdoors-dev`), SAS URL TTL (15 min), and .NET 10 references.
- **Corrected:** Download flow line said "browser redirects user to SAS URL" — the controller returns `200 OK` JSON; the frontend JS programmatically triggers the download. Updated to "frontend JavaScript triggers a browser download via the SAS URL (anchor click; no HTTP redirect from API)".
- **Added:** `ReceiptGenerationFunction` (queue: `receipt-requests`, container: `order-receipts`) was missing from the functions table, both diagrams, the storage table, and the communication flows table. Added to all four locations.
- **Fixed:** "Planned (Not Implemented)" falsely claimed order receipts were PDFs and not yet built. `ReceiptGenerationFunction` is already implemented and generates HTML. Updated accordingly.

### 2026-05-28T21:01:08.714-03:00 — Catalog MVP backend query composition
- `GET /api/v1/products` now accepts `minPrice`, `maxPrice`, and `sort` alongside the existing `categoryId` and `search` query params in `src\OutdoorsShop.Api\Controllers\ProductsController.cs`.
- Catalog filtering now flows through `IProductRepository.SearchProductsAsync` / `src\OutdoorsShop.Infrastructure\Repositories\ProductRepository.cs`, which composes search, category, and price predicates with AND logic and applies sorting after filtering.
- Allowed sort values are `name_asc`, `price_asc`, and `price_desc`; invalid values fall back to `name_asc`, and `minPrice > maxPrice` returns an empty array instead of a 400.
- Coverage for this contract lives in `tests\OutdoorsShop.Api.Tests\Controllers\ProductsControllerTests.cs`, `tests\OutdoorsShop.Api.Tests\Repositories\ProductRepositoryTests.cs`, and `tests\OutdoorsShop.Api.Tests\Integration\ProductsIntegrationTests.cs`.
## 2026-05-29T01:05:57.2395908Z — Scribe update
- Archived 0 decisions; merged 14 inbox files.

