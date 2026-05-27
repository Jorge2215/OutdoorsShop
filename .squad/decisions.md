## 2026-05-27T20:27:02Z — Merged from inbox: cinnamon-azure-deploy-readiness.md

# cinnamon-azure-deploy-readiness

Date: 2026-05-27T17:14:25.938-03:00
Owner: Cinnamon

Summary:
- Functions app (func-outdoors-dev) already has a CI/CD deploy workflow and is deployable.
- Backend API (app-outdoors-api-dev) has no deployment workflow; backend CI only builds/tests.

Recommendation:
- Add a deployment step for the API (Azure App Service) before pushing full dev deploy.
  - Use az webapp zip deploy or GitHub Action azure/webapps-deploy@v1 targeting app-outdoors-api-dev.
  - Ensure App Service app settings include: ConnectionStrings:DefaultConnection, AzureStorage:ConnectionString, JwtSettings:Secret (or use Key Vault / managed identity).
  - Run EF Core migrations on startup or add a migration step in pipeline.

Status: pending

## 2026-05-27T20:27:02Z — Merged from inbox: toru-azure-deploy-readiness.md

# Azure deploy readiness

author: "toru"
date: 2026-05-27T17:14:25.938-03:00

Summary

- Frontend CI/CD: wired. .github/workflows/frontend.yml builds and deploys to Azure Static Web Apps (requires AZURE_STATIC_WEB_APPS_API_TOKEN secret).
- Functions CI/CD: wired. .github/workflows/functions.yml builds, publishes, and runs az functionapp deployment on push to dev/main (uses Azure login secrets).
- Backend API: NOT wired for automated deployment. .github/workflows/backend.yml currently runs CI (build/tests) only; API deployment is manual and must be added (run-from-package or az webapp deploy).

Assessment

- The frontend and functions can be deployed now via their workflows (ensure repo secrets exist and are correct).
- The API should not be deployed automatically yet: the workflow lacks a deploy step and we must confirm configuration (CORS AllowedOrigins, connection strings, SQL firewall, secret rotation) before enabling automated deployment.

Recommendation (safest next action)

1. Deploy frontend + functions now (CI/CD already wired). Verify SWA hostname, update API AllowedOrigins and App Service settings accordingly.
2. Hold API automated deploy until we: (a) add a controlled deploy step to backend.yml (run-from-package or az webapp), (b) add required secrets (AZURE_CLIENT_ID/SECRET/TENANT/SUBSCRIPTION or use OIDC), and (c) run a smoke test against the dev App Service.

Action items (short)

- Cinnamon: confirm AZURE_STATIC_WEB_APPS_API_TOKEN and Azure login secrets are present in repo secrets.
- Cinnamon: open a small PR that adds an API deploy job to backend.yml (deploy to app-outdoors-api-dev on push to dev) and include a smoke test that hits /api/health.
- Toru: after SWA is live, update decisions and confirm AllowedOrigins in app-outdoors-api-dev.

Decision

- Proceed with partial deployment: run frontend + functions CI/CD now; postpone automated API deployment until wiring and safety checks are in place.

# Decisions

## 2026-05-25T14:05:01Z — Merged from inbox: cinnamon-soft-delete-fix.md

# Cinnamon inbox — soft-delete admin reads fix

- Date: 2026-05-25T11:05:01.947-03:00
- Owner: Cinnamon
- Area: backend products API

## Decision

Added an `includeInactive` query flag to the public product read endpoints, but only administrators may use it. When `includeInactive=true`, the controller switches to repository queries that call `IgnoreQueryFilters()` so soft-deleted products remain readable for admin review and reactivation.

## Rationale

- The global `HasQueryFilter(p => p.IsActive)` correctly hides inactive products from public catalog reads, but it also made admin soft deletes look like hard deletes.
- Admins need a way to confirm `IsActive=false`, inspect the record, and reactivate it later without exposing inactive products to anonymous users.
- `ProductDto` already includes `IsActive`, so the API contract could support this fix without a DTO change.

## Implementation notes

- `ProductsController.GetAll` and `GetById` now accept `includeInactive=false` by default and return `403` when non-admin callers try to enable it.
- `IProductRepository` / `ProductRepository` now expose `GetAllIncludingInactiveAsync()` and `GetByIdIncludingInactiveAsync(int id)` using `IgnoreQueryFilters()`.
- Default reads are unchanged for anonymous/public clients; soft-deleted products only appear when an administrator explicitly opts in.

## 2026-05-25T14:05:01Z — Merged from inbox: creta-admin-catalog-verdict.md

# Admin Products Catalog Validation Report

**Tested by:** Creta (Test Engineer)  
**Date:** 2026-05-25T11:05:01.947-03:00  
**Target branch:** `dev`  
**Live API:** `https://app-outdoors-api-dev.azurewebsites.net/api/v1`

## Scope reviewed

- Frontend: `frontend/src/pages/admin/AdminProductsPage.tsx`
- Backend: `.copilot-main/src/OutdoorsShop.Api/Controllers/ProductsController.cs`
- DTOs: `.copilot-main/src/OutdoorsShop.Core/DTOs/Products/CreateProductDto.cs`, `.copilot-main/src/OutdoorsShop.Core/DTOs/Products/UpdateProductDto.cs`
- Related root-cause check: `.copilot-main/src/OutdoorsShop.Infrastructure/Repositories/ProductRepository.cs`, `.copilot-main/src/OutdoorsShop.Infrastructure/Data/AppDbContext.cs`, `frontend/src/api/products.api.ts`

## Summary

- Tests run: **21**
- PASS: **20**
- FAIL: **1**
- BLOCKED: **0**
- **Verdict: FAIL**

## Test results

| ID | Name | Expected | Actual | Status |
|---|---|---|---|---|
| AUTH-01 | POST without token | 401 Unauthorized | HTTP 401 | PASS |
| AUTH-02 | PUT without token | 401 Unauthorized | HTTP 401 | PASS |
| AUTH-03 | DELETE without token | 401 Unauthorized | HTTP 401 | PASS |
| AUTH-04 | IMAGE without token | 401 Unauthorized | HTTP 401 | PASS |
| AUTH-05 | POST with Customer token | 403 Forbidden | HTTP 403 | PASS |
| AUTH-06 | PUT with Customer token | 403 Forbidden | HTTP 403 | PASS |
| AUTH-07 | DELETE with Customer token | 403 Forbidden | HTTP 403 | PASS |
| AUTH-08 | IMAGE with Customer token | 403 Forbidden | HTTP 403 | PASS |
| CRUD-01 | Admin creates product | 201 Created + product id | HTTP 201; ProductID=17 | PASS |
| CRUD-02 | Read created product | 200 OK with created name | HTTP 200; Name=Creta QA Product 41b60851 | PASS |
| CRUD-03 | Public list shows active created product | 200 OK and product present while active | HTTP 200; Present=true | PASS |
| CRUD-04 | Admin uploads product image | 200 OK with imageUrl | HTTP 200; ImageUrlPresent=true | PASS |
| CRUD-05 | Admin updates product | 200 OK with changed name and price | HTTP 200; Name=Creta QA Product Updated 41b60851; Price=199.49 | PASS |
| CRUD-06 | Admin deactivates product | 204 No Content | HTTP 204 | PASS |
| CRUD-07 | Deleted product remains soft-deleted record | 200 OK with IsActive=false | HTTP 404; IsActive=undefined | FAIL |
| CRUD-08 | Public list hides deactivated product | 200 OK and product absent after delete | HTTP 200; Present=false | PASS |
| VAL-01 | Create missing required fields | 400 Bad Request | HTTP 400 | PASS |
| VAL-02 | Create invalid category | 404 Not Found | HTTP 404 | PASS |
| VAL-03 | Create negative price | 400 Bad Request (flag if accepted) | HTTP 400 | PASS |
| VAL-04 | Update non-existent product | 404 Not Found | HTTP 404 | PASS |
| VAL-05 | Delete non-existent product | 404 Not Found | HTTP 404 | PASS |

## Bugs found

### 1. Blocking: soft-deleted product becomes unreadable and unmanageable

**Observed**

- `DELETE /api/v1/products/17` returned `204 No Content`.
- A follow-up `GET /api/v1/products/17` returned `404` with `{"message":"Product 17 not found."}`.
- The product also disappeared from the public `/products` listing.

**Expected**

- The delete flow is documented and implemented as a soft delete (`IsActive = false`), so the record should remain queryable somewhere in the admin contract, or at minimum be readable in a way that proves the object still exists with `IsActive=false`.

**Impact**

- Admins cannot verify the soft-delete state after deletion.
- Admins cannot review or reactivate deactivated products through the current admin catalog flow.
- The UI exposes an `Active` field, but deleted products vanish from the admin data source, so that field cannot be used to recover a deleted product.

**Code evidence**

- `ProductsController.Delete()` only sets `product.IsActive = false` before saving.
- `AppDbContext` applies `HasQueryFilter(p => p.IsActive)`.
- `ProductRepository.GetByIdAsync()` and `GetAllAsync()` do not use `IgnoreQueryFilters()`, so inactive products are filtered out from normal reads.
- `AdminProductsPage` loads via `productsApi.list()`, and `productsApi.list()` calls the public `/products` endpoint.

