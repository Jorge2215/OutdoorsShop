---
name: async-report-export-rollout
description: Rollout checklist for queue-backed report exports spanning ASP.NET Core API, Azure Functions, Azure SQL, and Blob Storage
domain: backend-deployment
confidence: high
source: manual
---

# Async report export rollout

**Date:** 2026-05-27T22:00:07.784-03:00
**Author:** Cinnamon

## When to use

Use this when an OutdoorsShop backend feature follows the pattern: API writes a request row, publishes a storage-queue message, an Azure Function generates the file, and the API later returns a SAS download URL.

## Rollout checklist

1. Deploy both backend surfaces, not just one:
   - ASP.NET Core API for request creation, status polling, and SAS download URLs
   - Azure Functions app for queue-triggered processing
2. Apply the EF Core migration before enabling traffic. If the request table is missing, the API cannot persist jobs and the Function cannot update status.
3. Point both apps at the same Azure SQL database.
4. Point both apps at the same Storage account:
   - API publishes the queue message
   - Function listens on the queue and uploads the file blob
   - API later signs the blob URL for download
5. Keep queue names aligned. If the Function trigger attribute hardcodes a queue name, do not override the publisher setting unless code is updated too.
6. Use a storage connection string with account key support anywhere the API generates SAS URLs.

## OutdoorsShop specifics

- Migration: `src\\OutdoorsShop.Infrastructure\\Data\\Migrations\\20260528003127_AddReportExportRequests.cs`
- API request/download surface: `src\\OutdoorsShop.Api\\Controllers\\ReportsController.cs`
- API queue publisher: `src\\OutdoorsShop.Infrastructure\\Services\\ReportExportQueuePublisher.cs`
- Function trigger: `src\\OutdoorsShop.Functions\\Functions\\ReportExportFunction.cs`
- Blob/SAS implementation: `src\\OutdoorsShop.Infrastructure\\Services\\BlobStorageService.cs`

## Gotchas

- `src\\OutdoorsShop.Api\\Program.cs` and `src\\OutdoorsShop.Functions\\Program.cs` do not run `Database.Migrate()`, so migrations are a deployment step, not a startup side effect.
- The current Function trigger listens to `report-export-requests` directly. The API publisher also supports `AzureStorage__ReportExportRequestsQueueName`, so non-default queue names are unsafe until the trigger is made configurable too.
- `BlobClient.GenerateSasUri(...)` requires credentials capable of signing SAS tokens; a bare blob endpoint is not enough.
- `.github\\workflows\\backend.yml` only packages and deploys the API on `push`; a green `workflow_dispatch` run proves the source builds but does not update `app-outdoors-api-dev`.
- If the backend push run reaches `azure/login@v2` and fails saying `client-id` / `tenant-id` are missing, the blocker is GitHub Actions Azure credential configuration, not the API publish command or report-export source.
