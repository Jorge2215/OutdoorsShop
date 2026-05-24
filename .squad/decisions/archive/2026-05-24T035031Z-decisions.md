# Squad Decisions

## Active Decisions

---

### 2026-05-23: Project Initialized

**By:** Jorgito (via Squad Coordinator)  
**What:** Outdoors Shop project initialized. Team hired from The Wind-Up Bird Chronicle universe: Toru (Architect), Cinnamon (Backend Developer), Malta (Frontend Developer), Creta (Test Suite), Scribe (Documentation), Ralph (Work Monitor). Squad v0.9.4.  
**Why:** New project kickoff.  

---

### 2026-05-23: Branch Strategy

**By:** Jorgito  
**What:** `dev` is the active development branch. `main` is for production deployment. All squad feature branches follow `squad/{issue-number}-{slug}` convention and target `dev`.  
**Why:** Specified in project brief. Standard feature-branch promotion: feature → dev → main for release.  

---

### 2026-05-23: Tech Stack Confirmed

**By:** Jorgito  
**What:** React + TypeScript (frontend), .NET 10 Web API C# ASP.NET Core (backend), Azure SQL Database with Adventure Works-inspired schema (data), Azure Functions .NET isolated (serverless auxiliary tasks), Azure Blob Storage (assets and reports), JWT role-based auth with roles Administrator and Customer.  
**Why:** Specified in project brief. Baseline for all implementation decisions.  

---

### 2026-05-23: Domain Model Scope

**By:** Jorgito  
**What:** Core entities are Products, Categories (Camping, Trekking, Cycling, Climbing), Customers, Orders, and Inventory. Inspired by Adventure Works simplified schema on Azure SQL Database.  
**Why:** Specified in project brief. Cinnamon owns the data model design under Toru's architecture review.  

---

### 2026-05-23: Azure Function Scope

**By:** Jorgito  
**What:** Azure Functions handle three auxiliary tasks: seasonal discounts (scheduled), payment confirmation (event-driven), stock updates (event-driven).  
**Why:** Specified in project brief. Keeps the Web API lean — background/async work lives in Functions.  

---

### 2026-05-23: Storage Account Scope

**By:** Jorgito  
**What:** Azure Storage Account (Blob) stores product images, order receipts, and exported reports (CSV/Excel). The Web API integrates with the storage account for read/write operations.  
**Why:** Specified in project brief.  

---

### 2026-05-23: Monorepo folder structure adopted
**By:** Toru (Architect)
**What:** Single Git repository with three top-level source areas: `src/` (.NET projects), `frontend/` (React + TypeScript), and `infra/` (Bicep IaC). A shared `OutdoorsShop.sln` ties all .NET projects together. Azure Functions live in `src/OutdoorsShop.Functions/` as an isolated-process project.
**Why:** Keeps the full stack version-aligned in one repo. Simplifies cross-concern refactors and CI/CD. Avoids submodule complexity for a single-team PoC.

---

### 2026-05-23: .NET project layering (Api / Core / Infrastructure / Functions / Tests)
**By:** Toru (Architect)
**What:** Five .NET projects: `OutdoorsShop.Api` (controllers, middleware, startup), `OutdoorsShop.Core` (domain entities, interfaces — no dependencies), `OutdoorsShop.Infrastructure` (EF Core, repositories, storage clients), `OutdoorsShop.Functions` (Azure Functions isolated), `OutdoorsShop.Tests` (xUnit).
**Why:** Classic onion/clean separation. Core has zero external dependencies, making unit testing trivial. Infrastructure is the only layer that touches EF Core and Azure SDKs.

---

### 2026-05-23: React app scaffolded with Vite + TypeScript
**By:** Toru (Architect)
**What:** React frontend lives in `frontend/` and is scaffolded with `npm create vite@latest -- --template react-ts`. Build output goes to `frontend/dist/`.
**Why:** Vite is the current standard React toolchain. Fast HMR for development, clean static output for deployment.

---

### 2026-05-23: Adventure Works-inspired schema — six core tables
**By:** Toru (Architect)
**What:** Schema has six application tables: `Categories`, `Products`, `Customers`, `Orders`, `OrderItems`, `Inventory`. Identity (users/roles) uses ASP.NET Core Identity default tables (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, etc.). `Customers.UserId` is a FK to `AspNetUsers.Id`.
**Why:** Separates authentication identity (ASP.NET Identity) from business domain (Customers). A Customer record is created upon registration and linked to the Identity user. This pattern aligns with Adventure Works's Person/Customer split.

---

### 2026-05-23: EF Core with repository pattern via OutdoorsShop.Infrastructure
**By:** Toru (Architect)
**What:** EF Core 10 is the ORM. DbContext lives in `OutdoorsShop.Infrastructure`. Repositories implement interfaces defined in `OutdoorsShop.Core`. No raw ADO.NET in application code. Migrations managed via EF Core CLI (`dotnet ef migrations`).
**Why:** Repository pattern decouples controllers from the ORM, making unit testing with in-memory providers or mocks straightforward. Cinnamon owns implementation; interfaces in Core enforce the contract.

---

### 2026-05-23: Soft deletes via IsActive flag on Products and Categories
**By:** Toru (Architect)
**What:** `Products.IsActive` and `Categories.IsActive` columns implement logical delete. Physical DELETE is not used for these tables. All queries filter `WHERE IsActive = 1` by default via EF Core global query filters.
**Why:** Product removal from catalog must not break historical order records that reference the product. Soft delete preserves referential integrity.

---

### 2026-05-23: Inventory table ReorderThreshold for stock alerts
**By:** Toru (Architect)
**What:** `Inventory.ReorderThreshold` is an INT column. When the `StockUpdateQueue` function reduces quantity, it checks if `Quantity <= ReorderThreshold` and can raise an alert (logged to App Insights for PoC; extensible to email later).
**Why:** Business requirement: inventory tracking. Threshold makes the stock-update function actionable without hard-coded logic.

---

### 2026-05-23: REST API base path /api/v1 with versioning from day one
**By:** Toru (Architect)
**What:** All Web API routes are prefixed `/api/v1/`. ASP.NET Core API versioning middleware (`Asp.Versioning`) is installed from the start. Current version is v1. Future breaking changes go to v2 without disrupting existing clients.
**Why:** Adding versioning retroactively is painful. Zero cost to add it now; v1 prefix becomes the permanent base.

---

### 2026-05-23: Six resource groups — Products, Categories, Customers, Orders, Inventory, Auth
**By:** Toru (Architect)
**What:** Controllers: `ProductsController`, `CategoriesController`, `CustomersController`, `OrdersController`, `InventoryController`, `AuthController`. A `ReportsController` is added for CSV/Excel export endpoints. Each controller maps exactly to one infrastructure concern.
**Why:** Matches the domain model 1:1. No "god controller." Easy for Cinnamon to implement one controller per sprint.

---

### 2026-05-23: OpenAPI/Swagger via Swashbuckle with XML doc comments
**By:** Toru (Architect)
**What:** Swashbuckle.AspNetCore added to `OutdoorsShop.Api`. XML documentation generated (`GenerateDocumentationFile = true` in .csproj). Swagger UI available at `/swagger` in dev only (disabled in prod). All endpoints annotated with `[ProducesResponseType]`.
**Why:** Swagger UI is the integration reference for Malta (Frontend). Disabling in prod avoids exposing the API surface to the public.

---

### 2026-05-23: Cart is client-side state, not server-side
**By:** Toru (Architect)
**What:** No `Cart` or `CartItems` table in the database. The shopping cart lives entirely in the React frontend (localStorage + Zustand store). On checkout, the frontend sends a `POST /api/v1/orders` with the full order payload.
**Why:** Simplifies the backend significantly for a PoC. No session management or abandoned-cart cleanup needed. Cart state survives page reload via localStorage persistence.

---

