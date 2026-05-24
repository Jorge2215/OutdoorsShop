# Toru — History (summary)

- Project context: OutdoorsShop PoC; monorepo with React+Vite frontend and .NET 10 API; infra in Azure via Bicep.
- Key outcomes: Deployed frontend to Blob static website; API and Functions deployed; CORS conflict diagnosed and fixed; architecture and ADRs recorded; v1.0.0 released.

- Operational notes: Keep CORS in app config (not App Service platform); Blob static website acceptable for dev; infra details in infra/ and decisions inbox.

## Learnings

- **2026-05-24 — SWA migration:** `Microsoft.Web/staticSites` IS available in `westus3` (previous note that it was unavailable was incorrect or region support expanded). `app-outdoorsweb-swa` provisioned successfully in `westus3`. Default hostname: `wonderful-plant-0a1ca5f0f.7.azurestaticapps.net`.
- **Secret write scope:** `gh secret set` requires a PAT with `secrets:write` scope. If CLI returns HTTP 403, user must set secrets manually via GitHub UI.
- **SWA vs Blob static website:** SWA returns HTTP 200 on all routes (SPA routing); Blob static website returns HTTP 404 on deep links even when serving index.html. Always use SWA for React SPAs.
- **CORS follow-up:** After SWA is live, add the SWA hostname to `AllowedOrigins__*` on `app-outdoors-api-dev`. Remove old `stoutdoorswebdev` origin once verified.
- **stoutdoorswebdev vs stoutdoorsdev:** `stoutdoorswebdev` = old SPA static hosting (decommission after SWA verified). `stoutdoorsdev` = blob storage for product-images/order-receipts/reports (never delete).

- 2026-05-24T15:02:56Z — Migrated frontend to Azure Static Web App `app-outdoorsweb-swa`; added infra/modules/staticwebapp.bicep and updated infra/main.bicep; updated GitHub workflow for full CI/CD. Manual step: set `AZURE_STATIC_WEB_APPS_API_TOKEN` in repository secrets.

(Full history archived to 2026-05-24T035031Z-history-archive.md)