**Recommendation**

- Add admin-specific list/get behavior for inactive products, or bypass query filters for admin reads.
- If the intended contract is hard delete semantics for reads, update the API/UI design and documentation to stop calling this a soft delete.

## 2026-05-25T14:05:01Z — Merged from inbox: malta-admin-inactive-fix.md

# Malta inbox — admin inactive products fix

- Date: 2026-05-25T11:05:01.947-03:00
- Owner: Malta
- Area: frontend admin catalog

## Decision

Admin product management should load products with `includeInactive=true` so soft-deleted items remain visible to administrators.

## Rationale

- Soft-deleted products disappearing from the admin catalog blocks review and recovery.
- Admins need a clear visual distinction between active and inactive records without making the table feel alarming.
- Reactivation can reuse the existing update flow by sending the full product payload with `isActive: true`.

## Implementation notes

- Extended `productsApi.list()` query options to append `includeInactive=true`.
- Updated `AdminProductsPage` to request inactive products, render active/inactive badges, mute inactive rows, and swap Delete for Reactivate when `isActive` is false.

## 2026-05-24T23:02:36Z — Merged from inbox: creta-image-upload-final-verdict.md

# Image Upload Final Verdict

**Tested by:** Creta (Test Engineer)
**Date:** 2026-05-24T16:52:12.609-03:00
**Endpoint:** `POST /api/v1/products/{id}/image`
**API:** `https://app-outdoors-api-dev.azurewebsites.net`
**Storage:** `stoutdoorsdev` / `product-images` container

---

## Results

```
Tests run: 22 / 22
PASS: 22
FAIL: 0
BLOCKED: 0

Overall: PASS
```

---

## Test Summary

| # | Test | Status | HTTP | Notes |
|---|------|--------|------|-------|
| PRE-01 | Health check | ✅ PASS | 200 | `{"status":"ok"}` |
| PRE-02 | Product #1 exists | ✅ PASS | 200 | Alpine Base Camp Tent 4P |
| PRE-03 | Existing imageUrl accessible | ✅ PASS | 200 | image/jpeg from Unsplash |
| PRE-04 | Endpoint deployed | ✅ PASS | 401 | Auth gate fires — endpoint live |
| H-01 | Upload valid JPG | ✅ PASS | 200 | imageUrl returned, blob confirmed in stoutdoorsdev |
| H-02 | Upload valid PNG | ✅ PASS | 200 | imageUrl ends `.png` |
| H-03 | Upload valid WEBP | ✅ PASS | 200 | imageUrl ends `.webp` |
| H-04 | Blob URL publicly accessible | ✅ PASS | 200 | No auth required, image/jpeg |
| H-05 | Product imageUrl updated in DB | ✅ PASS | 200 | GET /products/1 returns new blob URL |
| A-01 | No token → 401 | ✅ PASS | 401 | Auth guard fires before upload logic |
| A-02 | Customer token → 403 | ✅ PASS | 403 | Role guard correctly rejects Customer |
| A-03 | Admin token → 200 | ✅ PASS | 200 | Administrator JWT accepted |
| V-01 | .exe file → 400 | ✅ PASS | 400 | `"Invalid file type. Allowed types: jpg, jpeg, png, gif, webp."` |
| V-02 | .pdf file → 400 | ✅ PASS | 400 | `"Invalid file type. Allowed types: jpg, jpeg, png, gif, webp."` |
| V-03 | 6MB file → 400 | ✅ PASS | 400 | `"File size exceeds the 5 MB limit."` |
| V-04 | Empty file → 400 | ✅ PASS | 400 | `"No file uploaded."` |
| V-05 | No file field → 400 | ✅ PASS | 400 | ASP.NET model validation: `"The file field is required."` |
| E-01 | Non-existent product (99999) → 404 | ✅ PASS | 404 | `"Product 99999 not found."` |
| E-02 | Re-upload same product | ✅ PASS | 200 | New URL returned, DB updated to new blob |
| E-03 | Old blob cleanup | ✅ PASS* | — | Minimum condition met; new blob exists, DB updated |
| E-04 | Special chars filename | ✅ PASS | 200 | UUID blob name assigned — no 500, no filename leakage |
| C-01 | CORS OPTIONS preflight | ✅ PASS | 204 | Correct headers for SWA origin |

> *E-03: Old blob is **not deleted** on re-upload (previous blob remains accessible in storage). This is a non-blocking storage hygiene issue — the DB always points to the correct latest blob. See advisory below.

---

## Advisory Finding (Non-Blocking)

**E-03: No old blob cleanup on re-upload**

- **Observed:** After uploading a second image for product #2, the original blob URL (`products/2/bc65da28-....jpg`) continues to return HTTP 200 from Azure Blob Storage.
- **Impact:** Orphaned blobs accumulate over time in `product-images`, incrementally increasing storage costs.
- **Recommendation:** In the upload handler, read the current `product.ImageUrl` before overwriting. If it points to a `stoutdoorsdev.blob.core.windows.net/product-images/` URL, delete that blob before saving the new one.
- **Severity:** Low — no data integrity issue, no security issue, no functional regression.

---

## Confirmed Working

- ✅ Auth guard: 401 without token, 403 for Customer, 200 for Administrator
- ✅ File type validation: jpg, jpeg, png, gif, webp accepted; exe, pdf rejected with clear error
- ✅ File size validation: 5 MB limit enforced with clear error message
- ✅ Empty file and missing file field both return 400 with distinct messages
- ✅ Product existence check: 404 with `"Product {id} not found."` for unknown IDs
- ✅ Re-upload: new URL returned, DB updated — no stale data
- ✅ Filename normalization: special characters handled via UUID naming scheme
- ✅ Blob public access: returned URLs are immediately accessible without auth
- ✅ DB consistency: `GET /products/{id}` always reflects latest uploaded imageUrl
- ✅ CORS: SWA origin preflight returns correct headers including `Allow-Credentials: true`


## 2026-05-24T23:02:36Z — Merged from inbox: creta-admin-catalog-tests.md

# Test Scenarios — Admin Product Catalog Management

### 2026-05-24: Test scenarios — Admin Product Catalog
**By:** Creta (Test Engineer)
**Date:** 2026-05-24T19:59:32.340-03:00
**What:** Test coverage plan for Admin Product Catalog sprint
**Status:** Draft — for Cinnamon (backend) and Malta (frontend) to implement before ship

---

## Context & Assumptions

- Backend: .NET 10 Web API, JWT auth (`Administrator` / `Customer` roles)
- Admin credentials (dev): `admin@outdoorsshop.dev` / `Admin@123456`
- Auth shape: `POST /api/v1/auth/login` → `{ accessToken }` — `role: Administrator` claim in JWT
- Customer token → 403, no token → 401, expired token → 401 (not 403 — known pitfall; always mint fresh tokens before auth test runs)
- Frontend: React + TypeScript, SWA, role-based route guards
- Test stack: xUnit + WebApplicationFactory (backend), Vitest + React Testing Library (frontend), Playwright (E2E)

---

## Area 1 — Role-Based Access Control (RBAC) 🔴 Critical

These must be green before any other area is considered shippable.

| ID | Scenario | Method | Expected | Notes |
|----|----------|--------|----------|-------|
| RBAC-01 | No token → all admin product endpoints return 401 | Backend integration | 401 on every admin route | Cover: POST /products, PUT /products/{id}, DELETE /products/{id}, POST /products/{id}/image, POST /categories, PUT /categories/{id}, DELETE /categories/{id}, PUT /products/{id}/stock |
| RBAC-02 | Customer JWT → all admin endpoints return 403 | Backend integration | 403 on every admin route | Mint a fresh Customer token — stale token returns 401 instead of 403 |
| RBAC-03 | Admin JWT → all admin endpoints reachable (no 401/403) | Backend integration | 2xx or 404 (if resource missing), never 401/403 | |
| RBAC-04 | Admin can create a product | Backend integration | 201 Created | Happy path auth gate confirmation |
| RBAC-05 | Admin can read all products (including inactive) | Backend integration | 200 + full list | Customer/public list may hide inactive — admin list must show all |
| RBAC-06 | Admin can update a product | Backend integration | 200 OK | |
| RBAC-07 | Admin can delete a product | Backend integration | 204 No Content | |
| RBAC-08 | Admin UI route `/admin/products` accessible with Admin session | Frontend / E2E | Route renders, no redirect | |
| RBAC-09 | `/admin/products` with Customer session → redirected to home/403 page | Frontend / E2E | Redirect or 403 UI | |
| RBAC-10 | `/admin/products` with no session → redirected to login | Frontend / E2E | Redirect to `/login` | |

---

## Area 2 — Product CRUD (Backend) 🟠 High

### 2a — Happy Path