### 2026-05-23: ASP.NET Core Identity + JWT bearer tokens
**By:** Toru (Architect)
**What:** Authentication uses ASP.NET Core Identity for user/password/role management backed by Azure SQL. JWT bearer tokens are issued by the API's `AuthController` using `System.IdentityModel.Tokens.Jwt`. No third-party identity provider (no Entra ID, no Auth0) for PoC.
**Why:** Self-contained auth keeps the PoC infrastructure minimal. ASP.NET Core Identity is the standard .NET 10 approach. JWT is stateless and naturally fits React SPA + API architecture.

---

### 2026-05-23: JWT access token 15 min, refresh token 7 days stored in HttpOnly cookie
**By:** Toru (Architect)
**What:** Access tokens expire in 15 minutes. Refresh tokens expire in 7 days and are issued as `HttpOnly`, `Secure`, `SameSite=Strict` cookies. `POST /api/v1/auth/refresh` accepts the cookie and issues a new access token. Refresh tokens are stored hashed in `AspNetUserTokens`.
**Why:** Short-lived access tokens limit exposure window. HttpOnly cookie for refresh prevents XSS token theft. Storing refresh token hash in Identity's UserTokens table enables server-side revocation.

---

### 2026-05-23: Two roles — Administrator and Customer
**By:** Toru (Architect)
**What:** Role `Administrator`: full access to all endpoints including `GET/POST/PUT/DELETE` on products, categories, inventory, all orders, and reports. Role `Customer`: read products/categories, manage own orders (`GET/POST` on own orders only), read own customer profile (`GET/PUT`). No anonymous access beyond product/category browsing.
**Why:** Matches the project brief exactly. RBAC enforced at the controller level via `[Authorize(Roles = "Administrator")]` and `[Authorize]` attributes. Fine-grained resource ownership (customers seeing only their orders) enforced in service layer by comparing `CustomerId` from JWT `sub` claim.

---

### 2026-05-23: JWT claims structure
**By:** Toru (Architect)
**What:** JWT payload includes: `sub` (UserId GUID), `email`, `role` (Administrator | Customer), `given_name`, `family_name`, `customer_id` (CustomerId INT — added as custom claim for Customers), `jti` (unique token ID), `iss` (issuer), `aud` (audience), `exp`, `iat`.
**Why:** `customer_id` custom claim avoids a DB round-trip to resolve UserId → CustomerId on every request. `jti` enables token blacklisting if needed. Standard claims (`iss`, `aud`) required for proper JWT validation.

---

### 2026-05-23: Frontend stores access token in memory (not localStorage)
**By:** Toru (Architect)
**What:** The React app stores the access token in a Zustand auth store (in-memory, not persisted to localStorage or sessionStorage). On page refresh, the app calls `POST /api/v1/auth/refresh` using the HttpOnly cookie to silently re-issue the token.
**Why:** Storing JWTs in localStorage exposes them to XSS. In-memory + HttpOnly cookie refresh is the current best practice for SPA authentication security.

---

### 2026-05-23: Azure resource naming convention
**By:** Toru (Architect)
**What:** Pattern `{abbreviation}-outdoors-{environment}` (e.g., `app-outdoors-api-dev`, `kv-outdoors-prod`). Storage accounts use no hyphens due to Azure limits: `stoutdoorsdev`, `stoutdoorsprod`. Two resource groups: `rg-outdoors-dev` and `rg-outdoors-prod`.
**Why:** Predictable names reduce lookup friction. Abbreviations follow Microsoft CAF (Cloud Adoption Framework) conventions: `app`, `asp`, `sql`, `sqldb`, `st`, `func`, `kv`, `appi`, `law`.

---

### 2026-05-23: App Service B1 for dev, P2v3 for prod
**By:** Toru (Architect)
**What:** Web API hosted on Azure App Service. Dev uses B1 (Basic) plan. Production uses P2v3 (Premium v3) for auto-scale readiness. No VNet or private endpoints for PoC.
**Why:** App Service over Container Apps for simplicity — no container registry or orchestration overhead for a PoC. P2v3 chosen over P1v3 for baseline production memory headroom.

---

### 2026-05-23: Azure SQL Basic/S0 for dev, S2 for prod
**By:** Toru (Architect)
**What:** Azure SQL Database on the DTU model. Dev: Basic (5 DTU, sufficient for seeding and testing). Prod: S2 (50 DTU) for concurrent user load. Both use geo-redundant backup.
**Why:** DTU model is simpler to reason about for a PoC. S2 prod gives 50 DTU and 250 GB — enough for the domain scope.

---

### 2026-05-23: Azure Functions on Consumption plan
**By:** Toru (Architect)
**What:** Functions App uses the Consumption (Y1) hosting plan for both dev and prod.
**Why:** The three functions (discount timer, payment queue, stock queue) are infrequent. Consumption cost is near-zero for PoC traffic. Scale-to-zero is acceptable for auxiliary background tasks.

---

### 2026-05-23: Key Vault with managed identity access
**By:** Toru (Architect)
**What:** All secrets (connection strings, JWT signing key, Storage SAS token) stored in Azure Key Vault. App Service and Functions App access Key Vault via system-assigned managed identity with `Key Vault Secrets User` role. No connection strings in app settings or code.
**Why:** Eliminates secret rotation risk and credential exposure in CI/CD logs. Managed identity removes the need to manage service principal credentials.

---

### 2026-05-23: Storage Account containers and access levels
**By:** Toru (Architect)
**What:** Three Blob containers: `product-images` (Blob-level public read — product images are public), `order-receipts` (private — SAS URL generated by API on request), `exports` (private — SAS URL generated by API on download).
**Why:** Product images need public CDN-friendly URLs. Receipts and exports contain PII and must not be publicly listable.

---

### 2026-05-23: Three GitHub Actions workflows — backend, frontend, functions
**By:** Toru (Architect)
**What:** Three workflow files: `.github/workflows/backend.yml` (triggers on `src/OutdoorsShop.Api/**` and `src/OutdoorsShop.Core/**` and `src/OutdoorsShop.Infrastructure/**` changes), `.github/workflows/frontend.yml` (triggers on `frontend/**`), `.github/workflows/functions.yml` (triggers on `src/OutdoorsShop.Functions/**`). Each workflow runs on push to `dev` (deploy to dev environment) and push to `main` (deploy to prod environment).
**Why:** Scoped path filters prevent a frontend change from triggering a backend deployment. Independent pipelines allow the teams to move at different speeds.

---

### 2026-05-23: Secrets in GitHub Environments (dev / prod) using OIDC federated credentials
**By:** Toru (Architect)
**What:** GitHub Environments `dev` and `prod` are configured. Azure authentication uses OIDC (`azure/login@v2` with `client-id`, `tenant-id`, `subscription-id`) — no service principal secret stored. Application secrets (connection strings, JWT key) are stored in GitHub Environment secrets and injected as Azure App Service application settings at deploy time (not checked into code).
**Why:** OIDC eliminates the need to rotate service principal secrets. GitHub Environments provide environment-scoped secret isolation (prod secrets are not accessible from dev pipeline runs).

---

### 2026-05-23: Branch protection rules
**By:** Toru (Architect)
**What:** `main` branch: required status checks (backend build+test, frontend build+test), required PR with 1 approval, no direct push. `dev` branch: required status checks (build+test pass), direct push allowed for squad agents (to unblock flow during PoC). Squad feature branches follow `squad/{slug}` → PR → `dev`.
**Why:** `main` is protected to prevent broken production deployments. `dev` protection ensures tests pass before integration but allows direct push for squad agility during PoC phase.

---

### 2026-05-23: CI pipeline steps — build, test, migrate, deploy
**By:** Toru (Architect)
**What:** Backend pipeline steps: (1) `dotnet restore`, (2) `dotnet build --no-restore`, (3) `dotnet test --no-build --collect:"XPlat Code Coverage"`, (4) `dotnet publish -c Release -o ./publish`, (5) `dotnet ef database update` (dev only — prod migrations are script-based review), (6) `az webapp deploy`. Functions pipeline mirrors steps 1–4 then `az functionapp deployment source config-zip`.
**Why:** EF migrations run automatically on dev for speed. On prod, a reviewed SQL script is applied manually (or via deployment slot swap) to prevent accidental schema destruction. Code coverage collected for visibility without blocking the pipeline.

