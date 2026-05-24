# Cinnamon Decision — 2026-05-24T14:43:10-03:00 — Identity Role Seeding on API Startup

## Context

`POST /api/v1/auth/register` returned **500** with `"Role CUSTOMER does not exist."` because the
`AspNetRoles` table in Azure SQL (`OutdoorsShopDB`) was empty. `AddToRoleAsync("Customer")` fails
at runtime if the role row has never been inserted. There was no mechanism to seed the roles.

## Decision

Seed ASP.NET Core Identity roles (`Administrator`, `Customer`) at application startup inside
`src/OutdoorsShop.Api/Program.cs`, immediately before `app.Run()`, using `RoleManager<IdentityRole>`.
The seeding block is idempotent (checks `RoleExistsAsync` before `CreateAsync`).

A minimal-API health endpoint `GET /api/health` → `200 {"status":"ok"}` was also added to satisfy
Creta's test requirement and fix the pre-existing 404 on that path.

## Changes Applied

- `src/OutdoorsShop.Api/Program.cs` — added `using Microsoft.AspNetCore.Identity` and two blocks:
  1. `app.MapGet("/api/health", ...)` — anonymous health endpoint
  2. `using (var scope = ...) { ... RoleManager seeding loop ... }` — runs before `app.Run()`

## Deployment

- Published API for Linux (`-r linux-x64 --self-contained false /p:UseAppHost=false`)
- Zipped using `[System.IO.Compression.ZipFile]::CreateFromDirectory` (not `Compress-Archive -Path *`
  — the wildcard form on Windows PowerShell produced a broken 3-entry archive missing the `.runtimeconfig.json`)
- Uploaded to `stoutdoorsdev/webapp-releases/api-dev.zip`, restarted `app-outdoors-api-dev`

## Verification

| Endpoint | Expected | Actual |
|---|---|---|
| `GET /api/health` | 200 `{"status":"ok"}` | ✅ 200 |
| `POST /api/v1/auth/register` | 200 + JWT | ✅ 200 |
| `POST /api/v1/auth/login` | 200 + JWT | ✅ 200 |

## Consequences

- Roles are created once on first boot; subsequent restarts skip the `CreateAsync` call (idempotent).
- Any future role additions (e.g. `Manager`) should be appended to the same seeding array.
- `Compress-Archive -Path *` must **not** be used for App Service zip packages — use `ZipFile.CreateFromDirectory` instead.
