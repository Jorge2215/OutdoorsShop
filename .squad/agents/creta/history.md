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

### 2026-05-24 — Full E2E Journey Test (live endpoints)

---

#### SUMMARY (auto-generated)
- Creta is the test suite agent for Outdoors Shop, covering xUnit, Vitest, Playwright, and integration tests.
- Key focus: auth flows, cart, orders, CORS, Azure Functions, and cross-origin edge cases.
- Recent highlights: E2E journey test (12/12 pass), Azure Functions live test, SQLite in-memory fix for integration tests, CORS verification, SameSite/JWT fixes, and coverage numbers.
- All major backend and frontend flows are now passing, with production blockers resolved (role seeding, health endpoint, CORS origins).
- Known issues: Azure Functions queue triggers require infra work, and seasonal discount function needs date injection for deterministic tests.
- API hostname is confirmed as app-outdoors-api-dev.azurewebsites.net (not outdoors-shop-api-dev).

---

**Auth flow shape (live API, verified):**
- `POST /api/v1/auth/register` body: `{name, email, password, confirmPassword}` — NOT `{firstName, lastName}`. Combined `name` field, `confirmPassword` required.
- `POST /api/v1/auth/login` returns `{accessToken, ...}` — use `login.accessToken`.
- Refresh token is in `HttpOnly` cookie `refreshToken`, not in the JSON body.

**Cart is fully client-side:**
- ADR-004 confirmed: no server-side cart endpoints exist. `GET /api/v1/cart` → 404 by design.
- Cart → checkout journey requires Playwright browser tests (Zustand + localStorage).

**Order creation schema:**
- `POST /api/v1/Orders`: `{shippingAddress: string, paymentMethod: string, items: [{productID, quantity, unitPrice}]}`
- Endpoint path uses capital `O`: `/api/v1/Orders` (not `/api/v1/orders`)

**Auth guards confirmed working:**
- `GET /api/v1/Orders`, `GET /api/v1/customers`, `GET /api/v1/inventory` all return 401 without token.
- Wrong password login returns 401 (not 400 — confirmed via Invoke-WebRequest).

**Production blocker — missing roles:**
- `POST /api/v1/auth/register` → 500 `"Role CUSTOMER does not exist."`
- Production `AspNetRoles` table is empty; `Program.cs` has no startup role seeding.
- Fix: add `RoleManager` seeding to `Program.cs` startup or run direct SQL against `OutdoorsShopDB`.

**No health endpoint:**
- `/api/health`, `/health`, `/api/v1/health` all return 404. No `app.MapHealthChecks` in `Program.cs`.

**OpenAPI only in Development:**
- `app.MapOpenApi()` is inside `if (app.Environment.IsDevelopment())`.
- Live API is running with `ASPNETCORE_ENVIRONMENT=Development`, so OpenAPI is accessible at `/openapi/v1.json`.

**Windows curl pitfall:**
- `curl.exe --data-binary` with single-quoted JSON fails on Windows PowerShell (JSON parse error on `{`).
- Always use `Invoke-RestMethod` / `Invoke-WebRequest` for JSON POST bodies on Windows.

**Unsplash images all healthy:**
- All 16 product imageUrls use `https://images.unsplash.com/photo-{id}?w=400&fit=crop&auto=format`.
- 4/4 spot-checked URLs return HTTP 200 via HEAD request.

**Azure Functions health confirmed:**
- `GET https://func-outdoors-dev.azurewebsites.net/api/health` → 200 `{"status":"ok"}`

**SWA frontend serving:**
- `https://wonderful-plant-0a1ca5f0f.7.azurestaticapps.net` → 200 OK

### 2026-05-24 — Azure Functions live test results

- Verified deployment of 4 functions: Health, SeasonalDiscount, PaymentConfirmation, StockUpdate
- Health and SeasonalDiscount tested successfully (HTTP/admin API)
- PaymentConfirmation and StockUpdate queue triggers not operable (queues missing, listeners inactive)
- Admin API could not trigger queue functions (400)
- Recommendations: provision queues in infra, investigate listener config, document admin-trigger limitations

(See orchestration/session logs for details)

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

- API unit tests: 45 passed, 13 skipped (integration) — 6 controller files × avg 7 tests
- Function tests: 17 passed, 4 skipped (seasonal) — 3 function files × avg 6 tests
- ReportsController and service-layer unit tests are out of scope for this sprint.

### 2026-05-23 — SQLite in-memory fix for WebApplicationFactory integration tests