| ID | Scenario | Expected |
|----|----------|----------|
| CRUD-01 | POST /products with valid payload → 201 + product object with ID | Product persisted, retrievable via GET |
| CRUD-02 | GET /products/{id} returns created product | 200 + correct fields |
| CRUD-03 | PUT /products/{id} with updated name/price/description → 200 | Updated fields returned and persisted |
| CRUD-04 | DELETE /products/{id} → 204 | Product no longer returned in GET list (or soft-deleted per implementation) |
| CRUD-05 | GET /products after create → product appears in list | List length +1 |

### 2b — Validation

| ID | Scenario | Expected | Notes |
|----|----------|----------|-------|
| VAL-01 | POST with missing `name` → 400 | Validation error mentioning `name` | |
| VAL-02 | POST with missing `price` → 400 | Validation error mentioning `price` | |
| VAL-03 | POST with missing `categoryId` → 400 | Validation error | |
| VAL-04 | POST with `price = 0` → 400 | "Price must be greater than zero" | |
| VAL-05 | POST with `price = -9.99` → 400 | "Price must be greater than zero" | |
| VAL-06 | POST with duplicate SKU → 409 Conflict | Clear duplicate-SKU message | Only relevant if SKU is a unique field; flag for Cinnamon to confirm |
| VAL-07 | POST with `name` exceeding max length → 400 | Validation error | Confirm max length with Cinnamon (suggest 200 chars) |
| VAL-08 | POST with `description` exceeding max length → 400 | Validation error | |
| VAL-09 | PUT with invalid `price` (negative) → 400 | Same price validation as create | |
| VAL-10 | POST with empty body `{}` → 400 | All required-field errors returned | |

### 2c — Not Found

| ID | Scenario | Expected |
|----|----------|----------|
| NF-01 | PUT /products/99999 → 404 | `{"message":"Product 99999 not found."}` — consistent with image upload pattern |
| NF-02 | DELETE /products/99999 → 404 | Same 404 shape |
| NF-03 | GET /products/99999 → 404 | Same 404 shape |

---

## Area 3 — Category Management 🟠 High

| ID | Scenario | Expected | Notes |
|----|----------|----------|-------|
| CAT-01 | POST /products with valid `categoryId` → 201 | Product created with category | |
| CAT-02 | POST /products with non-existent `categoryId` → 400 or 422 | Rejection with clear message | FK violation must be caught at app layer, not leak as 500 |
| CAT-03 | POST /products with `categoryId = null` → 400 | Validation error | Only if category is required; confirm with Cinnamon |
| CAT-04 | POST /categories with valid payload → 201 | Category created | |
| CAT-05 | POST /categories with duplicate name → 409 | Conflict message | |
| CAT-06 | DELETE /categories/{id} that has products → 409 or 400 | Rejection — no orphaned products | ⚠️ Flag for Cinnamon: what's the cascade policy? |
| CAT-07 | DELETE /categories/{id} with no products → 204 | Category deleted cleanly | |

---

## Area 4 — Image Upload 🟡 Medium

> Core image upload (22/22 tests) already verified and passing. See `creta-image-upload-final-verdict.md`. These are incremental scenarios specific to the catalog management context.

| ID | Scenario | Expected | Notes |
|----|----------|----------|-------|
| IMG-01 | Admin creates a product then immediately uploads an image → imageUrl persisted | 200, GET /products/{id} returns new imageUrl | Full CRUD + image round-trip |
| IMG-02 | GET /products/{id} after image upload returns correct public blob URL | 200, imageUrl is `https://stoutdoorsdev.blob.core.windows.net/product-images/...` | Regression guard for image upload integration |
| IMG-03 | Upload image for soft-deleted product → 404 | If soft-delete is implemented, image upload on inactive product must return 404 | Flag for Cinnamon |
| IMG-04 | Valid GIF upload succeeds | 200, imageUrl ends `.gif` | All 5 allowed types should appear in at least one catalog integration test |

---

## Area 5 — Inventory / Stock 🟠 High

| ID | Scenario | Expected | Notes |
|----|----------|----------|-------|
| INV-01 | PUT /products/{id}/stock with `quantity = 0` → 200 | Stock set to zero, product still exists (out-of-stock state) | |
| INV-02 | PUT /products/{id}/stock with `quantity = -1` → 400 | "Stock cannot be negative" | |
| INV-03 | PUT /products/{id}/stock with `quantity = 100` → 200 | Stock updated, reflected in GET /products/{id} | |
| INV-04 | GET /products/{id} after stock update → correct `stockQuantity` field | 200 + updated value | Confirm field name with Cinnamon |
| INV-05 | GET /products (public/catalog) with stock = 0 → product appears but marked out-of-stock | Depends on business rule — flag for Cinnamon/Toru | Should out-of-stock products still appear in customer-facing catalog? |
| INV-06 | Stock update via admin does not require re-uploading image or touching other fields | 200, `imageUrl` and other fields unchanged | Partial update guard |

---

## Area 6 — Frontend (React) 🟡 Medium

### 6a — Role-Based UI (Vitest + React Testing Library)

| ID | Scenario | Expected |
|----|----------|----------|
| FE-01 | Nav renders "Admin" link when user has `Administrator` role in auth store | Admin nav item visible |
| FE-02 | Nav does NOT render "Admin" link for `Customer` role | Admin nav item absent |
| FE-03 | Nav does NOT render "Admin" link when unauthenticated | Admin nav item absent |
| FE-04 | `<AdminRoute>` guard redirects Customer to home/403 | React Router redirect fires |
| FE-05 | `<AdminRoute>` guard redirects unauthenticated user to `/login` | React Router redirect fires |

### 6b — Product Form Validation (Vitest + React Testing Library)

| ID | Scenario | Expected |
|----|----------|----------|
| FE-06 | Submit create-product form with empty `name` → inline error shown | Error message next to name field, form not submitted |
| FE-07 | Submit with `price = 0` → inline error | "Price must be greater than zero" |
| FE-08 | Submit with `price = -5` → inline error | Price validation message |
| FE-09 | Submit with no category selected → inline error | Category required message |
| FE-10 | Submit with all valid fields → API called once with correct payload | `fetch`/`axios` mock called once, no duplicate submissions |
| FE-11 | Submit button disabled while API call in-flight | Button disabled / loading state active |

### 6c — Optimistic UI & Error Recovery

| ID | Scenario | Expected |
|----|----------|----------|
| FE-12 | Delete product → item removed from list immediately (optimistic) | List updates before API response |
| FE-13 | Delete product → API returns 500 → item restored in list | Rollback to pre-delete state, error toast shown |
| FE-14 | Delete product → API returns 404 → item removed from list, no double-error | Clean UX for already-deleted item |
| FE-15 | Update product → API returns 409 (duplicate SKU) → form shows server-side error | Error from API surface as form-level validation message |

---

## Area 7 — Edge Cases & Risk Flags 🔴 Must Discuss Before Sprint

These are not standard test cases — they are architectural risk items Cinnamon, Malta, and Toru should explicitly decide before Creta writes tests for them.

### EC-01 — Concurrent Edits to the Same Product

**Risk:** Admin A and Admin B both open Product #5 in the edit form. Admin A saves first. Admin B saves a second later — Admin A's changes are silently overwritten.

**Options:**
1. Last-write-wins (simplest, acceptable for now)
2. Optimistic concurrency with EF Core `RowVersion` / `ConcurrencyToken` → 409 on conflict
3. Pessimistic locking (not recommended for REST)

**Test shape if option 2:** GET product → capture `rowVersion` → PUT with stale `rowVersion` → expect 409.
**Action needed:** Toru/Cinnamon to decide and document in decisions.md before sprint.

---

### EC-02 — Deleting a Product in Active Orders

**Risk:** Admin deletes Product #7. Customer's open order contains Product #7. What happens to:
- The order line item?
- The order total?
- The customer's order history?

**Options:**
1. Hard delete blocked if product has order references → 409 with clear message
2. Soft delete (set `IsActive = false`) — product hidden from catalog, order history preserved
3. Cascade delete — destroys order data (❌ never acceptable)

**Test shape for option 1:** Create order with product, attempt DELETE /products/{id} → expect 409.
**Test shape for option 2:** Delete → product hidden from GET /products list but GET /products/{id} returns 200 (or 200 + `"isActive": false`).
**Action needed:** Toru to decide cascade/soft-delete policy. This is a data-integrity risk.

---

### EC-03 — Search/Filter Performance on Large Catalog

**Risk:** Admin product list with 10,000+ products — pagination, filtering, and search may have N+1 queries or full-table scans.

**Test approach:**
- Load test: seed 1,000+ products, measure `GET /admin/products?page=1&pageSize=50&search=tent` latency
- Query: Cinnamon to confirm EF Core queries use `.Where()` before `.ToListAsync()` (not filter in memory)
- Index: Confirm `Products.Name` has a DB index for search
- Pagination: `GET /admin/products?page=1000` with empty results returns 200 + empty array (not 404)

**Action needed:** Cinnamon to confirm pagination is in scope for sprint. Creta will write a seeding fixture and latency assertion if so.

---

### EC-04 — Token Expiry During Long Admin Sessions

**Risk:** Admin opens the product form, goes to lunch (90 min), comes back and submits → `accessToken` expired → API returns 401 → frontend shows cryptic error.

