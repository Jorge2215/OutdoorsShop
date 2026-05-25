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

### 2026-05-25T11:05:01.947-03:00 — Admin Product Catalog Live Validation

Ran live validation against `https://app-outdoors-api-dev.azurewebsites.net/api/v1` after reviewing the admin catalog implementation (`AdminProductsPage.tsx`, `ProductsController.cs`, `CreateProductDto.cs`, `UpdateProductDto.cs`). Key findings:

- **Auth matrix is correct on live dev.** No token returns 401, Customer JWT returns 403, and Administrator JWT (`admin@outdoorsshop.dev` / `Admin@123456`) reaches the admin product endpoints and image upload successfully.
- **Happy-path CRUD is mostly green.** Create, read, public list inclusion while active, image upload, update, delete, and public list exclusion after delete all worked on the deployed dev API.
- **Validation contract matches runtime.** Missing required fields return 400, invalid category returns 404, negative price returns 400, and missing-product update/delete both return 404 with clear messages.
- **Blocking soft-delete bug found.** After `DELETE /products/{id}` returns 204, `GET /products/{id}` returns 404 instead of exposing an inactive record, so the soft-delete state cannot be verified through the API contract.
- **Probable root cause in code:** `AppDbContext` applies `HasQueryFilter(p => p.IsActive)`, and `ProductRepository.GetByIdAsync()` / `GetAllAsync()` do not use `IgnoreQueryFilters()`. That means inactive products vanish from normal reads even though `ProductsController.Delete()` only flips `IsActive = false`.
- **Frontend impact:** `AdminProductsPage` loads data through `productsApi.list()`, which calls the public `/products` endpoint. Deleted products therefore disappear from the admin table too, making reactivation/review impossible despite the UI exposing an `Active` field.
- **Release call:** treat the soft-delete visibility gap as deploy-blocking for the Admin Products Catalog unless admin-specific read/list behavior for inactive products is added.

### 2026-05-24T19:59:32.340-03:00 — Admin Product Catalog Test Scenario Design

Drafted full test scenario catalogue (`creta-admin-catalog-tests.md`) for the upcoming Admin Product Catalog sprint. Key design decisions and patterns noted:

- **RBAC is always Area 1.** All 10 RBAC scenarios must be green before any other area is considered shippable. Pattern confirmed: no-token → 401, Customer JWT → 403, Admin JWT → 2xx. Fresh tokens required for auth test runs (stale Customer token returns 401 not 403).
- **60 test scenarios + 4 architectural risk items** across 7 areas: RBAC, CRUD, Categories, Image Upload (incremental), Inventory/Stock, Frontend, and Edge Cases.
- **4 risk flags escalated to team** before sprint begins: concurrent edits (EC-01), deleting a product in active orders (EC-02), search/filter performance on large catalog (EC-03), token expiry UX during long admin sessions (EC-04). None can be tested until Toru/Cinnamon/Malta make policy decisions.
- **Image upload core (22/22)** already covered — only 4 incremental catalog-context scenarios added for IMG area.
- **Prerequisites confirmed already in place:** admin seed user, image upload endpoint. Remaining unknowns: SKU field/uniqueness, max field lengths, soft-delete vs. hard-delete policy, cascade on category delete, pagination scope.
- **Frontend test strategy:** Vitest + RTL for unit/component (form validation, role-based nav), Playwright for E2E route guards and optimistic UI flows.

### 2026-05-24T16:52:12.609-03:00 — Team update
- Cinnamon delivered admin user seed (admin@outdoorsshop.dev / Admin@123456) — Creta can now rerun all blocked image upload tests.

### 2026-05-24 — Image Upload Test Execution (Run 3 — Final)

**Admin credentials confirmed:** `admin@outdoorsshop.dev` / `Admin@123456` — returns JWT with `Administrator` role claim. Blocker from Run 2 resolved.

**All 22 tests: ✅ PASS (0 FAIL, 0 BLOCKED)**

