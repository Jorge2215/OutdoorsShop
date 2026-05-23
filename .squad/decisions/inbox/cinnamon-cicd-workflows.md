# Decision: GitHub Actions CI/CD Workflows

**Date:** 2026-05-23T20:39:55.398-03:00
**Author:** Cinnamon (Backend Dev)
**Status:** Accepted

## What

Three GitHub Actions workflows added to `.github/workflows/`:

| File | Trigger Paths | Purpose |
|---|---|---|
| `backend.yml` | `src/**` | Build + test full .NET solution |
| `frontend.yml` | `frontend/**` | Install deps + build React/Vite app |
| `functions.yml` | `src/OutdoorsShop.Functions/**` | Build Functions + run test suite + publish artifact |

## Key Choices

- **Solution path:** `OutdoorsShop.slnx` at repo root (not `src/OutdoorsShop.sln`). The `.slnx` format is the actual file on disk; dotnet CLI supports it in .NET 10.
- **Test runner (backend.yml):** runs against the full solution so all 74 test cases execute in one pass. Results saved as `.trx` and summarised in the GitHub job summary.
- **Test runner (functions.yml):** targets `OutdoorsShop.Tests.csproj` directly (no category filter applied) — all tests run, which includes Functions-related coverage without risk of missing tests due to missing category annotations.
- **Concurrency:** each workflow uses `cancel-in-progress: true` on the same branch to avoid redundant queued runs.
- **Permissions:** `contents: read` only — no write access needed for CI-only workflows.
- **Node cache:** `frontend.yml` caches npm via `cache-dependency-path: frontend/package-lock.json` for faster installs.
- **Azure deploy placeholder:** `functions.yml` includes a comment pointing to the Microsoft docs for adding an Azure Functions deploy step once publish-profile secrets are configured.

## Rationale

Monorepo path filters prevent unnecessary cross-job triggers (a backend change won't rebuild the frontend and vice versa). Separate workflows also allow independent badge URLs per component.