**Test shape:** Mock token expiry in frontend test → confirm UX shows "Session expired, please log in again" + redirect to login.
**Action needed:** Malta to confirm token refresh / expiry handling is in scope.

---

## Scenarios Count Summary

| Area | Total Scenarios | Priority |
|------|----------------|----------|
| RBAC | 10 | 🔴 Critical |
| Product CRUD | 18 | 🟠 High |
| Category Management | 7 | 🟠 High |
| Image Upload (incremental) | 4 | 🟡 Medium |
| Inventory / Stock | 6 | 🟠 High |
| Frontend | 15 | 🟡 Medium |
| Edge Cases / Risk Flags | 4 items (architectural) | 🔴 Must discuss |
| **Total** | **60 + 4 risk items** | |

---

## Recommended Execution Order

1. **RBAC-01..10** — auth gates first; nothing else is meaningful if auth is wrong
2. **NF-01..03 + CRUD-01..05** — basic CRUD health check
3. **VAL-01..10** — validation completeness
4. **CAT-01..07** — category FK integrity
5. **INV-01..06** — stock rules
6. **IMG-01..04** — catalog-level image integration (image upload core already covered)
7. **FE-01..15** — frontend in parallel with backend work
8. **EC-01..04** — pending architectural decisions

---

## Prerequisites / Dependencies

- Admin user seed already in place (`admin@outdoorsshop.dev` / `Admin@123456`) ✅
- Image upload endpoint already deployed and tested ✅
- **Cinnamon to confirm:** SKU field existence and uniqueness constraint, max field lengths, soft-delete vs. hard-delete policy, category cascade policy, pagination scope
- **Toru to confirm:** Concurrent edit strategy (EC-01), product-in-order delete policy (EC-02)
- **Malta to confirm:** Token expiry UX handling (EC-04), optimistic delete rollback implementation (FE-12..14)


## 2026-05-24T23:02:36Z — Merged from inbox: cinnamon-live-bug-fix.md

# Cinnamon Decision — 2026-05-24T19:19:19.460-03:00 — Live Bug Fix: CORS origin mismatch

**By:** Cinnamon (Backend Dev)
**Commit:** `68c2509`
**Status:** Fixed & verified (env var updated live; code committed to dev)

## Bugs Reported

1. **Account Registration not working** — browser blocked with "Failed to fetch"
2. **Products catalog shows "Catalog unavailable Failed to fetch"** — same CORS block

## Root Cause (both bugs share the same root)

**CORS AllowedOrigins pointed to the wrong SWA URL.**

- The App Service env var `AllowedOrigins__0` and `appsettings.json` had:
  `https://brave-beach-044d7c610.6.azurestaticapps.net`
- The **actual live SWA** (deployed, serving the React app) is:
  `https://wonderful-plant-0a1ca5f0f.7.azurestaticapps.net`
- `brave-beach` returns Azure SWA 404 (no content deployed there)
- `wonderful-plant` is the SWA in `rg-outdoors-dev` (`app-outdoorsweb-swa`) with the built React app

When the browser at `wonderful-plant` called the API, the request included
`Origin: https://wonderful-plant-0a1ca5f0f.7.azurestaticapps.net`.
ASP.NET Core CORS rejected it (no `Access-Control-Allow-Origin` in response).
The browser blocked both the products GET and the register POST → "Failed to fetch".

## Investigation steps confirmed

| Check | Result |
|---|---|
| `GET /api/health` | 200 ✓ — API is running |
| `GET /api/v1/products` | 200 with data ✓ — API works |
| CORS preflight from `brave-beach` | 204 + ACAO header ✓ |
| CORS preflight from `wonderful-plant` | 204 but **NO ACAO header** ✗ |
| SWA `brave-beach` | Returns Azure 404 — no app deployed |
| SWA `wonderful-plant` | Returns React app — JS bundle has correct API URL |

## Fix Applied

1. Updated `appsettings.json` `AllowedOrigins[2]` from `brave-beach` to `wonderful-plant`
2. Updated App Service env var `AllowedOrigins__0` directly (immediate effect, no redeploy)
3. Committed as `68c2509` and pushed to `dev`

## Verification

- CORS preflight `OPTIONS /api/v1/products` from `wonderful-plant` → 204 + `Access-Control-Allow-Origin: https://wonderful-plant-0a1ca5f0f.7.azurestaticapps.net` ✓
- CORS preflight `OPTIONS /api/v1/auth/register` from `wonderful-plant` → 204 + ACAO + `Access-Control-Allow-Methods: POST` ✓

## Notes

- The `brave-beach` URL was mistakenly set as the CORS origin during the previous CORS fix session (cinnamon-5). At the time, `brave-beach` may have been a newly-created SWA candidate, but the active SWA in the resource group remained `wonderful-plant`.
- The BlobStorageService, startup crash hypothesis, and platform CORS were all ruled out: the API was healthy throughout.
- `dev → main` still pending for this fix commit.


## 2026-05-24T23:02:36Z — Merged from inbox: cinnamon-dev-main-sync.md

# Decision: dev → main sync

**Date:** 2026-05-24T18:57:46.744-03:00
**Author:** Cinnamon

## Summary

All accumulated work from the `dev` branch was merged into `main` via a no-fast-forward merge commit (`56f6dec`). One merge conflict in `Program.cs` was resolved by keeping the full dev version (admin seed block with logger and role seeding improvements).

## Commits synced (dev → main)

| Commit | Description |
|--------|-------------|
| `22e971e` | Fix auth refresh cookie handling (SameSite=None + JWT given_name fix) |
| `cada3b2` | fix(cors): add SWA origin and harden CORS config reading |
| `9076954` | Enable Swagger UI in all environments |
| `943db2e` | Record Swagger rollout notes |
| `708af75` | seed: add admin user seed on startup |
| `526b8fa` | feat(api): add product image upload via Azure Blob Storage |
| `164a8e7` | feat(frontend): add admin product image upload UI |
| `b319577` | squad: log Swagger production rollout (cinnamon-6) |
| `7621926` | docs: admin seed history + decisions inbox entry |
| `5e7f93b` | Merge decisions inbox (orchestration logs, session log) |
| `883e73d` | Merge decisions inbox (team update: admin seed live) |

## Merge commit

`56f6dec` — "Merge dev into main: auth fixes, CORS, Swagger, blob image upload, admin seed"

## Conflict resolved

`src/OutdoorsShop.Api/Program.cs` — dev version kept (full admin seed block with `ILogger<Program>`, role seeding log, and admin user creation).

## Notes

- `main` worktree lives at `.copilot-main`; `git checkout main` from the repo root is blocked because of this linked worktree. Always run main-branch operations from `.copilot-main`.
- Dev is ahead of `origin/dev` by 2 local commits; a follow-up `git push origin dev` is recommended to keep origin in sync.


## 2026-05-24T16:52:12.609-03:00 — Merged from inbox: cinnamon-admin-seed.md

# Admin User Seed — 2026-05-24T16:52:12.609-03:00

**By:** Cinnamon (Backend Dev)
**Commit:** `708af75`
**Deployed to:** `app-outdoors-api-dev` (rg-outdoors-dev)

## Decision

Added an idempotent admin user seed to `Program.cs` that runs at startup after role seeding.

## Credentials (dev environment only)

| Field    | Value                      |
|----------|----------------------------|
| Email    | admin@outdoorsshop.dev     |
| Password | Admin@123456               |
| Role     | Administrator              |
| Name     | Admin User                 |

## What was added

- `UserManager<ApplicationUser>` creates the admin user if not already present
- Admin user assigned to `Administrator` role
- Corresponding `Customer` record created with `Name = "Admin User"` so the JWT `given_name` claim resolves correctly
- Full idempotency: `FindByEmailAsync` check prevents duplicates on every restart

## Smoke test results

- `POST /api/v1/auth/login` with above credentials → **200 OK**
- JWT claims verified:
  - `given_name: "Admin User"` ✓
  - `role: Administrator` ✓
  - `customer_id: 13` ✓

## Notes

- Password is stored hashed in ASP.NET Core Identity; plain text is in `Program.cs` as a dev-only seed (acceptable for dev environment per task brief)
- Seed is safe to run on every app startup; does nothing if admin already exists

---

## 2026-05-24T16:52:12.609-03:00 — Merged from inbox: cinnamon-blob-storage-upload.md

# Cinnamon Decision — 2026-05-24T16:52:12.609-03:00 — Product image upload via Azure Blob Storage

**By:** Cinnamon (Backend Dev)
**Status:** Implemented & deployed

## Decision

Implemented `POST /api/v1/products/{id}/image` endpoint (Administrator only) that uploads product images to Azure Blob Storage (`stoutdoorsdev`, container `product-images`) and persists the public URL to `Product.ImageUrl` in the database.

## What was already in place

- `Product.ImageUrl` — already a nullable `string?` on the entity; no EF migration needed
- `Azure.Storage.Blobs` NuGet — already referenced in `OutdoorsShop.Infrastructure`
- `IBlobStorageService` / `BlobStorageService` — already existed with `UploadAsync`, `DeleteAsync`, `GetSasUrlAsync`
- `AzureStorage:ConnectionString` config placeholder — already in `appsettings.json`
- `AddBlobStorage` DI extension — already wired in `ServiceCollectionExtensions` and called in `Program.cs`