---

# ADR-001: Monorepo Folder Structure

**Date:** 2026-05-23  
**Author:** Toru (Architect)  
**Status:** Accepted

## Decision

Adopt a single Git repository with three top-level source areas:

```
/src/           .NET projects (Api, Core, Infrastructure, Functions)
/frontend/      React + TypeScript (Vite)
/infra/         Bicep infrastructure-as-code
/tests/         .NET test projects (Api.Tests, Functions.Tests)
/frontend/tests Vitest + React Testing Library
/.github/       GitHub Actions workflows
```

A single `OutdoorsShop.sln` at repo root ties all .NET projects together.

## Rationale

A monorepo keeps the entire stack version-aligned in one repository. Cross-concern refactors (e.g., renaming a DTO that affects both the API and frontend) are atomic. It simplifies CI/CD configuration — all pipelines live in one `.github/workflows/` directory with path-based filters. Avoids submodule complexity for a single-team PoC.

## Consequences

- All developers clone one repository.
- CI workflows must use `paths:` filters to avoid rebuilding unaffected layers on every push.
- Frontend and backend can be deployed independently despite living in the same repo.

---

# ADR-002: .NET Clean Architecture Layering

**Date:** 2026-05-23  
**Author:** Toru (Architect)  
**Status:** Accepted

## Decision

Organise the .NET solution into four projects following clean/onion architecture:

| Project | Layer | Dependencies |
|---|---|---|
| `OutdoorsShop.Core` | Domain | None |
| `OutdoorsShop.Infrastructure` | Data/Services | Core |
| `OutdoorsShop.Api` | Presentation | Core, Infrastructure |
| `OutdoorsShop.Functions` | Background | Core, Infrastructure |

`OutdoorsShop.Core` contains: domain entities, repository interfaces, service interfaces, DTOs, enums, and domain exceptions. It references no NuGet packages with external runtime dependencies.

`OutdoorsShop.Infrastructure` contains: EF Core `DbContext`, repository implementations, Azure Storage clients, email stub clients.

`OutdoorsShop.Api` contains: controllers, middleware, program startup, request/response models, validators (FluentValidation).

`OutdoorsShop.Functions` is an isolated-process Azure Functions project. It references Core and Infrastructure but is deployed independently.

## Rationale

Classic onion separation. Core having zero external dependencies makes domain logic trivially unit-testable with no mocking of infrastructure. Infrastructure is the only layer that touches EF Core and Azure SDKs, so switching persistence or cloud provider is isolated to one project.

## Consequences

- Circular dependency between Core and Infrastructure is forbidden by compiler (Core cannot reference Infrastructure).
- Dependency injection wires Infrastructure implementations to Core interfaces at startup in `OutdoorsShop.Api`.
- `OutdoorsShop.Functions` re-uses the same DI wiring pattern via `HostBuilder`.

---

# ADR-003: JWT Bearer Authentication with ASP.NET Core Identity

**Date:** 2026-05-23  
**Author:** Toru (Architect)  
**Status:** Accepted

## Decision

Use **ASP.NET Core Identity** for user/role management combined with **JWT bearer tokens** for stateless API authentication.

**Token strategy:**
- Access token: RS256 signed, 15-minute expiry, stored in JavaScript memory (not localStorage).
- Refresh token: opaque random value, 7-day expiry, stored in an `HttpOnly; Secure; SameSite=Strict` cookie. Hashed SHA-256 before storage in `AspNetUserTokens`.

**Roles:** Two roles — `Administrator` and `Customer`.

**Custom JWT claims:**
- `customer_id` (GUID): maps the Identity user to the business Customer record. Avoids a DB round-trip on every authenticated request.
- `email`, `role` (standard claims).

**Refresh flow:** `POST /api/v1/auth/refresh` reads the HttpOnly cookie, validates the hashed token from `AspNetUserTokens`, issues a new access token and rotates the refresh token.

## Rationale

ASP.NET Core Identity provides battle-tested user/role management, password hashing, and account lockout out of the box. JWT bearer allows stateless horizontal scaling of the API without sticky sessions. Storing the access token in memory and the refresh token in an HttpOnly cookie is the recommended browser security pattern — it eliminates XSS access to long-lived tokens.

## Consequences

- The React frontend must implement a silent token refresh mechanism (intercepting 401 responses to call `/auth/refresh` before retrying).
- `AspNetUsers`, `AspNetRoles`, and `AspNetUserRoles` tables are created by Identity migrations.
- JWT signing key must be stored in Azure Key Vault, never in `appsettings.json`.

---

# ADR-004: Client-Side Cart with No Database Persistence

**Date:** 2026-05-23  
**Author:** Toru (Architect)  
**Status:** Accepted

## Decision

The shopping cart is managed entirely on the client side using **Zustand** state management with **localStorage** persistence. There is no `Cart` or `CartItem` table in the database.

Cart state is only persisted to the backend when the customer places an order — at that point the cart contents become `OrderItems` records in the database.

The `/api/v1/cart` endpoint group defined in the API contract is a thin pass-through for **order placement only**: `POST /api/v1/cart/checkout` converts the client-submitted cart into an Order.

## Rationale

A server-side cart adds significant database complexity (cart expiry, anonymous cart merging on login, orphaned records cleanup) for a PoC that does not require it. Client-side cart via Zustand + localStorage satisfies the business requirement of persisting the cart between browser sessions without backend infrastructure. This is consistent with how many small-to-medium e-commerce frontends operate.

## Consequences

- Cart state is lost if the customer switches browsers or devices — acceptable for PoC scope.
- `POST /api/v1/cart/checkout` accepts the full cart payload and creates an Order transactionally.
- A `Cart` table can be added in a future iteration without changing the existing Order API.
- Malta (Frontend) owns cart state management in `frontend/src/store/cartStore.ts`.

---

# ADR-005: EF Core 10 with Repository Pattern

**Date:** 2026-05-23  
**Author:** Toru (Architect)  
**Status:** Accepted

## Decision

Use **EF Core 10** as the ORM with a **repository pattern** enforced via interfaces.

- Repository interfaces are defined in `OutdoorsShop.Core/Interfaces/` (e.g., `IProductRepository`, `IOrderRepository`).
- Concrete implementations live in `OutdoorsShop.Infrastructure/Repositories/`.
- `OutdoorsShopDbContext` lives in `OutdoorsShop.Infrastructure/Data/`.
- Migrations are managed via EF Core CLI (`dotnet ef migrations add`). Migration files are committed to source control.
- No raw ADO.NET in application code. Stored procedures are not used in this PoC.

**Global Query Filters applied:**
- `Products`: `WHERE IsActive = 1`
- `Categories`: `WHERE IsActive = 1`

**Mapping:** Manual mapping or **Mapster** for entity → DTO projection. AutoMapper is explicitly excluded due to runtime magic complexity.

## Rationale

EF Core provides type-safe LINQ queries, migrations, and change tracking without raw SQL. Repository interfaces in Core decouple the application from the ORM, making unit testing straightforward with `InMemoryDatabase` or mock repositories. Mapster was chosen over AutoMapper because it is faster at runtime, has a simpler configuration model, and its compile-time mapping generation eliminates the AutoMapper "no mapping found" class of runtime errors.

## Consequences

- Cinnamon (Backend) owns DbContext configuration and migration authoring.
- Tests use `UseInMemoryDatabase` provider for repository unit tests.
- Adding a new entity requires: entity in Core, interface in Core, migration in Infrastructure, registration in DI.

---

# ADR-006: Azure Key Vault with Managed Identity for Secrets

**Date:** 2026-05-23  
**Author:** Toru (Architect)  
**Status:** Accepted

## Decision