**Root cause of EF Core 10.0 multi-provider conflict (fully diagnosed):**

- `IWebHostBuilder.ConfigureServices` callbacks in `WebApplicationFactory` run **BEFORE** `Program.cs` services are registered. `RemoveAll<DbContextOptions<AppDbContext>>()` was a no-op because SQL Server hadn't been registered yet.
- Filtering by assembly name (`Microsoft.EntityFrameworkCore.SqlServer`) or `FullName.Contains("SqlServer")` returned 0 results for the same reason.
- After our callbacks ran, `Program.cs`'s `AddDatabase(...)` added `UseSqlServer` (via `AddEntityFrameworkSqlServer()` internally), introducing a second `IDatabaseProvider` alongside our SQLite one.
- EF Core 10.0 validates the application service provider when the first `DbContext` instance is accessed and throws on two registered `IDatabaseProvider` implementations.

**The correct fix (approach that actually works):**

1. **Blank the connection string early** via `builder.UseSetting("ConnectionStrings:DefaultConnection", "")` — this runs before `Program.cs` reads its config from `WebApplicationBuilder`.
2. **Guard `AddDatabase`** in `ServiceCollectionExtensions.cs`: skip `UseSqlServer` when the connection string is empty/null.
3. **Register SQLite** in `ConfigureServices` callback — now the only provider, no conflict.
4. **Move seeding to `CreateHost` override** — runs after the full host (and all Identity services) is built, so `UserManager`, `RoleManager` etc. are available.

**Code pattern:**

```csharp
// ServiceCollectionExtensions.cs — null guard prevents SQL Server in tests
public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(connectionString))
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
    return services;
}

// TestWebAppFactory.cs — key excerpts
private readonly SqliteConnection _connection = new("DataSource=:memory:");

protected override void ConfigureWebHost(IWebHostBuilder builder)
{
    _connection.Open();
    builder.UseSetting("ConnectionStrings:DefaultConnection", "");  // skips AddDatabase's UseSqlServer
    builder.UseSetting("AzureStorage:ConnectionString", "");
    builder.UseEnvironment("Development");
    builder.ConfigureServices(services =>
    {
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        // blob mock...
    });
}

protected override IHost CreateHost(IHostBuilder builder)
{
    var host = base.CreateHost(builder);
    using var scope = host.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();  // builds schema from EF model — NOT Migrate()
    SeedTestData(scope.ServiceProvider).GetAwaiter().GetResult();
    return host;
}

protected override void Dispose(bool disposing)
{
    base.Dispose(disposing);
    if (disposing) _connection.Dispose();  // must stay open for factory lifetime
}
```

**Bonus fix discovered while running tests:**
- `AddJwtAuthentication`: added `options.MapInboundClaims = false` — the JWT middleware's default claim mapping was converting `sub` → `ClaimTypes.NameIdentifier`, silently breaking `User.FindFirstValue(JwtRegisteredClaimNames.Sub)` in `AuthController.Me()` and `Logout()`.

**Coverage numbers (after this sprint):**

- API tests: 58 passed, 0 skipped, 0 failed (45 unit + 13 integration)
- Function tests: 16 passed, 4 skipped (seasonal date-injection gap — unrelated to EF Core)
- Total: 74 passed, 4 skipped, 0 failed
### 2026-05-24 — Auth Fix Verification (role seeding deployed)

**E2E score: 12/12 ✅ (was 6/12)**

- Cinnamon's `Program.cs` role seeding fix confirmed working in production.
- `POST /api/v1/auth/register` now returns 200 with a full JWT on first registration (no 500).
- JWT contains `Customer` role claim under full URI key `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`.
- Register response includes `accessToken` immediately — no second login required after signup.
- `GET /api/health` → 200 `{"status":"ok"}` — health endpoint is now live (was 404 previously).
- **Orders list response is paginated** — `GET /api/v1/Orders` returns `{items, pageNumber, pageSize, totalCount, totalPages}`, NOT a plain array. Frontend and test code must access `.items[]` for the order list.
- All 6 previously-blocked steps (4, 5, 9, 10, 11, 12) now pass. No regressions on passing steps (1–3, 6–8).
- Updated SKILL.md known issues: health endpoint and role seeding are both resolved.

---

### 2026-05-24 — SameSite + JWT given_name Fix Verification (commit 22e971e)

**All 6 verification tests: ✅ PASS**