## What was added

1. **`IBlobStorageService.UploadProductImageAsync(Stream, string, string, int) → string`** — new method on the interface (no ASP.NET dependency, keeps Core clean)
2. **`BlobStorageService.UploadProductImageAsync`** — creates `product-images` container with `PublicAccessType.Blob`; blob name: `products/{productId}/{guid}{ext}`
3. **`ProductsController.UploadImage`** — `POST /api/v1/products/{id}/image`, `[Authorize(Roles="Administrator")]`, `[Consumes("multipart/form-data")]`; validates MIME type (jpg/jpeg/png/gif/webp) and size (≤ 5 MB); updates `Product.ImageUrl` and saves to DB; returns `{ imageUrl }`.
4. **Test fixes** — added `IBlobStorageService` mock to `ProductsControllerTests` ctor; added `UploadProductImageAsync` mock to `TestWebAppFactory`

## Azure setup

- Container `product-images` created in `stoutdoorsdev` with `--public-access blob`
- Real connection string injected as `AzureStorage__ConnectionString` App Service setting (not committed to source)

## Auth behavior (verified live)

| Request | Result |
|---|---|
| No token | 401 |
| Customer JWT | 403 |
| Administrator JWT | 200 + `{ imageUrl }` |

## Commit

`526b8fa` — `feat(api): add product image upload via Azure Blob Storage`

---

## 2026-05-24T16:52:12.609-03:00 — Merged from inbox: creta-image-upload-tests.md

# Creta Finding — Image Upload Tests (2026-05-24T16:52:12.609-03:00)

**By:** Creta (Test Engineer)
**Date:** 2026-05-24T16:52:12.609-03:00
**Status:** Findings — for team awareness

---

## Finding 1: No default Administrator user is seeded

`Program.cs` seeds the `Administrator` and `Customer` roles on startup, but does NOT create any default administrator user account. Any test or flow that requires `[Authorize(Roles = "Administrator")]` needs a pre-created admin user.

**Recommendation for Cinnamon/Toru:** Add a dev-only admin seed user (email: `admin@dev.local`, password from Key Vault) to `Program.cs` startup under `if (app.Environment.IsDevelopment())`. This unblocks integration tests and manual QA without exposing credentials in production.

---

## Finding 2: CORS middleware handles preflight for unregistered routes ✅

`OPTIONS /api/v1/products/1/image` from SWA origin returns **204** with all correct CORS headers even though the route doesn't exist yet. This is expected ASP.NET Core behavior — CORS middleware runs before routing and responds to preflight regardless of whether the downstream endpoint exists.

**No action needed.** This confirms the SWA origin is correctly configured in `AllowedOrigins`.

---

## Finding 3: Image upload endpoint NOT yet deployed

As of 2026-05-24T16:52:12.609-03:00, `POST /api/v1/products/{id}/image` returns 404. `ProductsController` has no upload action. `IBlobStorageService` is registered and ready.

**Blocked tests:** 17 functional tests (H-01..05, A-01..03, V-01..05, E-01..04) all pending Cinnamon's implementation.

---

## Finding 4: Old blob cleanup is a critical test

When re-uploading an image for the same product, there is a risk of blob proliferation if the old blob is not deleted. This must be explicitly tested (E-03). The `IBlobStorageService.DeleteAsync` method exists — Cinnamon's implementation must call it with the old `product.ImageUrl` blob name before writing the new one.

**Recommended:** Parse the old `imageUrl` to extract the blob name before overwriting.

---

## Finding 5: Returned blob URL must be publicly anonymous

If `BlobStorageService.UploadAsync` creates the container with `PublicAccessType.None` (current implementation), the returned URL will not be publicly accessible without a SAS token. For product images shown to anonymous shoppers, the container access level should be `PublicAccessType.Blob` (individual blobs readable, container listing blocked).

**Action for Cinnamon:** Either change the `CreateIfNotExistsAsync` call to `PublicAccessType.Blob` for the `product-images` container, or always return a SAS URL and store the SAS-less base URL in the DB.

**This is a potential defect if not addressed.** H-04 (verify public URL accessibility) will catch this at test time.

---

## 2026-05-24T16:52:12.609-03:00 — Merged from inbox: creta-image-upload-verdict.md

# Image Upload Test Verdict — POST /api/v1/products/{id}/image

**Tested by:** Creta (Test Engineer)
**Date:** 2026-05-24T16:52:12.609-03:00
**Endpoint:** `POST /api/v1/products/{id}/image`
**API Base:** `https://app-outdoors-api-dev.azurewebsites.net`

---

## Verdict Summary

| | Value |
|---|---|
| Tests in plan | 22 (17 pending + 4 PRE + 1 C-01 from Run 1) |
| Tests run this session | 3 |
| **PASS** | **3** |
| **FAIL** | **0** |
| **BLOCKED** | **14** |
| Overall | ⚠️ **CONDITIONAL PASS** |

---

## Passing Tests

| Test | Description | Actual Result |
|------|-------------|---------------|
| A-01 | No token → 401 | ✅ HTTP 401 Unauthorized |
| A-02 | Customer JWT → 403 | ✅ HTTP 403 Forbidden |
| C-01 | CORS OPTIONS from SWA origin | ✅ HTTP 204, all CORS headers correct |

**Good news:**
- Endpoint is deployed and in Swagger spec.
- Auth guards (`[Authorize(Roles = "Administrator")]`) are correctly enforced.
- CORS preflight from `https://brave-beach-044d7c610.6.azurestaticapps.net` works correctly.
- The endpoint architecture is sound — 401 and 403 fire before any business logic.

---

## Blocked Tests (14)

**Root cause: No administrator user exists in the database.**

`Program.cs` seeds the `Administrator` and `Customer` roles at startup, but **no admin user account is created**. Without an Administrator JWT, all requests return 401/403 before reaching file validation, product lookup, or blob upload logic.

7 credential combinations attempted at `POST /api/v1/auth/login` — all returned 401.

| Blocked Test | Description |
|---|---|
| H-01 | Upload valid JPG as Administrator |
| H-02 | Upload valid PNG as Administrator |
| H-03 | Upload valid WEBP as Administrator |
| H-04 | Returned URL is publicly accessible |
| H-05 | GET /products/1 reflects new imageUrl |
| A-03 | Administrator token → 200 |
| V-01 | `.exe` file → 400 |
| V-02 | `.pdf` file → 400 |
| V-03 | 6MB file → 400 |
| V-04 | Empty file (0 bytes) → 400 |
| V-05 | No file field → 400 |
| E-01 | Non-existent product 99999 → 404 |
| E-02 | Re-upload same product |
| E-03 | Old blob cleanup after re-upload |
| E-04 | Filename with special characters |

---

## Action Required to Unblock

**Option A (fastest) — DB role escalation:**

```sql
-- Get the Administrator role ID:
SELECT Id FROM AspNetRoles WHERE Name = 'Administrator';

-- Register a test user via API, then get their UserId:
SELECT Id FROM AspNetUsers WHERE Email = 'admin-creta@test.com';

-- Assign Administrator role:
INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES ('<userId>', '<adminRoleId>');
```

Then provide `admin-creta@test.com` / `<password>` to Creta.

**Option B — Program.cs startup seeding:**

Add a default admin account seed to `Program.cs` (email + known password), gated by environment (`IsDevelopment()`). This removes the DB-access dependency for test runs.

**Option C — `/api/v1/admin/seed-test-user` endpoint (dev-only):**

Add a dev-only endpoint that creates a test admin user on demand. Gate with `[ApiExplorerSettings(IgnoreApi = !isDevelopment)]`.

---

## Risk Assessment

| Risk | Severity | Notes |
|------|----------|-------|
| File validation not tested | High | V-01..V-05: exe, pdf, >5MB, empty, no-file not verified |
| Blob naming / public URL not verified | High | H-04: returned URL accessibility unknown |
| Old blob cleanup not verified | Medium | E-03: could cause blob storage bloat on re-uploads |
| Product 99999 → 404 not verified | Low | E-01: likely works based on standard controller pattern |

---

## Observation: Token TTL

Access tokens appeared to expire in approximately 90 minutes (not 15 min as previously documented). The `exp` claim on the token issued was ~90 min after creation. This may have been changed by Cinnamon. Worth verifying the `JwtSettings:AccessTokenExpirationMinutes` app setting.

---

## Next Steps for Creta

1. Receive admin credentials from Jorgito or Cinnamon (via DB escalation or Program.cs seed)
2. Re-run H-01..H-05, A-03, V-01..V-05, E-01..E-04
3. Update `image-upload-test-plan.md` with full results
4. Issue final verdict (PASS / FAIL)

---

## 2026-05-24T16:52:12.609-03:00 — Merged from inbox: malta-blob-image-upload-ui.md

# Decision: Admin Product Image Upload UI