All application secrets (database connection strings, JWT signing key, Storage Account keys) are stored in **Azure Key Vault**. The App Service and Functions App access Key Vault using **system-assigned managed identities** — no service principal credentials or connection strings appear in `appsettings.json` or application settings.

**Key Vault naming:** `kv-outdoors-{env}` (e.g., `kv-outdoors-dev`).

**Secret names (convention):** `{resource}--{property}` using double-dash to match ASP.NET Core configuration hierarchy (e.g., `ConnectionStrings--DefaultConnection`, `Jwt--SigningKey`).

**Local development:** Developers use `dotnet user-secrets` and Azure CLI authentication (`az login`). The `DefaultAzureCredential` chain handles both local and production scenarios transparently.

## Rationale

Secrets in source control or app settings environment variables are a primary attack surface (OWASP A02: Cryptographic Failures / Secrets Exposure). Managed identity eliminates long-lived credentials entirely — there is nothing to rotate, leak, or store. This is the Azure-recommended pattern for App Service and Functions workloads. `DefaultAzureCredential` allows the same code to work locally (via Azure CLI token) and in production (via managed identity) without any conditional logic.

## Consequences

- The App Service and Functions App managed identities must be granted `Key Vault Secrets User` role on the Key Vault (RBAC, not vault access policies — access policies are legacy).
- Key Vault is provisioned in `infra/modules/keyvault.bicep` before the App Service is deployed.
- Rotation of the JWT signing key triggers a brief window where existing access tokens are invalid — acceptable for PoC; document as known limitation.
- Local developers need `az login` and Key Vault `Secrets User` assignment on their Azure user identity.

---

### 2026-05-23: REST API base path /api/v1 with versioning from day one
**By:** Toru (Architect)
**What:** All Web API routes are prefixed `/api/v1/`. ASP.NET Core API versioning middleware (`Asp.Versioning`) is installed from the start. Current version is v1. Future breaking changes go to v2 without disrupting existing clients.
**Why:** Adding versioning retroactively is painful. Zero cost to add it now; v1 prefix becomes the permanent base.

### 2026-05-23: Six resource groups — Products, Categories, Customers, Orders, Inventory, Auth
**By:** Toru (Architect)
**What:** Controllers: `ProductsController`, `CategoriesController`, `CustomersController`, `OrdersController`, `InventoryController`, `AuthController`. A `ReportsController` is added for CSV/Excel export endpoints. Each controller maps exactly to one infrastructure concern.
**Why:** Matches the domain model 1:1. No "god controller." Easy for Cinnamon to implement one controller per sprint.

### 2026-05-23: OpenAPI/Swagger via Swashbuckle with XML doc comments
**By:** Toru (Architect)
**What:** Swashbuckle.AspNetCore added to `OutdoorsShop.Api`. XML documentation generated (`GenerateDocumentationFile = true` in .csproj). Swagger UI available at `/swagger` in dev only (disabled in prod). All endpoints annotated with `[ProducesResponseType]`.
**Why:** Swagger UI is the integration reference for Malta (Frontend). Disabling in prod avoids exposing the API surface to the public.

### 2026-05-23: Cart is client-side state, not server-side
**By:** Toru (Architect)
**What:** No `Cart` or `CartItems` table in the database. The shopping cart lives entirely in the React frontend (localStorage + Zustand store). On checkout, the frontend sends a `POST /api/v1/orders` with the full order payload.
**Why:** Simplifies the backend significantly for a PoC. No session management or abandoned-cart cleanup needed. Cart state survives page reload via localStorage persistence.

---

### 2026-05-23: ASP.NET Core Identity + JWT bearer tokens
**By:** Toru (Architect)
**What:** Authentication uses ASP.NET Core Identity for user/password/role management backed by Azure SQL. JWT bearer tokens are issued by the API's `AuthController` using `System.IdentityModel.Tokens.Jwt`. No third-party identity provider (no Entra ID, no Auth0) for PoC.
**Why:** Self-contained auth keeps the PoC infrastructure minimal. ASP.NET Core Identity is the standard .NET 10 approach. JWT is stateless and naturally fits React SPA + API architecture.

### 2026-05-23: JWT access token 15 min, refresh token 7 days stored in HttpOnly cookie
**By:** Toru (Architect)
**What:** Access tokens expire in 15 minutes. Refresh tokens expire in 7 days and are issued as `HttpOnly`, `Secure`, `SameSite=Strict` cookies. `POST /api/v1/auth/refresh` accepts the cookie and issues a new access token. Refresh tokens are stored hashed in `AspNetUserTokens`.
**Why:** Short-lived access tokens limit exposure window. HttpOnly cookie for refresh prevents XSS token theft. Storing refresh token hash in Identity's UserTokens table enables server-side revocation.

### 2026-05-23: Two roles — Administrator and Customer
**By:** Toru (Architect)
**What:** Role `Administrator`: full access to all endpoints including `GET/POST/PUT/DELETE` on products, categories, inventory, all orders, and reports. Role `Customer`: read products/categories, manage own orders (`GET/POST` on own orders only), read own customer profile (`GET/PUT`). No anonymous access beyond product/category browsing.
**Why:** Matches the project brief exactly. RBAC enforced at the controller level via `[Authorize(Roles = "Administrator")]` and `[Authorize]` attributes. Fine-grained resource ownership (customers seeing only their orders) enforced in service layer by comparing `CustomerId` from JWT `sub` claim.

### 2026-05-23: JWT claims structure
**By:** Toru (Architect)
**What:** JWT payload includes: `sub` (UserId GUID), `email`, `role` (Administrator | Customer), `given_name`, `family_name`, `customer_id` (CustomerId INT — added as custom claim for Customers), `jti` (unique token ID), `iss` (issuer), `aud` (audience), `exp`, `iat`.
**Why:** `customer_id` custom claim avoids a DB round-trip to resolve UserId → CustomerId on every request. `jti` enables token blacklisting if needed. Standard claims (`iss`, `aud`) required for proper JWT validation.

### 2026-05-23: Frontend stores access token in memory (not localStorage)
**By:** Toru (Architect)
**What:** The React app stores the access token in a Zustand auth store (in-memory, not persisted to localStorage or sessionStorage). On page refresh, the app calls `POST /api/v1/auth/refresh` using the HttpOnly cookie to silently re-issue the token.
**Why:** Storing JWTs in localStorage exposes them to XSS. In-memory + HttpOnly cookie refresh is the current best practice for SPA authentication security.

---

### 2026-05-23: Azure resource naming convention
**By:** Toru (Architect)
**What:** Pattern `{abbreviation}-outdoors-{environment}` (e.g., `app-outdoors-api-dev`, `kv-outdoors-prod`). Storage accounts use no hyphens due to Azure limits: `stoutdoorsdev`, `stoutdoorsprod`. Two resource groups: `rg-outdoors-dev` and `rg-outdoors-prod`.
**Why:** Predictable names reduce lookup friction. Abbreviations follow Microsoft CAF (Cloud Adoption Framework) conventions: `app`, `asp`, `sql`, `sqldb`, `st`, `func`, `kv`, `appi`, `law`.

### 2026-05-23: App Service B1 for dev, P2v3 for prod
**By:** Toru (Architect)
**What:** Web API hosted on Azure App Service. Dev uses B1 (Basic) plan. Production uses P2v3 (Premium v3) for auto-scale readiness. No VNet or private endpoints for PoC.
**Why:** App Service over Container Apps for simplicity — no container registry or orchestration overhead for a PoC. P2v3 chosen over P1v3 for baseline production memory headroom.

### 2026-05-23: Azure SQL Basic/S0 for dev, S2 for prod
**By:** Toru (Architect)
**What:** Azure SQL Database on the DTU model. Dev: Basic (5 DTU, sufficient for seeding and testing). Prod: S2 (50 DTU) for concurrent user load. Both use geo-redundant backup.
**Why:** DTU model is simpler to reason about for a PoC. S2 prod gives 50 DTU and 250 GB — enough for the domain scope.

