# Malta — History

## Core Context

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

- 2026-05-24T16:52:12.609-03:00: Implemented product image upload UI for admin. Key findings: `fetchWithAuth` always injects `Content-Type: application/json` via `mergeHeaders` — multipart uploads need a separate helper (`fetchWithAuthMultipart`) that skips Content-Type so the browser sets it with the boundary. Backend returns `{ imageUrl: string }` from `POST /api/products/{id}/image`; handled both string and object shapes defensively. Image upload is edit-only (requires an existing product ID); the Create modal keeps the text Image URL field. `ProductImageUpload` component manages file selection, 5 MB validation, MIME type check, preview via `URL.createObjectURL`, upload state, and onUploaded callback to sync the imageUrl back into the parent form. Customer-facing display already used `getProductImage(imageUrl)` with placeholder fallback — no changes needed there.

- 2026-05-24T15:03:44.249-03:00: API contract for register endpoint confirmed: `POST /api/v1/auth/register` accepts `{ name, email, password, confirmPassword }` and returns `{ accessToken, refreshToken, expiresAt }` (JWT auto-login). `GET /api/v1/auth/me` (Bearer) returns `{ userId, email, name, customerID, roles[] }` — role is in the `roles` array, mapped in `mapUserProfile()` from `roles.includes('Administrator')` / `roles.includes('Customer')`. Found the frontend was already correct: `auth.api.ts register()` maps `firstName`/`lastName` → `name` (combined) and passes `confirmPassword`, exactly matching the backend `RegisterDto`. Auto-login after register was already implemented (calls `getMe` then `setTokenAndUser` then navigates to `/products`). The actual 500 error was a backend role-seeding bug (Cinnamon/Program.cs fix). `npm run build` passes clean. No frontend code changes were required.
- 2026-05-23T19:06:28.812-03:00: Rebuilt `frontend/src/` around a Tailwind design system with Cinzel/Lato typography, warm crimson-gold-jade palette, parchment surfaces, and reusable ornate shells in `src/index.css`, `src/components/ui/`, and `src/components/layout/`.
- 2026-05-23T19:06:28.812-03:00: Frontend data flow now uses typed fetch wrappers in `src/api/` with `/api/v1` base routing, automatic 401 refresh via `src/api/client.ts`, in-memory auth in `src/store/authStore.ts`, and persisted cart state in `src/store/cartStore.ts`.
- 2026-05-23T19:06:28.812-03:00: Route-level pages were split with `React.lazy` in `src/App.tsx`, covering public catalog pages, customer checkout/order/profile flows, and admin dashboard/products/inventory/orders workspaces in `src/pages/`.
- 2026-05-24T11:39:21.890-03:00: Created `frontend/.env.production` with `VITE_API_URL=https://app-outdoors-api-dev.azurewebsites.net`. Vite automatically picks this up during `npm run build`, eliminating the need to manually set `$env:VITE_API_URL` before each production build. The file is intentionally committed to the repo (not in .gitignore) since it contains no secrets — just the public API base URL.
- 2026-05-24T15:02:56Z: Noted by Scribe — frontend SWA migration completed by Toru; confirmed `frontend/.env.production` points to `https://app-outdoors-api-dev.azurewebsites.net` for production builds.
- 2026-05-24T17:48:06Z: Scribe — Orders endpoint `GET /api/v1/Orders` returns a paginated payload `{items, pageNumber, pageSize, totalCount, totalPages}`; frontend/client code must unwrap `.items` when consuming order lists. Also: role claim is encoded under the full URI `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`; token parsing should accept both the short `role` claim and the full URI key.