- **Date:** 2026-05-24T16:52:12.609-03:00
- **Author:** Malta (Frontend Dev)
- **Status:** Implemented

## What

Added admin-only product image upload UI to the `AdminProductsPage` edit modal. Builds on Cinnamon's `POST /api/products/{id}/image` endpoint (multipart form data, Administrator role).

## Changes

| File | Change |
|------|--------|
| `frontend/src/api/client.ts` | Added `fetchWithAuthMultipart` — skips Content-Type so browser sets multipart boundary; retries on 401 with token refresh |
| `frontend/src/api/products.api.ts` | Added `uploadImage(productId, file)` using `fetchWithAuthMultipart`; handles both `string` and `{ imageUrl: string }` response shapes |
| `frontend/src/components/products/ProductImageUpload.tsx` | New component: file picker, MIME + 5 MB validation, object-URL preview, upload with loading state, success/error feedback, onUploaded callback |
| `frontend/src/pages/admin/AdminProductsPage.tsx` | Imports `ProductImageUpload`; renders it inside the edit modal only (create flow has no product ID yet) |

## Why

- Image upload requires an existing product ID → upload section appears only in edit mode, not create.
- `fetchWithAuth` hardcodes `Content-Type: application/json` via `mergeHeaders`; multipart needs a separate helper so the browser can set the boundary automatically.
- Customer-facing `ProductCard` and `ProductDetailPage` already call `getProductImage(imageUrl)` with placeholder fallback — no customer-side changes required.

## Constraints respected

- Upload UI is gated to admin edit modal only.
- JWT Bearer token injected via `fetchWithAuthMultipart`.
- Max 5 MB / accepted types (JPG, PNG, GIF, WEBP) validated on the client before upload.
- `npm run build` passes clean (0 TypeScript errors).

---



## Merged from inbox: cinnamon-swagger-prod.md

### 2026-05-24: Enable Swagger in all environments
**By:** Cinnamon (Backend Dev)
**What:** Removed IsDevelopment() guard from Swagger/SwaggerUI setup in Program.cs
**Why:** Backlog item — API docs should always be accessible at /swagger
**Commit:** 9076954d0d896275d691cbf0f75bd8ee216824c0
**Verified:** /swagger returns 200 in production


Archived: 2026-05-24T035031Z



---

## Merged from inbox: copilot-directive-20260523223344.md


### 2026-05-23T22:33:44-03:00: User directive
**By:** Jorgito (via Copilot)
**What:** Prefer `westus3` over `eastus` for Azure deployments. Going forward, default Azure region = `westus3`.
**Why:** User request â€” Toru's westus3 pivot confirmed as the preferred region; eastus has quota issues and westus3 is preferred.


---

## Merged from inbox: copilot-directive-frontend-swa.md

## 2026-05-27T15:13:32.353-03:00 — Merged from inbox: cinnamon-stock-producer.md

# Cinnamon — 2026-05-27T15:13:32.353-03:00 — Stock producer integration

## Decision
- Keep the API/order services as the source of truth for inventory mutations.
- Emit `stock-updates` queue messages after the database write, using the existing consumer contract (`productId`, `quantityDelta`, `reason`, `notes`, `updatedAt`).
- Make the `StockUpdate` function idempotent by skipping messages whose exact stock movement is already present in `StockUpdateLogs`.

## Why
- `InventoryService.UpdateAsync` already applies admin inventory changes directly, and `OrderService.CreateAsync` already deducts stock directly.
- Blindly adding queue publishing on top of those writes would double-apply stock changes when the existing `StockUpdate` function processed the message.
- Consumer-side dedup keeps current stock math correct while still wiring both admin and order flows into the queue contract.

## Impact
- Admin inventory updates now publish delta-based queue messages without changing the existing absolute-quantity API contract.
- Order creation now publishes queue messages for stock deductions, aggregated per product, while preserving current synchronous stock reservation behavior.
- `StockUpdateLogs` now act as both the movement audit trail and the idempotency key for replayed/duplicated queue messages.


## 2026-05-24T14:24:58.550-03:00 — Merged from inbox: cinnamon-image-urls.md

# Cinnamon Decision — 2026-05-24T14:24:58.550-03:00 — Product image URLs via Unsplash CDN

## Context

All 16 seeded products had `NULL` ImageUrl values in Azure SQL (`OutdoorsShopDB`). The frontend product cards render `<img src={product.imageUrl}>`, so null URLs produced broken image icons.

## Decision

Use **Unsplash free-tier CDN URLs** (`https://images.unsplash.com/photo-{id}?w=400&fit=crop&auto=format`) for all 16 product images rather than uploading owned blobs to `stoutdoorsdev`.

## Why this option

- **Zero cost & zero infra overhead:** Unsplash Source URLs are publicly accessible, no auth required, and served from a global CDN — no blob upload step needed.
- **Variety per product:** A unique, category-relevant photo was picked per product (no duplicates).
- **Reversible:** If the team ever wants owned images in `stoutdoorsdev/product-images`, it's a 16-row UPDATE away.

## Image mapping

| ProductID | Name | Unsplash Photo ID |
|-----------|------|-------------------|
| 1 | Alpine Base Camp Tent 4P | photo-1504280390367-361c6d9f38f4 |
| 2 | TrailRest Mummy Sleeping Bag -10C | photo-1544348817-5f2cf14b88c8 |
| 3 | Summit Lite Backpacking Stove | photo-1563299796-17596ed6b017 |
| 4 | NightTrail 350 Headlamp | photo-1414694762283-acccc27bca85 |
| 5 | Trailblazer Carbon Trekking Poles | photo-1551632811-561732d1e306 |
| 6 | Granite Ridge Hiking Boots Mid | photo-1542401886-65d6c61db217 |
| 7 | HydroFlow 3L Hydration Pack | photo-1538635993-85060e52fd8a |
| 8 | TrailNavigator GPS 500 | photo-1532274402911-5a369e4c4bb5 |
| 9 | VertexMTB Trail Helmet | photo-1541625602330-2277a4c46182 |
| 10 | GripForce Cycling Gloves Full-Finger | photo-1558981403-c5f9899a28bc |
| 11 | LumaBolt 1000 Bike Light Set | photo-1485965120184-e220f721d03e |
| 12 | TrailFix Pro Bike Repair Kit | photo-1571068316344-75bc76f77890 |
| 13 | Ascent Pro Climbing Harness | photo-1522163182402-834f871fd851 |
| 14 | Summit Chalk Bag with Belt | photo-1564760055775-d63b17a55c44 |
| 15 | VértexEdge Rock Climbing Shoes | photo-1574397113396-4369b6dc0dbc |
| 16 | IronLink Carabiner Set 6-pack | photo-1599508704512-2f19efd1e35f |

## Implementation

