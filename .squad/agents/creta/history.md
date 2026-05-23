# Creta — History

## Core Context

- **Project:** Outdoors Shop
- **Owner:** Jorgito
- **Role:** Test Suite
- **Joined:** 2026-05-23
- **Repo:** https://github.com/Jorge2215/OutdoorsShop.git (dev = development, main = production)
- **Stack:** xUnit (.NET) | WebApplicationFactory | Vitest | React Testing Library | Playwright
- **Domain entities:** Products, Categories, Customers, Orders, OrderItems, Inventory
- **My scope:** xUnit backend unit tests, WebApplicationFactory integration tests, Vitest + React Testing Library frontend tests, Playwright E2E, edge cases (empty cart, out-of-stock, invalid orders, auth boundaries)
- **Team:** Toru (Architect), Cinnamon (Backend), Malta (Frontend), Scribe (Docs), Ralph (Monitor)
- **Purpose:** Proof of concept comparing GitHub Copilot + Squad vs traditional development

## Learnings

### 2026-05-23 — Comprehensive xUnit Test Suite (Controllers + Azure Functions)

**Moq patterns discovered:**

- `UserManager<T>` requires 9 constructor args — mock `IUserStore<T>` + 8 nulls. Wrap in `Mock<UserManager<T>>` to `.Setup()` methods. Same pattern for `SignInManager<T>` (7 args).
- `ClaimsPrincipal` for controller unit tests: use `new ClaimsIdentity(claims, "Test")` — the `"Test"` authenticationType is required for `Identity.IsAuthenticated` to return true.
- `User.IsInRole("Administrator")` checks `ClaimTypes.Role` (the full URI `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`), not the string `"role"`.
- `[Authorize(Roles = "Administrator")]` attribute enforcement is middleware-level and NOT testable in controller unit tests. Must use integration tests for attribute-based auth.

**EF Core InMemory patterns:**

- Global query filters (`.HasQueryFilter(p => p.IsActive)`) ARE applied by InMemory. Use `.IgnoreQueryFilters()` to bypass in tests.
- `HasData()` seed in `OnModelCreating` is NOT seeded into InMemory databases — must insert manually in tests.
- Use `Guid.NewGuid().ToString()` as database name to ensure isolation between tests.

**EF Core 10.0 known issue — WebApplicationFactory multi-provider conflict:**

- When `WebApplicationFactory` replaces `DbContextOptions<AppDbContext>` (SqlServer → InMemory), EF Core 10.0 throws: `"Only a single database provider can be registered in a service provider."` even after `RemoveAll<DbContextOptions<AppDbContext>>()`.
- Root cause: EF Core 10.0 validates the application service provider for conflicting provider registrations. `AddEntityFrameworkStores<AppDbContext>()` (from Identity) may contribute to the conflict by re-registering provider services.
- **Remediation (not yet applied):** Use `UseSqlite("DataSource=:memory:")` with a shared `SqliteConnection` instead of `UseInMemoryDatabase`. SQLite counts as a single provider replacement for SqlServer. See `.squad/decisions/inbox/creta-test-strategy.md` §4.
- Current workaround: All 13 integration tests are `[Fact(Skip = ...)]` with a detailed skip reason.

**SeasonalDiscountFunction date injection gap:**

- `SeasonalDiscountFunction.Run()` reads `DateTime.UtcNow` directly — no `IClock`/`IDateTimeProvider`/`TimeProvider` abstraction.
- Makes season-specific tests non-deterministic across calendar months.
- **Recommendation:** Inject `TimeProvider` (built into .NET 8+) into `SeasonalDiscountFunction`. Then tests can `Mock<TimeProvider>().Setup(tp => tp.GetUtcNow()).Returns(January date)` to force winter season.
- 4 season tests skipped; 2 season-agnostic tests active.

**Blob storage initialization issue:**

- `appsettings.json` has `"AzureStorage:ConnectionString": "REPLACE_WITH_STORAGE_CONNECTION"` (placeholder). `AddBlobStorage` creates `new BlobServiceClient(connectionString)` at DI registration time — throws `FormatException` for placeholder.
- Fix: `builder.UseSetting("AzureStorage:ConnectionString", "")` in TestWebAppFactory — empty string falls back to `"UseDevelopmentStorage=true"` (valid Azurite format). Also mock `IBlobStorageService` as belt-and-suspenders.

**Coverage numbers (as of this sprint):**

- API unit tests: 46 passed, 13 skipped (integration) — 6 controller files × avg 7 tests
- Function tests: 17 passed, 4 skipped (seasonal) — 3 function files × avg 6 tests
- ReportsController and service-layer unit tests are out of scope for this sprint.
