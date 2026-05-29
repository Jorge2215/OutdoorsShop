# Creta history (summary)

Recent activity summary:
- Added 7 change-password integration tests (happy path, wrong password, unauthenticated, validation scenarios, security checks).
- Noted test failures (6 contract tests 404) due to backend route mismatch; recommended Cinnamon align route to /api/v1/users/change-password.
- Fixed TestWebAppFactory bootstrap for Identity tables.
- Continued routine E2E and journey testing results archived.
- Reviewed async order receipts on 2026-05-27T16:51:34.303-03:00, added receipt endpoint integration coverage (401/200/403/404), and added receipt HTML encoding coverage for XSS-sensitive fields.

## Learnings
- 2026-05-28T23:14:22.978-03:00 — Read-only anonymous probe of the dev API confirmed the public surface is live: `GET /api/health`, `GET /swagger/index.html`, `GET /swagger/v1/swagger.json`, `GET /api/v1/products`, and `GET /api/v1/products/1` all returned 200 from `https://app-outdoors-api-dev.azurewebsites.net`.
- 2026-05-28T23:14:22.978-03:00 — Auth and admin gates are enforced on the live dev API for anonymous callers: `GET /api/v1/auth/me`, `POST /api/v1/auth/logout`, `POST /api/v1/products`, `PUT /api/v1/products/1`, `DELETE /api/v1/products/1`, and `PUT /api/v1/users/change-password` returned 401, while `GET /api/v1/products?includeInactive=true` and `GET /api/v1/products/1?includeInactive=true` returned 403 with an administrator-required message.
- 2026-05-28T23:14:22.978-03:00 — Live Swagger still exposes `api/v1/auth/*`, `api/v1/products*`, and `api/v1/users/change-password` without per-operation security metadata even though `src\OutdoorsShop.Api\Controllers\AuthController.cs` and `src\OutdoorsShop.Api\Controllers\ProductsController.cs` declare `[Authorize]` on several of those routes.
- 2026-05-28T23:14:22.978-03:00 — Read-only smoke probe against `https://app-outdoors-api-dev.azurewebsites.net` showed `GET /api/health`, `GET /swagger/index.html`, `GET /swagger/v1/swagger.json`, and `GET /api/v1/products` all returning 200, while anonymous `GET /api/v1/auth/me` and `POST /api/v1/auth/refresh` returned 401.
- 2026-05-28T23:14:22.978-03:00 — Auth contract on the live dev API currently behaves as cookie+bearer hybrid: `POST /api/v1/auth/login` returns 200, issues an `accessToken`, and sets an HttpOnly `refreshToken` cookie; the returned bearer token succeeds on `GET /api/v1/auth/me`.
- 2026-05-28T23:14:22.978-03:00 — Swagger generation in `src\OutdoorsShop.Api\Extensions\ServiceCollectionExtensions.cs` defines the document but no bearer security scheme/requirement, so `swagger/v1/swagger.json` does not flag `[Authorize]` routes like `api/v1/auth/me`, `api/v1/auth/logout`, or admin product mutations as secured.
- 2026-05-28T21:01:08.714-03:00 — Catalog filter/sort coverage now spans repository behavior (`tests\OutdoorsShop.Api.Tests\Repositories\ProductRepositoryTests.cs`), controller forwarding (`tests\OutdoorsShop.Api.Tests\Controllers\ProductsControllerTests.cs`), and HTTP integration (`tests\OutdoorsShop.Api.Tests\Integration\ProductsIntegrationTests.cs`).
- 2026-05-28T21:01:08.714-03:00 — `SearchProductsAsync` in `src\OutdoorsShop.Infrastructure\Repositories\ProductRepository.cs` is the unified catalog query path; legacy `GetAllAsync`, `GetByCategoryAsync`, and `SearchAsync` helpers delegate to it.
- 2026-05-28T21:01:08.714-03:00 — SQLite-backed integration tests can be case-sensitive for catalog `search` terms, so composition tests should use seeded casing unless the contract explicitly requires case-insensitive matching.
