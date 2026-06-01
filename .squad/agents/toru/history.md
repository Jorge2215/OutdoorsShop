# Toru — History (summary)

- Project context: OutdoorsShop PoC; monorepo with React+Vite frontend and .NET 10 API; infra in Azure via Bicep.
- Key outcomes: Deployed frontend to Blob static website; API and Functions deployed; CORS conflict diagnosed and fixed; architecture and ADRs recorded; v1.0.0 released.

- Operational notes: Keep CORS in app config (not App Service platform); Blob static website acceptable for dev; infra details in infra/ and decisions inbox.

## Learnings
- **2026-05-28T11:23:06.621-03:00 — Async report export architecture documented:**
  - Updated `docs/architecture.md` to reflect the live async report export feature.
  - Added `ReportExportFunction` (queue trigger: `report-export-requests`) to §2 diagram and §5 Functions table.
  - Documented end-to-end flow in §5: POST /requests → queue → ReportExportFunction → blob write → SAS download.
  - Added `report-exports` container and `report-export-requests` queue to §7 storage table.
  - Added `ReportExportRequests` table to §6 data architecture.
  - Corrected `.NET 8` → `.NET 10` isolated runtime throughout (§2, §5 tech stack, §10 resources, §10.1 diagram).
  - Removed stale Known Gap item "CSV/Excel report exports not wired up" from §12.
  - Updated §10.2 communication flows: added report export queue and SAS download flow rows; corrected API→Storage description.
  - Added `/admin/reports` → `AdminReportsPage` to the frontend routes table (§3).

- **2026-05-28T01:36:33.323-03:00 — Backend workflow runtime restore fix:**
  - Diagnosed push-triggered deploy failures as a restore/publish runtime mismatch (NETSDK1047).
  - Compared failed push and successful workflow_dispatch logs; only restore step differed.
  - Fixed by adding `--runtime linux-x64` to restore in backend.yml, matching publish step.
  - Documented in decisions inbox for team visibility.
- **2026-05-27T22:06:23 — Rollout order for async report export:**
  1. Commit/push to dev triggers CI/CD for frontend and functions; backend API deploy and DB migration are manual.
  2. Verify SWA hostname, update AllowedOrigins, confirm secrets, and smoke test all components before merging to main.

- **2026-05-27T22:00:07 — Dev rollout sequence for async report export:**
  - Functions and frontend deploy automatically to dev on push via CI/CD.
  - Backend API deploy is manual (no deploy step in backend.yml); must use az webapp deploy or similar.
  - Database migration for report export must be run manually or on API startup.
  - CORS AllowedOrigins must be updated if SWA hostname changes.
  - All required secrets must be present for CI/CD to succeed.

- **2026-05-27T20:59:19 — Recovery branch consolidation:** Merged `recovery/b69d5fd-20260527-182815` into `dev`. The recovery branch had 1 squad-docs commit + an uncommitted `workflow_dispatch` addition to `backend.yml`. Both were cleanly absorbed into `dev` with no conflicts. Both recovery and backup branches deleted (local + remote). PR dev→main must be created manually by the user via GitHub web UI because the active GitHub CLI session is an Enterprise Managed User (`JVILABOA_pampa`) which cannot create PRs on personal repos.

