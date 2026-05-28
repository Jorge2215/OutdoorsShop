# Toru — History (summary)

- Project context: OutdoorsShop PoC; monorepo with React+Vite frontend and .NET 10 API; infra in Azure via Bicep.
- Key outcomes: Deployed frontend to Blob static website; API and Functions deployed; CORS conflict diagnosed and fixed; architecture and ADRs recorded; v1.0.0 released.

- Operational notes: Keep CORS in app config (not App Service platform); Blob static website acceptable for dev; infra details in infra/ and decisions inbox.

## Learnings
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
