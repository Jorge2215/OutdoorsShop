---
name: "catalog-query-composition"
description: "Extend product catalog filters by threading new params through one controller/repository query path"
domain: "api-design"
confidence: "high"
source: "earned"
---

## Context
Use this when the product catalog gains new query parameters. The repo already had separate search/category paths, but MVP-safe changes come from extending the existing endpoint and consolidating the repository query rather than creating parallel read flows.

## Patterns
- Keep `GET /api/v1/products` as the single catalog read endpoint.
- Add new query params to `ProductsController.GetAll`, then forward all filter/sort inputs to one repository method.
- Compose optional filters with successive LINQ `Where` clauses so search, category, and price bounds combine with AND logic.
- Apply sorting after all filters, and normalize invalid sort values back to the documented default.
- Preserve older repository helpers by delegating them to the unified query method so existing callers keep working.
- Cover the contract at three levels when test infrastructure exists: controller forwarding, repository query behavior, and end-to-end HTTP integration.
- In SQLite-backed integration tests, use search tokens that match seeded casing unless case-insensitive search is itself the behavior under test.

## Examples
- `src\OutdoorsShop.Api\Controllers\ProductsController.cs` forwards `categoryId`, `search`, `minPrice`, `maxPrice`, and `sort` into `SearchProductsAsync`.
- `src\OutdoorsShop.Infrastructure\Repositories\ProductRepository.cs` normalizes `name_asc`, `price_asc`, and `price_desc`, then orders after filtering.
- `tests\OutdoorsShop.Api.Tests\Repositories\ProductRepositoryTests.cs` verifies AND-composed filters, invalid sort fallback, and empty results for inverted price bounds.

## Anti-Patterns
- Adding a second catalog endpoint just for new filters.
- Branching controller logic by filter combination (`if search`, `else if category`, etc.) once multiple filters need to compose.
- Applying sorting before all filters are in place.
- Returning `400 Bad Request` for `minPrice > maxPrice` when the catalog contract expects an empty array.