**SameSite fix confirmed:**
- `POST /api/v1/auth/register` and `POST /api/v1/auth/login` both return: `Set-Cookie: refreshToken=...; secure; samesite=none; httponly`
- `SameSite=Strict` is gone. `SameSite=None; Secure` is live.
- `/auth/refresh` (Test 3): 200 OK — rotated cookie also carries `samesite=none; secure`.
- Cross-origin refresh (Test 6): `POST /refresh` with `Origin: https://brave-beach-044d7c610.6.azurestaticapps.net` → 200 OK.

**JWT given_name fix confirmed:**
- `"given_name": "Creta Test"` in JWT payload — name, not email.
- `/auth/me` returns `{"name": "Creta Test", ...}`.

**Key observation (action needed):**
- When the SWA origin `brave-beach-044d7c610.6.azurestaticapps.net` was sent, the API response had **no CORS headers**. This means the new SWA URL is not in `AllowedOrigins`. The SameSite fix is correct, but browsers will still block the response until CORS origins are updated. Old config pointed to `stoutdoorswebdev.z1.web.core.windows.net`.

**Token rotation behavior:**
- After `POST /login`, the register-issued refresh token is invalidated. First refresh attempt with old cookie returned 401. After using the login cookie, refresh returned 200. Expected behavior — not a bug.

**API hostname clarification:**
- The provided mission URL `outdoors-shop-api-dev.azurewebsites.net` does NOT resolve (DNS failure).
- Working URL remains `app-outdoors-api-dev.azurewebsites.net` — same as previous sessions.

---

### 2026-05-24 — CORS Verification after Cinnamon-5 fix (commit cada3b2)

## 2026-05-24 — CORS Verification (creta-5)
- Ran 8-test cross-origin verification suite against live API after Cinnamon-5 CORS fix
- All 8 tests passed: preflight on auth+products, register/login/refresh flows, negative tests
- Confirmed: SWA origin (brave-beach-044d7c610.6.azurestaticapps.net) allowed
- Confirmed: stale blob origin, unknown origins, rogue SWA origin all blocked
- Confirmed: cookie samesite=none;secure;httponly on all auth flows
- Confirmed: JWT given_name = registered name (not email)
- Verdict: PASSED — Cinnamon's fix is solid

**All 8 tests: ✅ PASS**

**CORS preflight confirmed (all auth + product endpoints):**
- OPTIONS /api/v1/auth/register, /auth/login, /api/v1/products with `Origin: https://brave-beach-044d7c610.6.azurestaticapps.net` → 204 with correct `Access-Control-Allow-Origin` + `Access-Control-Allow-Credentials: true`

**Functional cross-origin flows confirmed:**
- Register (POST), Login (POST), and Refresh (POST) all return correct ACAO header under the SWA origin.
- All three `Set-Cookie: refreshToken` responses carry `secure; samesite=none; httponly` ✅

**JWT `given_name` claim confirmed correct:**
- `given_name` = the registered display name ("Creta Verify"), not the email address. Fix from commit 22e971e is still intact.

**Token rotation working:**
- Login → rotates register cookie; Refresh → rotates login cookie. Each refresh token value is distinct.

**Negative tests confirmed (origin blocklist working):**
- `stoutdoorswebdev.z1.web.core.windows.net` (stale blob) → no ACAO header (correctly rejected)
- `evil.example.com` (unknown) → no ACAO header (correctly rejected)
- `wonderful-plant-0a1ca5f0f.7.azurestaticapps.net` (rogue Azure platform entry, now cleared) → no ACAO header (correctly rejected)

**API hostname clarification (still valid):**
- `outdoors-shop-api-dev.azurewebsites.net` still does NOT resolve.
- Working URL: `app-outdoors-api-dev.azurewebsites.net` (unchanged from previous sessions).

---

## 2026-05-23 — Integration tests fixed (Creta)
Key learnings:
- ConfigureServices callbacks in WebApplicationFactory run BEFORE Program.cs services; RemoveAll<DbContextOptions<>>() is a no-op in that callback for later registrations.
- Guard AddDatabase to skip when connection string is empty.
- Blank connection string via builder.UseSetting to prevent production AddDatabase from registering providers.
- SqliteConnection for in-memory must remain open for factory lifetime; call EnsureCreated() and dispose in Dispose(bool).
- Set JwtBearerOptions.MapInboundClaims = false so 'sub' maps to JwtRegisteredClaimNames.Sub and User.FindFirstValue works in AuthController.Me().