### 2026-05-23: Azure Functions on Consumption plan
**By:** Toru (Architect)
**What:** Functions App uses the Consumption (Y1) hosting plan for both dev and prod.
**Why:** The three functions (discount timer, payment queue, stock queue) are infrequent. Consumption cost is near-zero for PoC traffic. Scale-to-zero is acceptable for auxiliary background tasks.

### 2026-05-23: Key Vault with managed identity access
**By:** Toru (Architect)
**What:** All secrets (connection strings, JWT signing key, Storage SAS token) stored in Azure Key Vault. App Service and Functions App access Key Vault via system-assigned managed identity with `Key Vault Secrets User` role. No connection strings in app settings or code.
**Why:** Eliminates secret rotation risk and credential exposure in CI/CD logs. Managed identity removes the need to manage service principal credentials.

### 2026-05-23: Storage Account containers and access levels
**By:** Toru (Architect)
**What:** Three Blob containers: `product-images` (Blob-level public read — product images are public), `order-receipts` (private — SAS URL generated by API on request), `exports` (private — SAS URL generated by API on download).
**Why:** Product images need public CDN-friendly URLs. Receipts and exports contain PII and must not be publicly listable.

---

### 2026-05-23: Three GitHub Actions workflows — backend, frontend, functions
**By:** Toru (Architect)
**What:** Three workflow files: `.github/workflows/backend.yml` (triggers on `src/OutdoorsShop.Api/**` and `src/OutdoorsShop.Core/**` and `src/OutdoorsShop.Infrastructure/**` changes), `.github/workflows/frontend.yml` (triggers on `frontend/**`), `.github/workflows/functions.yml` (triggers on `src/OutdoorsShop.Functions/**`). Each workflow runs on push to `dev` (deploy to dev environment) and push to `main` (deploy to prod environment).
**Why:** Scoped path filters prevent a frontend change from triggering a backend deployment. Independent pipelines allow the teams to move at different speeds.

### 2026-05-23: Secrets in GitHub Environments (dev / prod) using OIDC federated credentials
**By:** Toru (Architect)
**What:** GitHub Environments `dev` and `prod` are configured. Azure authentication uses OIDC (`azure/login@v2` with `client-id`, `tenant-id`, `subscription-id`) — no service principal secret stored. Application secrets (connection strings, JWT key) are stored in GitHub Environment secrets and injected as Azure App Service application settings at deploy time (not checked into code).
**Why:** OIDC eliminates the need to rotate service principal secrets. GitHub Environments provide environment-scoped secret isolation (prod secrets are not accessible from dev pipeline runs).

### 2026-05-23: Branch protection rules
**By:** Toru (Architect)
**What:** `main` branch: required status checks (backend build+test, frontend build+test), required PR with 1 approval, no direct push. `dev` branch: required status checks (build+test pass), direct push allowed for squad agents (to unblock flow during PoC). Squad feature branches follow `squad/{slug}` → PR → `dev`.
**Why:** `main` is protected to prevent broken production deployments. `dev` protection ensures tests pass before integration but allows direct push for squad agility during PoC phase.

### 2026-05-23: CI pipeline steps — build, test, migrate, deploy
**By:** Toru (Architect)
**What:** Backend pipeline steps: (1) `dotnet restore`, (2) `dotnet build --no-restore`, (3) `dotnet test --no-build --collect:"XPlat Code Coverage"`, (4) `dotnet publish -c Release -o ./publish`, (5) `dotnet ef database update` (dev only — prod migrations are script-based review), (6) `az webapp deploy`. Functions pipeline mirrors steps 1–4 then `az functionapp deployment source config-zip`.
**Why:** EF migrations run automatically on dev for speed. On prod, a reviewed SQL script is applied manually (or via deployment slot swap) to prevent accidental schema destruction. Code coverage collected for visibility without blocking the pipeline.

---

### 2026-05-23: Data Model Approved

**By:** Toru (Architect)
**What:** Reviewed submitted C# data model (Product, ProductCategory, Customer, SalesOrder, SalesOrderDetail, ProductInventory). Approved with required additions: `Product.IsActive` (bool, soft-delete), `ProductCategory.IsActive` (bool, soft-delete), `Customer.UserId` (string, FK to AspNetUsers.Id for role-based auth), `SalesOrder.Status` (string, order management/tracking), `SalesOrder.PaymentStatus` (string, payment simulation feature), `ProductInventory.LastUpdated` (DateTime, StockUpdateQueue Function audit trail), `ProductInventory.ReorderThreshold` (int, stock alert threshold). `SalesOrderDetail` approved as-is. Cart is client-side only — no Cart or CartItem entities in the DB (ADR-004 confirmed). C# class names to be aligned to established DB table names via EF `ToTable()` configuration (SalesOrder→Orders, SalesOrderDetail→OrderItems, ProductCategory→Categories, ProductInventory→Inventory).
**Why:** Additions are the strict minimum required to support all 8 confirmed feature requirements. No over-engineering. Client-side cart eliminates two entities with zero feature loss.

---

### 2026-05-23: Adventure Works-inspired schema — six core tables
**By:** Toru (Architect)
**What:** Schema has six application tables: `Categories`, `Products`, `Customers`, `Orders`, `OrderItems`, `Inventory`. Identity (users/roles) uses ASP.NET Core Identity default tables (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, etc.). `Customers.UserId` is a FK to `AspNetUsers.Id`.
**Why:** Separates authentication identity (ASP.NET Identity) from business domain (Customers). A Customer record is created upon registration and linked to the Identity user. This pattern aligns with Adventure Works's Person/Customer split.

### 2026-05-23: EF Core with repository pattern via OutdoorsShop.Infrastructure
**By:** Toru (Architect)
**What:** EF Core 10 is the ORM. DbContext lives in `OutdoorsShop.Infrastructure`. Repositories implement interfaces defined in `OutdoorsShop.Core`. No raw ADO.NET in application code. Migrations managed via EF Core CLI (`dotnet ef migrations`).
**Why:** Repository pattern decouples controllers from the ORM, making unit testing with in-memory providers or mocks straightforward. Cinnamon owns implementation; interfaces in Core enforce the contract.

### 2026-05-23: Soft deletes via IsActive flag on Products and Categories
**By:** Toru (Architect)
**What:** `Products.IsActive` and `Categories.IsActive` columns implement logical delete. Physical DELETE is not used for these tables. All queries filter `WHERE IsActive = 1` by default via EF Core global query filters.
**Why:** Product removal from catalog must not break historical order records that reference the product. Soft delete preserves referential integrity.

### 2026-05-23: Inventory table ReorderThreshold for stock alerts
**By:** Toru (Architect)
**What:** `Inventory.ReorderThreshold` is an INT column. When the `StockUpdateQueue` function reduces quantity, it checks if `Quantity <= ReorderThreshold` and can raise an alert (logged to App Insights for PoC; extensible to email later).
**Why:** Business requirement: inventory tracking. Threshold makes the stock-update function actionable without hard-coded logic.

---

### 2026-05-23: Monorepo folder structure adopted
**By:** Toru (Architect)
**What:** Single Git repository with three top-level source areas: `src/` (.NET projects), `frontend/` (React + TypeScript), and `infra/` (Bicep IaC). A shared `OutdoorsShop.sln` ties all .NET projects together. Azure Functions live in `src/OutdoorsShop.Functions/` as an isolated-process project.
**Why:** Keeps the full stack version-aligned in one repo. Simplifies cross-concern refactors and CI/CD. Avoids submodule complexity for a single-team PoC.

### 2026-05-23: .NET project layering (Api / Core / Infrastructure / Functions / Tests)
**By:** Toru (Architect)
**What:** Five .NET projects: `OutdoorsShop.Api` (controllers, middleware, startup), `OutdoorsShop.Core` (domain entities, interfaces — no dependencies), `OutdoorsShop.Infrastructure` (EF Core, repositories, storage clients), `OutdoorsShop.Functions` (Azure Functions isolated), `OutdoorsShop.Tests` (xUnit).
**Why:** Classic onion/clean separation. Core has zero external dependencies, making unit testing trivial. Infrastructure is the only layer that touches EF Core and Azure SDKs.

