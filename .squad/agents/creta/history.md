# Creta history (summary)

Recent activity summary:
- Added 7 change-password integration tests (happy path, wrong password, unauthenticated, validation scenarios, security checks).
- Noted test failures (6 contract tests 404) due to backend route mismatch; recommended Cinnamon align route to /api/v1/users/change-password.
- Fixed TestWebAppFactory bootstrap for Identity tables.
- Continued routine E2E and journey testing results archived.
- Reviewed async order receipts on 2026-05-27T16:51:34.303-03:00, added receipt endpoint integration coverage (401/200/403/404), and added receipt HTML encoding coverage for XSS-sensitive fields.

## Learnings
- 2026-05-28T21:01:08.714-03:00 — Catalog filter/sort coverage now spans repository behavior (`tests\OutdoorsShop.Api.Tests\Repositories\ProductRepositoryTests.cs`), controller forwarding (`tests\OutdoorsShop.Api.Tests\Controllers\ProductsControllerTests.cs`), and HTTP integration (`tests\OutdoorsShop.Api.Tests\Integration\ProductsIntegrationTests.cs`).
- 2026-05-28T21:01:08.714-03:00 — `SearchProductsAsync` in `src\OutdoorsShop.Infrastructure\Repositories\ProductRepository.cs` is the unified catalog query path; legacy `GetAllAsync`, `GetByCategoryAsync`, and `SearchAsync` helpers delegate to it.
- 2026-05-28T21:01:08.714-03:00 — SQLite-backed integration tests can be case-sensitive for catalog `search` terms, so composition tests should use seeded casing unless the contract explicitly requires case-insensitive matching.
