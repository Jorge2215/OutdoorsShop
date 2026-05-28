# Cinnamon — History (summarized)

- Full chronological history archived to history-archive.md.
- Recent highlights: async order receipts landed, stock-update queue publishing stayed observational, and async report export shipped with queue + blob + status endpoints.

## Learnings

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