### 2026-05-23: React app scaffolded with Vite + TypeScript
**By:** Toru (Architect)
**What:** React frontend lives in `frontend/` and is scaffolded with `npm create vite@latest -- --template react-ts`. Build output goes to `frontend/dist/`.
**Why:** Vite is the current standard React toolchain. Fast HMR for development, clean static output for deployment.

---

### 2026-05-23: EF Core InitialCreate Migration Applied

**By:** Cinnamon (Backend Developer)
**What:** Created EF Core initial migration (InitialCreate) targeting OutdoorsShopDB on Azure SQL (azure-sql-pampa.database.windows.net). Migration files in src/OutdoorsShop.Infrastructure/Data/Migrations/. Connection string stored in .NET User Secrets for the Api project — never committed. Tables defined: Products, Categories, Customers, Orders, OrderItems, Inventory + ASP.NET Core Identity tables (AspNetUsers, AspNetRoles, AspNetUserRoles, etc.).
**Why:** Database schema must be applied before any API endpoints can be tested end-to-end.
**Status:** Migration CREATED and committed. `database update` BLOCKED — ShopAdmin user lacks CREATE TABLE permission on OutdoorsShopDB. Needs `ALTER ROLE db_ddladmin ADD MEMBER ShopAdmin;` (or `db_owner`) executed by the Azure SQL server admin before schema can be applied.
**Fix required:** Connect to azure-sql-pampa.database.windows.net as server admin and run:
```sql
USE OutdoorsShopDB;
ALTER ROLE db_ddladmin ADD MEMBER ShopAdmin;
-- or for full ownership:
ALTER ROLE db_owner ADD MEMBER ShopAdmin;
```
Then re-run: `dotnet ef database update --project src/OutdoorsShop.Infrastructure --startup-project src/OutdoorsShop.Api`
**Also fixed:** AppDbContext.OnModelCreating — added explicit HasKey() for ProductCategory.CategoryID, SalesOrder.OrderID, SalesOrderDetail.OrderDetailID (non-conventional PK names not auto-detected by EF Core convention).

---

### 2026-05-23: Database Schema Applied to Azure SQL

**By:** Jorgito (confirmed) / Cinnamon (migration)
**What:** dotnet ef database update completed successfully (exit code 0). All tables created on OutdoorsShopDB at azure-sql-pampa.database.windows.net: Products, Categories, Customers, Orders, OrderItems, Inventory + full ASP.NET Core Identity schema (AspNetUsers, AspNetRoles, AspNetUserClaims, AspNetUserTokens, AspNetRoleClaims, AspNetUserRoles). ShopAdmin granted db_owner role by Jorgito via Azure portal.
**Why:** Database schema must exist before API endpoints can be tested end-to-end. Milestone: backend + database are fully connected.

---

### 2026-05-23: Products and Categories API Implemented

**By:** Cinnamon (Backend Developer)
**What:** Full CRUD for Products and Categories. GET endpoints are public (AllowAnonymous). POST/PUT/DELETE require Administrator role. Soft-delete pattern (IsActive=false) for both entities. ProductRepository now eagerly loads Category on all queries. Creating a product automatically creates a ProductInventory record (qty=0, threshold=5). Categories seeded: Camping, Trekking, Cycling, Climbing. Global query filters exclude inactive records transparently.
**Why:** First API endpoints — unblocks Malta's catalog page and Creta's integration tests.

---

## Governance

- All meaningful architectural changes require Toru's approval
- Document architectural decisions here via the inbox drop-box pattern
- Keep history focused on work, decisions focused on direction


---

# Merged from inbox: cinnamon-azure-functions.md

# Decision: Azure Functions — Queue Message Contracts and Architecture

**Date:** 2026-05-23T19:36:12.645-03:00  
**Author:** Cinnamon (Backend Dev)  
**Status:** Accepted

---

## Context

Three Azure Functions were implemented in `src/OutdoorsShop.Functions` (isolated worker, .NET 10). Key architecture choices were made regarding queue message shapes, entity ID types, and DI pattern.

---

## Decisions

### 1. Queue message `orderId` and `productId` are `int`, not Guid

The task spec suggested Guid, but the actual domain entities (`SalesOrder.OrderID`, `ProductInventory.ProductID`) use `int` PKs. Queue messages were designed to match the domain to avoid unnecessary mapping.

### 2. Payment confirmation queue name: `payment-confirmations`

The original stub used `payment-results`. Changed to `payment-confirmations` per task specification. Connection string key: `AzureWebJobsStorage`.

### 3. `paymentStatus` string field in PaymentConfirmationMessage, not enum

The queue message carries `paymentStatus: "Success|Failed|Pending"` as a plain string. These values intentionally differ from the internal `PaymentStatus` enum ("Confirmed" not "Success") — the function maps them explicitly, keeping the external contract decoupled from internal enum names.

### 4. `StockUpdateLog.ProductId` is `int`

Consistent with `ProductInventory.ProductID` (int). The `Id` PK of `StockUpdateLog` is `Guid` (new audit entity, no FK constraints).

### 5. Inject `AppDbContext` directly into functions (not repositories)

For Azure Functions, direct `AppDbContext` injection is simpler and avoids unnecessary indirection for background tasks. Repositories remain available for use from the API project.

### 6. Season boundary: UTC month only

Season detection uses `DateTime.UtcNow.Month`. No timezone conversion. This matches the function's UTC timer schedule (`0 0 2 * * *`).

---

## Queue Message Contracts

### `payment-confirmations`
```json
{
  "orderId": 42,
  "paymentReference": "REF-ABC-123",
  "paymentStatus": "Success",
  "amount": 199.99,
  "processedAt": "2026-05-23T22:36:12Z"
}
```
- `paymentStatus`: `"Success"` | `"Failed"` | `"Pending"`

### `stock-updates`
```json
{
  "productId": 7,
  "quantityDelta": 50,
  "reason": "Restock",
  "notes": "Supplier shipment PO-9901",
  "updatedAt": "2026-05-23T22:36:12Z"
}
```
- `quantityDelta`: positive (restock/return), negative (sale/adjustment)
- `reason`: `"Restock"` | `"Sale"` | `"Adjustment"` | `"Return"`

---

## Schema Changes

| Migration | Field |
|---|---|
| `AddProductDiscountMultiplier` | `Product.DiscountMultiplier decimal(5,4) DEFAULT 1.0` |
| `AddOrderPaymentFields` | `SalesOrder.PaymentReference nvarchar(max) NULL`, `SalesOrder.PaidAt datetimeoffset NULL` |
| `AddStockUpdateLog` | New `StockUpdateLogs` table |


---

# Merged from inbox: cinnamon-remaining-endpoints.md

# Cinnamon backend decisions inbox

- **Timestamp:** 2026-05-23T14:02:03.844-03:00
- **Scope:** Customers, Orders, Inventory, and Reports endpoints for the .NET 10 Web API.

## Proposed team-relevant decisions

1. **Protected ownership rules live in services, not controllers.**
   - `CustomersController` and `OrdersController` pass JWT context into services.
   - `CustomerService` and `OrderService` decide whether a customer can read or mutate a resource.
   - This keeps authorization-by-ownership reusable if Functions, jobs, or future endpoints need the same rule.

2. **Paged responses are now the standard for admin list endpoints.**
   - `PagedResult<T>` was introduced in `src/OutdoorsShop.Core/DTOs/Common/PagedResult.cs`.
   - Customers, Orders, and Inventory list endpoints now share `pageNumber` / `pageSize` semantics.
   - Recommend frontend and tests treat list payloads as `{ items, pageNumber, pageSize, totalCount, totalPages }`.

