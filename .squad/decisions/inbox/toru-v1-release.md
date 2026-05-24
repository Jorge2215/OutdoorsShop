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
| Strategy | `--no-ff` merge from `dev` → `main` |
| Commits merged | 21 |
| Date | 2026-05-23T21:12:05.666-03:00 |

## What Shipped

### Backend — .NET 10 Web API
- **7 controllers:** Auth, Products, Categories, Customers, Orders, Inventory, Reports
- JWT bearer auth (ASP.NET Core Identity), 15-min access token, 7-day refresh in HttpOnly cookie
- EF Core 10 + repository pattern, Azure SQL, CSV/Excel exports
- API versioned at `/api/v1/`

### Azure Functions
- `SeasonalDiscountFunction` — timer-triggered daily discount recalculation
- `PaymentConfirmationFunction` — queue-triggered payment confirmation processor
- `StockUpdateFunction` — queue-triggered inventory adjustment with reorder alerts

### Frontend — React + TypeScript
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

- `main` — production; requires PR + approval + status checks
- `dev` — integration; status checks only
- Feature branches off `dev`, merged via PR

## Benchmark Notes

This PoC was built entirely using GitHub Copilot + Squad (Cinnamon/Backend, Malta/Frontend, Creta/Testing, Toru/Architecture, Scribe/Docs, Ralph/Monitoring). The release demonstrates the full end-to-end capability of the AI-assisted development workflow.