- Created `scripts/update-image-urls.sql` — runs 16 UPDATE statements and a verification SELECT.
- Updated `scripts/seed-products.sql` — replaced NULL with the Unsplash URLs in the INSERT block so future reseeds are correct.
- Ran the UPDATE script via `sqlcmd` against `azure-sql-pampa.database.windows.net / OutdoorsShopDB`.
- Required opening firewall rule `AllowCinnamonAgent` in resource group `AzureSqlRg` (not `rg-outdoors-dev` — that's where the Azure SQL server lives).

## Verification

`GET https://app-outdoors-api-dev.azurewebsites.net/api/v1/products` returned 16 products, all with non-null `imageUrl`.

## Consequences

- Product images are served from Unsplash CDN — any future Unsplash rate-limiting or takedown would break them.
- For production, consider uploading owned images to `stoutdoorsdev/product-images` and pointing `ImageUrl` there.


## 2026-05-24T14:43:10-03:00 — Merged from inbox: cinnamon-role-seeding-fix.md

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



### 2026-05-23T23:51:06-03:00: User directive
**By:** Jorgito (via Copilot)
**What:** Deploy the React frontend as an Azure Static Web App in West US 3 (westus3)
**Why:** User request — captured for team memory


---

## Merged from inbox: toru-azure-deploy-strategy.md


# Toru â€” Azure deploy strategy

- **Date:** 2026-05-23T21:32:34.383-03:00
- **Decision:** Reuse the existing Azure SQL server `azure-sql-pampa.database.windows.net` and database `OutdoorsShopDB` for the dev deployment.
- **Why:** The database already exists in the subscription, EF Core migrations were already applied, and the live data path is known-good. Reusing it avoided provisioning a second empty SQL server (`sql-outdoors-dev`) and avoided rerunning migrations against a fresh database.
- **Implementation:** Updated `infra/main.bicep` to support `deploySql = false` plus an injected `existingSqlConnectionString`, and set `infra/parameters/dev.bicepparam` to default to the existing server FQDN.
- **Operational note:** The original full-stack deployment in `eastus` failed because the subscription had `0` Microsoft.Web server farm quota there, so the web-facing modules were deployed in `westus3` as a workaround while still pointing to the existing SQL server.
- **Result:** API infrastructure deployed successfully and `https://app-outdoors-api-dev.azurewebsites.net/api/v1/products` returned `200 OK`. The Functions app URL was provisioned but remained unhealthy (`503`) and needs follow-up investigation.


---

## Merged from inbox: toru-cors-fix.md


# Toru Decision â€” 2026-05-24T00:12:30.732-03:00 â€” Resolve dev API/frontend CORS conflict

## Context
The Blob-hosted frontend at `https://stoutdoorswebdev.z1.web.core.windows.net` was loading the shell and then failing during API access. The dev API at `https://app-outdoors-api-dev.azurewebsites.net` already had ASP.NET Core CORS middleware enabled via `UseCors("ReactDevPolicy")`, with origins sourced from `AllowedOrigins__*` application settings.

At the same time, Azure App Service platform CORS was also configured with:
- `https://stoutdoorswebdev.z1.web.core.windows.net`
- `http://localhost:5173`
- `http://localhost:3000`

This created two CORS enforcement layers for the same API.

## Decision
Remove all Azure App Service platform CORS allowed origins for `app-outdoors-api-dev` and keep CORS only in ASP.NET Core middleware.

## Changes applied
- Ran:
  - `az webapp cors remove --name app-outdoors-api-dev --resource-group rg-outdoors-dev --allowed-origins "https://stoutdoorswebdev.z1.web.core.windows.net" "http://localhost:5173" "http://localhost:3000"`
- Verified platform CORS is now empty with:
  - `az webapp cors show --name app-outdoors-api-dev --resource-group rg-outdoors-dev -o json`
  - Result: `"allowedOrigins": []`
- Verified application CORS configuration remains active through App Service settings:
  - `AllowedOrigins__0 = https://stoutdoorswebdev.z1.web.core.windows.net`
  - `AllowedOrigins__1 = http://localhost:5173`
  - `AllowedOrigins__2 = http://localhost:3000`

## Verification
- `GET /api/v1/products` with frontend `Origin` returns:
  - `200 OK`
  - `Access-Control-Allow-Origin: https://stoutdoorswebdev.z1.web.core.windows.net`
  - `Access-Control-Allow-Credentials: true`
  - body: `[]`
- `OPTIONS /api/v1/auth/refresh` with frontend `Origin` returns:
  - `200 OK`
  - single valid ACAO header for the frontend origin
- `POST /api/v1/auth/refresh` without cookie returns:
  - `401` (expected)
  - still includes correct ACAO/ACAC headers
- App log tail showed normal EF Core queries for `Products`; no API crash was observed during validation
- `$web` storage container contains current SPA assets dated `2026-05-24T03:00:33Z` to `03:00:35Z`

## Consequences
- CORS behavior now has one source of truth: the API application
- Future origin changes should be made in app settings / configuration, not Azure platform CORS
- The database is currently empty (`/api/v1/products` returns `[]`), but that is separate from the CORS failure and was not changed here


---

## Merged from inbox: toru-frontend-deploy.md


# Toru Decision â€” Frontend dev deployment

**Date:** 2026-05-23T23:49:31.687-03:00  
**By:** Toru (Architect)  
**Status:** Accepted

## Decision

Deploy the React + TypeScript (Vite) frontend SPA to an Azure Blob Storage static website hosted in a new westus3 storage account: `stoutdoorswebdev`.

**Live URL:** `https://stoutdoorswebdev.z1.web.core.windows.net`

## Why this option

- **Azure Static Web Apps** was evaluated first, but `Microsoft.Web/staticSites` was not available in `westus3`; the available regions were `centralus`, `eastus2`, `westus2`, `westeurope`, and `eastasia`.
- **Azure Blob Storage static website** works with plain `az` CLI, keeps the frontend as a static deploy, and let us stay in `rg-outdoors-dev` and `westus3`.
- **Azure App Service** was rejected as unnecessary runtime overhead for a compiled SPA.

## Implementation notes

- Built `frontend/` with `VITE_API_URL=https://app-outdoors-api-dev.azurewebsites.net`.
- Enabled static website hosting with `index.html` as both the index and 404 document.
- Uploaded the Vite `dist/` output to the `$web` container.
- Updated API CORS on `app-outdoors-api-dev` for `https://stoutdoorswebdev.z1.web.core.windows.net` and restarted the app so the new `AllowedOrigins` values were applied.
- Set the new storage account minimum TLS version to `TLS1_2`.

## Verification

- Frontend root returned `200 OK`.
- Deployed JS bundle references `https://app-outdoors-api-dev.azurewebsites.net`.
- API CORS preflight from the frontend origin returned `200 OK` with `Access-Control-Allow-Origin: https://stoutdoorswebdev.z1.web.core.windows.net` and `Access-Control-Allow-Credentials: true`.

## Follow-up

- Blob static website fallback serves `index.html` for unknown paths but returns HTTP 404 status on deep links; acceptable for dev, but Static Web Apps remains the better production-grade SPA host once region constraints are revisited.


---

## Merged from inbox: toru-swagger-deploy.md


# Toru â€” Swagger deploy outcome

- **Date:** 2026-05-23T22:49:04.177-03:00
- **Decision:** Keep `app-outdoors-api-dev` on the existing blob-backed package flow for now, but update the backing blob with a Linux-targeted publish when deploying API changes.
- **Why:** The App Service currently has `WEBSITE_RUN_FROM_PACKAGE` set to a SAS URL for `stoutdoorsdev/webapp-releases/api-dev.zip`. `az webapp deploy` created a successful OneDeploy record, but it did not update the running site until the backing blob package was replaced and the app was restarted.
- **Implementation:** Published `src/OutdoorsShop.Api/OutdoorsShop.Api.csproj` for Linux (`-c Release -r linux-x64 --self-contained false /p:UseAppHost=false`), uploaded the zip to `webapp-releases/api-dev.zip`, then restarted `app-outdoors-api-dev` in `rg-outdoors-dev`.
- **Result:** `https://app-outdoors-api-dev.azurewebsites.net/openapi/v1.json` returned `200 OK` and `https://app-outdoors-api-dev.azurewebsites.net/swagger` returned `200 OK` after restart.
- **Follow-up:** Align the documented backend deploy path with the actual run-from-package blob strategy, or remove the blob URL app setting so `az webapp deploy` can be the single source of truth.


---

## Merged from inbox: toru-v1-release.md


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
| Strategy | `--no-ff` merge from `dev` â†’ `main` |
| Commits merged | 21 |
| Date | 2026-05-23T21:12:05.666-03:00 |

## What Shipped

### Backend â€” .NET 10 Web API
- **7 controllers:** Auth, Products, Categories, Customers, Orders, Inventory, Reports
- JWT bearer auth (ASP.NET Core Identity), 15-min access token, 7-day refresh in HttpOnly cookie
- EF Core 10 + repository pattern, Azure SQL, CSV/Excel exports
- API versioned at `/api/v1/`

### Azure Functions
- `SeasonalDiscountFunction` â€” timer-triggered daily discount recalculation
- `PaymentConfirmationFunction` â€” queue-triggered payment confirmation processor
- `StockUpdateFunction` â€” queue-triggered inventory adjustment with reorder alerts

### Frontend â€” React + TypeScript
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

- `main` â€” production; requires PR + approval + status checks
- `dev` â€” integration; status checks only
- Feature branches off `dev`, merged via PR

## Benchmark Notes

This PoC was built entirely using GitHub Copilot + Squad (Cinnamon/Backend, Malta/Frontend, Creta/Testing, Toru/Architecture, Scribe/Docs, Ralph/Monitoring). The release demonstrates the full end-to-end capability of the AI-assisted development workflow.

---

## Merged from inbox: creta-auth-fix-verification.md

# Auth Fix Verification — 2026-05-24T14:57:00-03:00

**Tested by:** Creta (Test Engineer)  
**Date:** 2026-05-24T14:57:00-03:00  
**Fix verified:** Cinnamon's role seeding in `Program.cs` (Administrator + Customer)  
**Test email used:** `testuser_20260524145712@test.com`

---

## Quick Auth Smoke Test

| Step | Endpoint | Status | Result |
|------|----------|--------|--------|
| Register | POST /api/v1/auth/register | 200 | ✓ PASS — User created, accessToken returned |
| Login | POST /api/v1/auth/login | 200 | ✓ PASS — accessToken + refreshToken returned |
| Role claim | JWT payload `role` claim | — | ✓ PASS — `Customer` role present |
| Logout | POST /api/v1/auth/logout | 200 | ✓ PASS |

### JWT Claims (register response)
```json
{
  "sub": "b7777b5f-fb0c-4446-9e70-2adaa51922ae",
  "email": "testuser_20260524145712@test.com",
  "customer_id": "2",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "Customer",
  "exp": 1779646334,
  "iss": "https://app-outdoors-api-dev.azurewebsites.net",
  "aud": "OutdoorsShopClient"
}
```
Role seeding confirmed working: `Customer` role is present in the JWT after first registration.

---

## Full 12-Step E2E Journey (Updated)

| Step | Description | HTTP Status | Pass/Fail |
|------|-------------|-------------|-----------|
| 1 | GET /api/health | 200 `{"status":"ok"}` | ✓ PASS |
| 2 | GET /api/v1/products (list all) | 200 — 16 products, 0 null imageUrls | ✓ PASS |
| 3 | GET /api/v1/categories | 200 — 4 categories | ✓ PASS |
| 4 | POST /api/v1/auth/register | 200 — accessToken returned | ✓ PASS (was ✖ 500) |
| 5 | POST /api/v1/auth/login | 200 — accessToken + expiresAt | ✓ PASS (was ✖ blocked) |
| 6 | GET /api/v1/products/{id} | 200 — product detail with imageUrl | ✓ PASS |
| 7 | GET /api/v1/products?category=Camping | 200 | ✓ PASS |
| 8 | GET /api/v1/products?search=tent | 200 | ✓ PASS |
| 9 | GET /api/v1/Orders (with JWT) | 200 — paginated response, 1 order after creation | ✓ PASS (was ✖ blocked) |
| 10 | POST /api/v1/Orders (create order) | 201 — orderID=1, total=149.99 | ✓ PASS (was ✖ blocked) |
| 11 | GET /api/v1/Orders/1 (specific order) | 200 — orderID=1, status=0, total=149.99 | ✓ PASS (was ✖ blocked) |
| 12 | POST /api/v1/auth/logout | 200 | ✓ PASS (was ✖ blocked) |

---

## Summary

- **Previous score:** 6/12
- **New score:** 12/12 ✓
- **Fixed:** Steps 4, 5, 9, 10, 11, 12 (all were blocked by missing `AspNetRoles`)
- **Still failing:** None

### Additional Observations

1. **Register returns a full JWT immediately** — not just a success message. This is good UX (no forced second login after signup).
2. **Orders response is paginated** — `GET /api/v1/Orders` returns `{items, pageNumber, pageSize, totalCount, totalPages}`, not a plain array. The SKILL.md and any frontend code consuming orders must handle the `.items` wrapper.
3. **Role claim format** — The role is encoded under the full URI key `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`, which is the ASP.NET Identity standard. Frontend token parsing should handle both short `role` and full URI key.
4. **Health endpoint now live** — `GET /api/health → 200 {"status":"ok"}`. Previously 404. SKILL.md known issues section needs updating.

### No regressions found
All steps that previously passed (1, 2, 3, 6, 7, 8) continue to pass.

## 2026-05-27T18:30:18Z — Merged from inbox: cinnamon-azure-feature-ideas.md

# Cinnamon — 2026-05-27T15:30:18.727-03:00 — Azure Functions / Queue / Storage feature ideas

## Summary
I inspected existing Functions and patterns (stock-updates queue consumer, payment-confirmations queue consumer, seasonal discount timer, Blob upload patterns and DI for EF Core). Below are 3 practical features that reuse current patterns and need modest changes.

## Proposed features

1) Order receipt generation (recommended)
- Flow: API or PaymentConfirmation publishes a `receipt-requests` queue message after payment success → ReceiptGenerationFunction (QueueTrigger) reads order from DB, renders PDF/HTML receipt, uploads to private blob container `order-receipts`, updates Order. Optionally generate short-lived SAS and send link via email or write to order record.
- Reuses: DbContext injection in Functions, BlobServiceClient patterns, queue contract style already used by `payment-confirmations` and `stock-updates`.
- Effort: small (new queue + new Function + small upload service + wiring in PaymentConfirmation or OrderService).
- Showcases: Queue + Function + Blob Storage together.