3. **Order creation is server-validated against current catalog price and stock.**
   - The API accepts `UnitPrice`, but `OrderService` rejects mismatches against the active product price.
   - Inventory decrement and order persistence happen inside one EF Core transaction.
   - This should remain the contract until a dedicated payment/cart service exists.

4. **Report exports should keep data shaping in services and file rendering in API.**
   - Services return report row DTOs.
   - `ReportsController` owns CSV/Excel formatting with `CsvHelper` and `ClosedXML`.
   - This avoids pushing transport-specific libraries into repository code.


---

# Merged from inbox: creta-integration-test-fix.md

# Decision: EF Core Provider Strategy for Integration Tests

**Date:** 2026-05-23T20:10:00.511-03:00  
**Author:** Creta (Test Engineer)  
**Status:** Applied

---

## Context

Integration tests in `tests/OutdoorsShop.Api.Tests/Integration/` use `WebApplicationFactory<Program>` to spin up the full ASP.NET Core pipeline. The tests need a real database (for schema validation, Identity, foreign keys) but cannot require a live SQL Server instance in CI.

The original attempt (`UseInMemoryDatabase`) was blocked by an EF Core 10.0 multi-provider conflict: "Only a single database provider can be registered in a service provider."

## Root Cause (Fully Diagnosed)

`IWebHostBuilder.ConfigureServices` callbacks in `WebApplicationFactory` execute **before** `Program.cs` services are registered. This means:

1. Our `RemoveAll<DbContextOptions<AppDbContext>>()` had nothing to remove.
2. After our callbacks finished, `Program.cs`'s `AddDatabase(...)` registered `UseSqlServer` (and its `IDatabaseProvider` via `AddEntityFrameworkSqlServer()`).
3. Our SQLite provider was already registered, so both `IDatabaseProvider` implementations coexisted.
4. EF Core 10.0 detects this and throws on first `DbContext` access.

## Decision

**Use SQLite in-memory** (`DataSource=:memory:`) as the EF Core provider for integration tests.

**Implementation pattern chosen:** Guard `AddDatabase` against empty connection strings, then blank the connection string in the test factory via `builder.UseSetting` before `Program.cs` reads it. This prevents `UseSqlServer` from ever registering, making SQLite the only provider.

### Why SQLite over InMemory

- SQLite enforces foreign keys, NULL constraints, and unique indexes — closer to SQL Server behavior.
- SQLite works with `EnsureCreated()` which builds the schema from the EF model directly.
- InMemory would have the same conflict issue, and it doesn't enforce relational constraints.

### Why NOT Migrate()

SQLite does not support all SQL Server migration syntax (e.g., some `ALTER TABLE` patterns, computed columns). `EnsureCreated()` builds the schema directly from the current EF model — correct for test environments.

### Connection lifetime

The `SqliteConnection` is a field on `TestWebAppFactory` and is opened in `ConfigureWebHost`. It must stay open for the factory's lifetime — SQLite in-memory databases are destroyed when their connection closes. It is disposed in `Dispose(bool)`.

### Seeding timing