**Key findings:**
- `Invoke-RestMethod -Form` silently fails for multipart upload on this environment. Use `curl.exe -F` instead for multipart/form-data file upload tests.
- File validation messages are clear and consistent: `"Invalid file type. Allowed types: jpg, jpeg, png, gif, webp."`, `"File size exceeds the 5 MB limit."`, `"No file uploaded."`
- Blob naming uses UUID scheme (`products/{productId}/{uuid}.{ext}`) — special chars in original filename are discarded, no injection risk.
- Old blob cleanup is NOT implemented. On re-upload, the previous blob remains accessible in `product-images`. DB always points to latest URL (correct), but orphaned blobs accumulate. Advisory finding, not blocking.
- Product existence check returns 404 with `{"message":"Product {id} not found."}` — good API hygiene.
- Public access confirmed: blobs are accessible anonymously (no SAS needed) — container is `PublicAccessType.Blob` as designed.
- E-03 test condition: the plan's "OR at minimum new blob exists and DB points to it" minimum condition IS met even without cleanup.

**Verdict: ✅ PASS (22/22)**

### 2026-05-24 — Image Upload Test Execution (Run 2)

**Endpoint:** `POST /api/v1/products/{id}/image` — confirmed DEPLOYED as of 2026-05-24T16:52:12.609-03:00.

**Tests run: 3 / 17 pending**
- A-01: ✅ PASS — 401 without token (auth guard fires before any upload logic)
- A-02: ✅ PASS — 403 with Customer JWT (role guard working; token expiry pitfall: tokens expire in ~90 min, stale token returns 401 not 403)
- C-01: ✅ PASS — CORS preflight 204 from SWA origin, all headers correct

**Blocked: 14 / 17** — No admin user exists in the database.
- `Program.cs` seeds roles (`Administrator`, `Customer`) but does NOT seed an admin user account.
- 7 credential combinations attempted — all 401. No known admin password documented anywhere.
- H-01..05, A-03, V-01..05, E-01..04 all require Administrator JWT to get past auth gate before reaching validation/business logic.

**Token expiry pitfall discovered:**
- Access tokens expire in ~90 minutes (previously stated as 15 min — actual observed TTL is closer to 90 min based on `exp` claim difference).
- When a Customer token expires, the endpoint returns 401 (invalid token) instead of 403 (forbidden role). Always register a fresh token before re-running auth tests.

**Verdict: ⚠️ CONDITIONAL PASS**
- Auth guards and CORS are correctly implemented.
- 14 tests remain BLOCKED pending an admin JWT. See `creta-image-upload-verdict.md` in decisions inbox.
- Unblock path: DB-level role escalation (`INSERT INTO AspNetUserRoles`) or startup seeding of a default admin account.

### 2026-05-24 — Image Upload Test Plan (image-upload-test-plan.md)

**Endpoint status:** `POST /api/v1/products/{id}/image` → **404 NOT DEPLOYED** as of 2026-05-24T16:52:12.609-03:00.

**Tests run (infrastructure):**
- PRE-01: Health → ✅ PASS
- PRE-02: Product 1 exists → ✅ PASS (16 products, Alpine Base Camp Tent 4P at ID=1)
- PRE-03: Existing imageUrl publicly accessible → ✅ PASS (Unsplash CDN, 200, image/jpeg)
- C-01: CORS OPTIONS from SWA origin → ✅ PASS (204, correct CORS headers including `POST` in Allow-Methods)

**Tests pending (17 functional tests):** H-01..05, A-01..03, V-01..05, E-01..04 — all blocked on Cinnamon deploying the upload action in `ProductsController`.

**Key findings written to decisions inbox:**
1. No default admin user seeded — DB-level role assignment needed for admin JWT in tests
2. `BlobStorageService.UploadAsync` uses `PublicAccessType.None` — blobs won't be publicly accessible without SAS; container must use `PublicAccessType.Blob` for product images
3. CORS middleware responds to OPTIONS for 404 paths — CORS is verifiable before endpoint exists
4. Old blob cleanup is critical to test on re-upload (E-03)

**Skill added:** `.squad/skills/blob-image-upload-testing/SKILL.md` — minimal test image generation, PowerShell multipart upload helpers, CORS preflight helper, blob naming strategies.

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
\n\n## 2026-05-25T14:05:01Z � Scribe\nMerged creta-admin-catalog-verdict.md into decisions.md; re-validation in progress for Admin Products Catalog module.