- **2026-05-28T01:00:14.073-03:00 — Dev API deploy failure & root cause:**
  - Observed: Recent dev push runs failed in the 'build-and-test' job during 'dotnet publish' with a runtime-assets error (NETSDK1047). The publish step targets linux-x64 while the earlier 'dotnet restore' did not restore runtime-specific assets, so publish failed and the deploy job could not proceed.
  - Impact: app-outdoors-api-dev was not updated; Swagger and the new report routes are still missing, causing 404s for /api/v1/reports/requests/*.
  - Short recovery options (concrete):
    1. Manual deploy now: build/publish locally and run `az webapp deployment source config-zip` against `app-outdoors-api-dev` (rg-outdoors-dev) using the current publish artifact.
    2. Quick CI fix: re-run backend workflow after ensuring restore includes the runtime (e.g. `dotnet restore --runtime linux-x64`) or remove the `--no-restore`/use runtime-aware restore before `dotnet publish` so the publish succeeds and deploy job runs.
    3. After deployment, verify Swagger shows the new report endpoints and run the EF migration if needed.

  - Notes: Repository workflow already conditions deploy on push to 'dev' and 'main'; no environment protection blocks exist for 'dev'. Ensure required Azure secrets are present for the Azure login step to succeed.

- **Pattern:** When a recovery/worktree branch diverges and only touches `.squad/` files and CI config, a direct `git merge` into dev is safe and typically conflict-free.

- **2026-05-24 — SWA migration:** `Microsoft.Web/staticSites` IS available in `westus3` (previous note that it was unavailable was incorrect or region support expanded). `app-outdoorsweb-swa` provisioned successfully in `westus3`. Default hostname: `wonderful-plant-0a1ca5f0f.7.azurestaticapps.net`.
- **Secret write scope:** `gh secret set` requires a PAT with `secrets:write` scope. If CLI returns HTTP 403, user must set secrets manually via GitHub UI.
- **SWA vs Blob static website:** SWA returns HTTP 200 on all routes (SPA routing); Blob static website returns HTTP 404 on deep links even when serving index.html. Always use SWA for React SPAs.
- **CORS follow-up:** After SWA is live, add the SWA hostname to `AllowedOrigins__*` on `app-outdoors-api-dev`. Remove old `stoutdoorswebdev` origin once verified.
- **stoutdoorswebdev vs stoutdoorsdev:** `stoutdoorswebdev` = old SPA static hosting (decommission after SWA verified). `stoutdoorsdev` = blob storage for product-images/order-receipts/reports (never delete).

- 2026-05-24T15:02:56Z — Migrated frontend to Azure Static Web App `app-outdoorsweb-swa`; added infra/modules/staticwebapp.bicep and updated infra/main.bicep; updated GitHub workflow for full CI/CD. Manual step: set `AZURE_STATIC_WEB_APPS_API_TOKEN` in repository secrets.

(Full history archived to 2026-05-24T035031Z-history-archive.md)

- **2026-05-24 — Architecture document:** Created `docs/architecture.md` (comprehensive reference covering all system layers). Committed to `dev` branch.
  - Confirmed: 4-project solution (`Api`, `Core`, `Infrastructure`, `Functions`); 7 controllers; 4 Azure Functions; Flex Consumption plan.
  - Confirmed: Frontend uses Zustand in-memory for access tokens (not localStorage); refresh token in HttpOnly cookie.
  - Confirmed: `staticwebapp.config.json` uses `navigationFallback` for SPA routing — correct SWA behaviour.
  - Confirmed: `SeasonalDiscountFunction` is a Timer trigger (02:00 UTC daily), not HTTP — task description listed it as HTTP incorrectly.
  - Confirmed: `stoutdoorsdev` stores `webapp-releases/api-dev.zip` in addition to product images and queues.
  - Known gap noted: `backend.yml` runs CI only; API deployment is a manual run-from-package blob step.
  - Known gap noted: CORS `AllowedOrigins` still includes old `stoutdoorswebdev` origin post-SWA migration.

- **2026-05-24 — Azure resource relationships added to architecture doc** (`docs/architecture.md` Section 10 enhanced):
  - Added ASCII dependency diagram showing GitHub Actions → SWA/App Service → SQL/Blob Storage → Functions chain.
  - Added Communication Flows table covering all runtime paths (Browser, API, Functions, CI/CD).
  - Added Resource Group Map: `rg-outdoors-dev` (App Service, Functions, SWA, Storage, KV, Insights) vs `AzureSqlRg` (SQL Server + DB).
  - ⚠️ **Critical note:** SQL is in `AzureSqlRg`, not `rg-outdoors-dev` — firewall rules must target `AzureSqlRg`. `deploySql=false` in Bicep prevents accidental re-provisioning.
  - `SeasonalDiscountFunction` correctly shown as Timer trigger (02:00 UTC), not HTTP.

- 2026-05-24 � Cinnamon: Fixed API role seeding and added /api/health endpoint; redeployed API (commits: dev:786cc88, main:a6a1780).

## 2026-05-27T18:30:18Z — Azure feature ideas (inbox)

- Authored `.squad/decisions/inbox/toru-azure-feature-ideas.md` describing Azure Functions + Queue + Storage feature options for review by the team.
- Topic: Azure Functions + Queue + Storage feature recommendation.

2026-05-27T20:27:02Z - scribe: merged inbox entries into .squad/decisions.md (
  - cinnamon-azure-deploy-readiness.md
  - toru-azure-deploy-readiness.md
)

## 2026-05-28T01:15:13Z — Scribe team update
- Merged Toru inbox decisions into decisions.md: queue-first stock writing is not recommended for the current POC; use async report exports or low-stock alerts first.
- Logged rollout guidance that pushes to dev auto-deploy frontend and Functions, but backend API deployment and the report-export EF migration remain manual steps.

- **2026-05-27T22:24:02.039-03:00 — Validation & permissions check:** Confirmed dev rollout path for async report export: Frontend (SWA) and Functions auto-deploy on push; API deploy and EF migration remain manual. Verified current Azure CLI identity (JVILABOA@pampa.com) has Owner role on subscription `bb5ffe61-553c-4019-a657-79878bed7e08`, which is sufficient to perform API deployment and run the EF migration against the dev Azure SQL. Documented required sequencing and app-setting requirements in `.squad/decisions/inbox/toru-report-export-rollout-order.md`.

- Recorded recovery-branch cleanup and Azure SQL missing-table root-cause notes in the shared decision record.

## 2026-05-28T01:24:02Z — Orchestration
- Orchestration log written: `.squad/orchestration-log/2026-05-28T01-24-02Z-toru.md`.
- Session log recorded: `.squad/log/2026-05-28T01-24-02Z-scribe-session.md`.

## 2026-05-28T03:01:40Z — Scribe: inbox merge & orchestration
- Merged remaining decision inbox files into `.squad/decisions/decisions.md` (3 files: cinnamon-api-deploy-workflow.md, cinnamon-design-time-ef-config.md, toru-api-deploy-workflow.md).
- Orchestration log written: `.squad/orchestration-log/2026-05-28T03-01-40Z-toru.md`.
- Session log recorded: `.squad/log/2026-05-28T03-01-40Z-api-deploy-workflow.md`.

- **2026-05-27T23:58:19.829-03:00 — API deploy workflow decision & guidance:**
  - Finding: repo currently lacks an automatic App Service deploy step for the Web API; backend.yml only builds and tests.
  - Decision: Extend `.github/workflows/backend.yml` (add a publish+deploy job) rather than creating a separate workflow so CI/test and deploy live together as in other repo workflows.
  - Naming / targets: `app-outdoors-api-dev` / `rg-outdoors-dev` for dev; `app-outdoors-api-prod` / `rg-outdoors-prod` for main/prod.
  - Approach: Mirror existing Functions workflow — dotnet publish -> zip -> azure/login@v2 -> az webapp deployment source config-zip --name "$AZURE_WEBAPP_NAME" --resource-group "$AZURE_RESOURCE_GROUP" --src publish/api.zip --timeout 600.
  - Secrets required in repository: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`. Also ensure `SQL_ADMIN_PASSWORD` / Key Vault references are present as needed.
  - Owner: Cinnamon to implement the change in `backend.yml` and validate deployment to dev. Keep deploy step gated by `if: github.event_name == 'push'` and environment mapping identical to `functions.yml`.

- **2026-05-28T00:25:21.638-03:00 — Async export routes missing live due to skipped API deploy, not missing source:**
  - `origin/dev` already contains the async report-request controller actions and EF migration from commit `0809095`.
  - The latest dev push run for `backend.yml` (`d01e899`) failed at `dotnet publish` with `NETSDK1047` because the workflow restored without the `linux-x64` runtime target, so the deploy job was skipped.
  - Result: `app-outdoors-api-dev` kept serving older App Service content, which explains Swagger showing only legacy report routes and `404` on `/api/v1/reports/requests*`.
  - Shortest safe recovery: publish/deploy the current API build with a runtime-aware restore (or remove `--no-restore` for publish), then verify Swagger includes the request routes before moving on to DB/function checks.

- **2026-05-28T21:01:08.714-03:00 — Catalog MVP design review approved:**
  - Confirmed 3-item MVP scope: price-range filtering (`minPrice`/`maxPrice`), sort (`sort` param), and frontend controls (inputs + dropdown).
  - Defined API contract: all filters compose with AND; sort enum is `name_asc|price_asc|price_desc`; response shape unchanged.
  - Key architectural decision: unify repository into one composable query method (`SearchProductsAsync`) rather than branching on search/category/price separately.
  - No DB migration, no pagination envelope, no slider — scope kept tight.
  - Split work: Cinnamon (backend repo + controller), Malta (frontend UI + API client), Creta (tests).
  - Decision written to `.squad/decisions/inbox/toru-catalog-mvp-design-review.md`.

## 2026-05-29T01:22:55.575Z (UTC) — Scribe update
- Orchestration log: .squad/orchestration-log/2026-05-29T01:22:55.575Z-toru.md
- Session log: .squad/log/2026-05-29T01:22:55.575Z-scribe-session.md
- decisions.md size: 54039 bytes; inbox processed: 0; archival: none moved.


- 2026-05-31T21:53:53.116-03:00 — Main avatar rollout: promoted the backend avatar API, additive EF migration, and backend workflow updates to main. Main workflow now targets pp-outdoors-api-prod / g-outdoors-prod and can build the EF bundle without a real database connection, but deploy run 26730246749 stopped at configuration validation because AZURE_SQL_CONNECTION_STRING is not configured for the prod environment/repository.