Seeding is performed in a `CreateHost` override, after `base.CreateHost(builder)` returns. This ensures the full application service provider is built (including Identity's `UserManager<ApplicationUser>` and `RoleManager<IdentityRole>`) before seeding runs.

## Scope

- Applies to `TestWebAppFactory` in `tests/OutdoorsShop.Api.Tests/`
- `ServiceCollectionExtensions.AddDatabase` modified to guard against empty connection string (minimal production code change; does not affect production behavior when connection string is provided)

## Related Fix

`ServiceCollectionExtensions.AddJwtAuthentication`: added `options.MapInboundClaims = false`. The JWT middleware's default claim mapping was converting the `sub` claim to `ClaimTypes.NameIdentifier`, which caused `AuthController.Me()` and `Logout()` to silently receive `null` when calling `User.FindFirstValue(JwtRegisteredClaimNames.Sub)`. This was a pre-existing production bug exposed when integration tests began running.

## Outcome

- API tests: 58 passed, 0 skipped, 0 failed (45 unit + 13 integration)
- Function tests: 16 passed, 4 skipped (seasonal date-injection gap — separate issue)
- Total: 74 passed, 4 skipped, 0 failed


---

# Merged from inbox: creta-test-strategy.md

# Test Architecture Decisions
**Author:** Creta (Test Engineer)
**Date:** 2026-05-23T19:44:50.257-03:00
**Status:** Proposed

---

## Summary

This document captures the test architecture decisions made when writing the comprehensive xUnit test suite for OutdoorsShop.

---

## 1. Unit vs Integration Split

**Decision:** Primary coverage via Moq-based controller unit tests; integration tests defined but skipped pending infrastructure fix.

**Rationale:**
- The codebase uses service interfaces (`ICustomerService`, `IOrderService`, `IInventoryService`) and repository interfaces (`IProductRepository`, etc.) consistently. This makes Moq-based controller unit tests accurate and maintainable — tests survive refactoring of service implementations.
- For `ProductsController` and `CategoriesController`, repository interfaces are injected directly, so repository-level mocks were used.
- Integration tests via `WebApplicationFactory<Program>` are the right long-term approach for HTTP-level contract validation but have a current blocker (see §4 below).

---

## 2. Mocking Approach

**Decision:** Mock at the service/repository boundary, not at the DbContext boundary.

**Why it's correct:**
- The domain service interfaces (`ICustomerService`, `IOrderService`, `IInventoryService`) return `OperationResult<T>` — this allows testing all result paths (success, forbidden, not found, bad request) without understanding EF Core internals.
- Controllers that use services directly (CustomersController, OrdersController, InventoryController) have complete behavioral coverage because all paths through `ToActionResult()` are testable with mocked service results.

**What is NOT covered by unit tests:**
- Authorization attribute enforcement (`[Authorize(Roles = "Administrator")]`) — this is middleware-enforced and only testable via integration tests.
- The `Forbidden()` result from `[Authorize(Roles = ...)]` on `InventoryController` (the entire controller is `[Authorize(Roles = "Administrator")]`) — unit tests call the controller method directly, bypassing auth middleware.

---

## 3. Azure Functions Testing

**Decision:** Use `AppDbContext` with `UseInMemoryDatabase` directly. No HTTP layer.

**Rationale:** Azure Functions (`SeasonalDiscountFunction`, `PaymentConfirmationFunction`, `StockUpdateFunction`) accept `AppDbContext` as a constructor parameter. InMemory DbContext is the cleanest isolation mechanism without requiring a real database.

**Known gap — SeasonalDiscountFunction date injection:**
`SeasonalDiscountFunction.Run()` reads `DateTime.UtcNow` directly. Tests for specific seasons (winter → 0.85, summer → 0.90) are **skipped** because they are non-deterministic across calendar months.

**Remediation:**
1. Introduce `IDateTimeProvider` interface (or `TimeProvider` from .NET 8) in `OutdoorsShop.Core`.
2. Inject it into `SeasonalDiscountFunction` instead of using `DateTime.UtcNow`.
3. Unskip `Execute_AppliesWinterDiscount_*`, `Execute_AppliesSummerDiscount_*`, `Execute_ResetsDiscount_InSpring`, `Execute_ResetsDiscount_InAutumn` with a mock `TimeProvider`.

The `Run_SetsCorrectMultipliersForCurrentSeason` and `Run_OnlyAffectsActiveProducts_InactiveProductNotModified` tests DO run without date injection.

---

## 4. Integration Test Infrastructure Gap — EF Core 10.0 Multi-Provider Conflict

**Status:** BLOCKED — all integration tests currently skipped.

**Error:** `System.InvalidOperationException: Services for database providers 'Microsoft.EntityFrameworkCore.SqlServer', 'Microsoft.EntityFrameworkCore.InMemory' have been registered in the service provider. Only a single database provider can be registered in a service provider.`

**Root cause:** EF Core 8.0+ validates that only one database provider is registered in the application service provider. When `WebApplicationFactory` replaces `DbContextOptions<AppDbContext>` (SqlServer → InMemory), the validation detects both provider registrations and throws during service provider construction.

**Remediation options (in order of preference):**

1. **Use SQLite in-memory** (single provider replacement): Replace `UseInMemoryDatabase` with `UseSqlite("DataSource=:memory:")` and a shared `SqliteConnection`. SQLite is a real relational DB, so schema creation works via `EnsureCreated()`.

   ```csharp
   services.AddSingleton<DbConnection>(_ =>
   {
       var conn = new SqliteConnection("DataSource=:memory:");
       conn.Open();
       return conn;
   });
   services.AddDbContext<AppDbContext>((sp, opt) =>
       opt.UseSqlite(sp.GetRequiredService<DbConnection>()));
   ```

2. **Suppress DI validation in test host**: `builder.UseDefaultServiceProvider(opt => { opt.ValidateOnBuild = false; })`. This is the quickest fix but masks DI misconfiguration.

3. **Isolate test DI from app DI**: Create a separate test-only `WebApplicationFactory` that builds the host without `Program.cs`'s `AddDatabase()` call — use `builder.ConfigureAppConfiguration` to suppress the connection string and `AddDbContext` with InMemory only.

---

## 5. Coverage Gaps Not Addressed

| Area | Reason |
|------|--------|
| `ReportsController` | Not in scope for this sprint. Requires blob storage mock. |
| Auth token refresh (integration) | Skipped with integration tests. Unit test covers `Refresh_Returns401_WhenNoCookiePresent`. |
| Attribute-only auth (`[Authorize(Roles = ...)]` on `InventoryController`) | Integration test needed — unit tests bypass middleware. |
| `CustomerService`, `OrderService`, `InventoryService` | Service unit tests not in scope; covered by controller tests. |
| FluentValidation rules | Not tested; would require integration or manual model state manipulation. |

---

## 6. Test Naming Convention

All tests follow `MethodOrScenario_ExpectedBehavior_WhenCondition`. Example:
- `GetById_Returns403_WhenCustomerAccessesOtherProfile`
- `Run_ClampsToZero_WhenDeltaExceedsStock`


---

# Merged from inbox: malta-frontend-theme.md

# Malta frontend theme decisions

- **Date:** 2026-05-23T19:06:28.812-03:00
- **Author:** Malta

## Decisions

1. The storefront uses Tailwind CSS with a shared oriental palette (`crimson`, `gold`, `jade`, `ink`, `parchment`, `copper`, `mist`) defined in `frontend/tailwind.config.js` so every page can reuse the same visual tokens.
2. Layout and surface styling live in `frontend/src/index.css` through reusable shells (`container-shell`, `ornate-card`, `panel-shell`, `field-input`) instead of ad-hoc page styling, keeping the magical bazaar tone consistent across catalog, checkout, and admin views.
3. Route pages rely on reusable UI building blocks in `frontend/src/components/ui/` and domain components in `frontend/src/components/products/` so customer and admin screens share the same visual language while staying responsive and accessible.
4. Auth and cart flows follow team security decisions: access token remains in memory via `frontend/src/store/authStore.ts`, refresh is cookie-based through `frontend/src/api/client.ts`, and the cart persists only in localStorage via `frontend/src/store/cartStore.ts`.

---


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


---


# ADR: Azure Bicep Infrastructure Templates for Dev Environment

**Date:** 2026-05-23  
**By:** Toru (Architect)  
**Status:** Accepted

## Context

The OutdoorsShop project needs reproducible, version-controlled Azure infrastructure for the dev environment. Manual portal configuration is not acceptable for a PoC that aims to demonstrate engineering best practices. All secrets must never appear in config files, environment variables, or deployment logs.

## Decision

Provision all dev Azure resources via Azure Bicep templates in `infra/`. A single orchestrator (`main.bicep`) calls six modules in dependency order. Sensitive parameters (`sqlAdminPassword`, `jwtSecret`) are `@secure()` and must be passed via CLI flags or CI/CD secret injection — never committed to source.

## Resources provisioned

| Resource | Name | SKU / Tier |
|---|---|---|
| Log Analytics | `law-outdoors-dev` | PerGB2018, 30-day retention |
| Application Insights | `appi-outdoors-dev` | Workspace-based |
| SQL Server | `sql-outdoors-dev` | v12, TLS 1.2, Azure services firewall open |
| SQL Database | `sqldb-outdoors-dev` | Basic (5 DTU), geo-redundant backup |
| Storage Account | `stoutdoorsdev` | Standard_LRS, StorageV2, HTTPS-only |
| App Service Plan | `asp-outdoors-dev` | B1, Linux |
| App Service (API) | `app-outdoors-api-dev` | .NET 10, system-assigned MI |
| Functions Hosting Plan | `asp-outdoors-func-dev` | Y1 (Consumption), Linux |
| Functions App | `func-outdoors-dev` | .NET isolated 10, system-assigned MI |
| Key Vault | `kv-outdoors-dev` | Standard, soft-delete 7 days |

## Secret management approach

- All secrets stored in Key Vault (`kv-outdoors-dev`).
- App Service and Functions access secrets via `@Microsoft.KeyVault(VaultName=...;SecretName=...)` app setting references.
- Managed identities granted `get`/`list` access policies on Key Vault.
- Bicep outputs for connection strings use `@secure()` to prevent logging.

## Deployment order rationale

Key Vault is deployed **last** in `main.bicep`. This is intentional: the Key Vault access policies require the `principalId` from the App Service and Functions managed identities, which are only available after those resources are created. Bicep resolves this via output references, creating an implicit dependency chain. App settings with Key Vault references are valid strings from the moment of App Service creation; Azure resolves them at runtime once Key Vault is live.

## DB migration note (ShopAdmin)

The `ShopAdmin` SQL user requires the `db_ddladmin` role so that EF Core's migration runner can execute `CREATE`/`ALTER`/`DROP` DDL statements. Without this role, `dotnet ef database update` fails. This is a one-time manual step documented in `infra/README.md`.

## Consequences

- Single `az deployment group create` command deploys all dev infrastructure.
- No secrets in source code, app settings, or CI logs.
- Key Vault access policies (not RBAC) are used as specified; can be migrated to RBAC in a future ADR.
- `db_ddladmin` must be granted manually after first SQL deployment (not automated in Bicep to avoid embedding the app user password in the template).


---

# Decision: Use `System.TimeProvider` for Date Abstraction in Azure Functions

**Date:** 2026-05-23T21:00:31.176-03:00  
**Author:** Cinnamon  
**Status:** accepted

---

## Context

`SeasonalDiscountFunction` used `DateTime.UtcNow` directly, making the 4 season-specific tests untestable without running them on specific calendar dates. The tests were marked `[Fact(Skip = "...")]`.

## Decision

Use the .NET 8+ built-in `System.TimeProvider` abstract class as the canonical time abstraction. Do **not** introduce a custom `ITimeProvider` interface.

## Rationale

- `TimeProvider` is a first-party .NET 8+ API, no added dependencies.
- Subclassing for tests (`FakeTimeProvider : TimeProvider`) requires only 3 lines and is self-contained.
- Optional constructor parameter (`TimeProvider? timeProvider = null`, defaulting to `TimeProvider.System`) keeps all existing callers backward-compatible.
- `builder.Services.AddSingleton(TimeProvider.System)` is the DI registration for production.

## Consequences

- All date-dependent function tests are deterministic regardless of when CI runs.
- `DateTime.UtcNow` is banned in Azure Functions code — use `_timeProvider.GetUtcNow().UtcDateTime` instead.
- Pattern applies to any future function that branches on the current date/time.

## Files Changed

- `src/OutdoorsShop.Functions/Functions/SeasonalDiscountFunction.cs`
- `src/OutdoorsShop.Functions/Program.cs`
- `tests/OutdoorsShop.Functions.Tests/Functions/SeasonalDiscountFunctionTests.cs`
