# Malta — History

## Core Context

## Sprint Context (2026-05-24): Admin Product Catalog frontend sprint is coming. Backend (Products, Categories, Inventory controllers) is fully built with [Authorize(Roles=Administrator)]. Malta owns 3 tasks: AdminProductsPage (M), AdminCategoriesPage (S), categoriesApi mutations (S).

- **Project:** Outdoors Shop
- **Owner:** Jorgito
- **Role:** Frontend Developer
- **Joined:** 2026-05-23
- **Repo:** https://github.com/Jorge2215/OutdoorsShop.git (dev = development, main = production)
- **Stack:** React + TypeScript | Vite | JWT client-side auth | Fetch/OpenAPI client
- **Domain entities:** Products, Categories (Camping/Trekking/Cycling/Climbing), Customers, Orders, Cart
- **My scope:** React + TypeScript, product catalog by category, shopping cart, order flow, payment simulation UI, role-based views (Admin/Customer), JWT client-side auth
- **Team:** Toru (Architect), Cinnamon (Backend), Creta (Tester), Scribe (Docs), Ralph (Monitor)
- **Purpose:** Proof of concept comparing GitHub Copilot + Squad vs traditional development

## Learnings

- 2026-05-25T19:08:32.516-03:00: ProfilePage now owns two independent forms inside the same card: customer details still save through `customersApi.update(...)`, while password changes call `authApi.changePassword(...)` against `PUT /api/v1/users/change-password` with `{ currentPassword, newPassword, confirmNewPassword }`. Keep password validation inline (required fields, minimum 8 characters, confirmation match), show section-specific alerts, and clear the password fields after a successful change.
- 2026-05-25T11:05:01.947-03:00: Admin catalog now requests `productsApi.list({ includeInactive: true })` so soft-deleted products stay visible to administrators. Inactive rows use muted styling plus a danger badge, and reactivation is handled through `productsApi.update(..., isActive: true)` while active products keep the soft-delete action.

### 2026-05-24T16:52:12.609-03:00 — Team update
- Cinnamon delivered admin user seed (admin@outdoorsshop.dev / Admin@123456) — admin login now unblocks all image upload tests for Creta.


- 2026-05-24T16:52:12.609-03:00: Implemented product image upload UI for admin. Key findings: `fetchWithAuth` always injects `Content-Type: application/json` via `mergeHeaders` — multipart uploads need a separate helper (`fetchWithAuthMultipart`) that skips Content-Type so the browser sets it with the boundary. Backend returns `{ imageUrl: string }` from `POST /api/products/{id}/image`; handled both string and object shapes defensively. Image upload is edit-only (requires an existing product ID); the Create modal keeps the text Image URL field. `ProductImageUpload` component manages file selection, 5 MB validation, MIME type check, preview via `URL.createObjectURL`, upload state, and onUploaded callback to sync the imageUrl back into the parent form. Customer-facing display already used `getProductImage(imageUrl)` with placeholder fallback — no changes needed there.

- 2026-05-24T15:03:44.249-03:00: API contract for register endpoint confirmed: `POST /api/v1/auth/register` accepts `{ name, email, password, confirmPassword }` and returns `{ accessToken, refreshToken, expiresAt }` (JWT auto-login). `GET /api/v1/auth/me` (Bearer) returns `{ userId, email, name, customerID, roles[] }` — role is in the `roles` array, mapped in `mapUserProfile()` from `roles.includes('Administrator')` / `roles.includes('Customer')`. Found the frontend was already correct: `auth.api.ts register()` maps `firstName`/`lastName` → `name` (combined) and passes `confirmPassword`, exactly matching the backend `RegisterDto`. Auto-login after register was already implemented (calls `getMe` then `setTokenAndUser` then navigates to `/products`). The actual 500 error was a backend role-seeding bug (Cinnamon/Program.cs fix). `npm run build` passes clean. No frontend code changes were required.
- 2026-05-23T19:06:28.812-03:00: Rebuilt `frontend/src/` around a Tailwind design system with Cinzel/Lato typography, warm crimson-gold-jade palette, parchment surfaces, and reusable ornate shells in `src/index.css`, `src/components/ui/`, and `src/components/layout/`.
- 2026-05-23T19:06:28.812-03:00: Frontend data flow now uses typed fetch wrappers in `src/api/` with `/api/v1` base routing, automatic 401 refresh via `src/api/client.ts`, in-memory auth in `src/store/authStore.ts`, and persisted cart state in `src/store/cartStore.ts`.
- 2026-05-23T19:06:28.812-03:00: Route-level pages were split with `React.lazy` in `src/App.tsx`, covering public catalog pages, customer checkout/order/profile flows, and admin dashboard/products/inventory/orders workspaces in `src/pages/`.
- 2026-05-24T11:39:21.890-03:00: Created `frontend/.env.production` with `VITE_API_URL=https://app-outdoors-api-dev.azurewebsites.net`. Vite automatically picks this up during `npm run build`, eliminating the need to manually set `$env:VITE_API_URL` before each production build. The file is intentionally committed to the repo (not in .gitignore) since it contains no secrets — just the public API base URL.
- 2026-05-24T15:02:56Z: Noted by Scribe — frontend SWA migration completed by Toru; confirmed `frontend/.env.production` points to `https://app-outdoors-api-dev.azurewebsites.net` for production builds.
- 2026-05-24T17:48:06Z: Scribe — Orders endpoint `GET /api/v1/Orders` returns a paginated payload `{items, pageNumber, pageSize, totalCount, totalPages}`; frontend/client code must unwrap `.items` when consuming order lists. Also: role claim is encoded under the full URI `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`; token parsing should accept both the short `role` claim and the full URI key.

\n\n## 2026-05-25T14:05:01Z � Scribe\nMerged malta-admin-inactive-fix.md into decisions.md; frontend updated AdminProductsPage to request includeInactive and show Reactivate action; commit a704695.