2) Inventory CSV export (timer or on-demand)
- Flow: Timer-triggered or HTTP-triggered function queries inventory, writes CSV/Excel to `exports/inventory-YYYYMMDD.csv` in blob storage. Optionally enqueue a `report-ready` message for UI to pick up.
- Reuses: Timer trigger pattern (SeasonalDiscount), DbContext, Blob upload skill.
- Effort: modest (new Function, CSV writer, container creation).

3) Image processing pipeline (thumbnail generation)
- Flow: Use a BlobTrigger Function (on `product-images` container) to create thumbnails and store them under `product-images/thumbs/{productId}/` or update product metadata with thumbnail URL.
- Reuses: Blob patterns and blob container naming; requires adding BlobTrigger to Functions (isolated worker supports blob trigger) and a small image-resize dependency.
- Effort: moderate (adding BlobTrigger experience and image library dependency).

## Recommended next work (easiest high-value)
Implement (1) Order receipt generation. It provides immediate product value (receipts), demonstrates Queue + Function + Blob in a single user story, and reuses existing DI/Blob/Queue patterns. Steps:
- Add `receipt-requests` queue producer in PaymentConfirmationFunction (or OrderService) after marking payment success.
- Add `ReceiptGenerationFunction` (QueueTrigger) that loads order, renders receipt (simple HTML -> store as .html or use wkhtmltopdf if available), uploads to `order-receipts` container with `PublicAccessType.None`, and updates Order with blob path and/or SAS.
- Add unit tests in `tests/OutdoorsShop.Functions.Tests` for the new Function using the established fake TimeProvider and in-memory DB patterns.

## Implementation notes / files to change
- `src/OutdoorsShop.Functions/Functions/ReceiptGenerationFunction.cs` (new)
- `src/OutdoorsShop.Functions/OutdoorsShop.Functions.csproj` (add reference if needed)
- `src/OutdoorsShop.Api` or `Infrastructure` — add `IReceiptQueuePublisher` + implementation, and call after payment success (or within `PaymentConfirmationFunction` flows as queue producer).
- DI: add BlobServiceClient to Functions `Program.cs` (pattern exists in SKILL.md)
- Tests: `tests/OutdoorsShop.Functions.Tests` add ReceiptGenerationFunctionTests

## Questions / follow-ups for team
- PDF vs HTML receipts? (HTML is quickest; PDF conversion needs native tooling or third-party library). I recommend HTML-first with future PDF conversion.
- Do receipts need to be publicly accessible (SAS) or private behind API? I recommend private blobs + SAS per-request.

---

If approved I will implement the ReceiptGenerationFunction and the minimal queue producer wiring in PaymentConfirmation or OrderService.

## 2026-05-27T18:30:18Z — Merged from inbox: toru-azure-feature-ideas.md

---
author: Toru
date: 2026-05-27T15:30:18.727-03:00
---

Subject: Proposed Azure Functions + Queue + Storage features for OutdoorsShop

Context
- Purpose: Provide practical feature ideas that exercise Azure Functions, Storage Queue, and Blob Storage while delivering product value.

Proposals

1) Asynchronous Product Image Pipeline (recommended)
- What: When a user uploads product images, frontend writes the file to the "stoutdoorsdev" Blob container and adds a message to a Storage Queue. A queue-triggered Azure Function pulls the blob, generates thumbnails (multiple sizes), optimizes and writes processed images back to blob storage under /images/{productId}/, then updates product metadata via API or emits an event.
- Business fit: High — improves product display, SEO, page load, and conversion.
- Implementation effort: Medium — requires image processing library (ImageSharp), wiring blob/queue access, and small API metadata update.

2) Order Receipt Generation and Archival
- What: After order completion, API enqueues an order-id message. A queue-triggered Function generates a PDF receipt, stores it in blob storage under /receipts/{orderId}.pdf, and optionally emails link to customer.
- Business fit: Medium-High — reliable receipts and archival; useful for audit and customer support.
- Implementation effort: Low-Medium — PDF library + storage write; optional email adds SMTP/SendGrid config.

3) Supplier Feed Ingestion and Catalog Sync
- What: Suppliers drop CSV/catalog files into a supplier-feeds blob container. A blob-created event (or queue) triggers a Function that parses the feed, enqueues per-product processing messages, and worker Functions update the catalog in Azure SQL.
- Business fit: Medium — automates supplier updates; useful for inventory-heavy sellers.
- Implementation effort: High — parsing, validation, deduplication, SQL transactions, and supplier mapping.

Ranking (by business impact / effort)
- 1) Product Image Pipeline — Best balance: High impact, Medium effort.
- 2) Order Receipt Generation — Quick win: Medium-High impact, Lower effort.
- 3) Supplier Feed Ingestion — High utility for scale, but high effort and integration risk.

Recommendation
- Implement the Asynchronous Product Image Pipeline first. It exercises Azure Functions, Storage Queue, and Blob Storage; delivers visible UX improvements; and is straightforward to scope for a PoC.

Next steps (minimal MVP)
- Provision resources: Storage Account (stoutdoorsdev) containers: images, receipts, supplier-feeds; Storage Queue: processing-queue.
- Create Azure Function (queue trigger) in Functions project: ProcessImageMessage -> fetch blob, generate thumbnails (64, 256, 1024), write back.
- Add small API endpoint or message to update product image URLs in database.
- CI: extend existing Functions project pipeline; add localdev settings for Azure Storage emulator or Azurite.

Assignments
- Implementation: Cinnamon (image processing code, Function wiring)
- Tests: Creta (integration tests around blob writes and metadata updates)
- Infra: Toru (Bicep module and minimal RBAC/storage policy)

Notes
- Use existing "stoutdoorsdev" storage account; reuse naming and CORS rules in infra.
- Keep functions cold-start friendly: small, single-responsibility worker per queue message.

Decision made by Toru on 2026-05-27T15:30:18.727-03:00



