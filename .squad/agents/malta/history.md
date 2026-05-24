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

<!-- Append learnings below -->
- 2026-05-23T19:06:28.812-03:00: Rebuilt `frontend/src/` around a Tailwind design system with Cinzel/Lato typography, warm crimson-gold-jade palette, parchment surfaces, and reusable ornate shells in `src/index.css`, `src/components/ui/`, and `src/components/layout/`.
- 2026-05-23T19:06:28.812-03:00: Frontend data flow now uses typed fetch wrappers in `src/api/` with `/api/v1` base routing, automatic 401 refresh via `src/api/client.ts`, in-memory auth in `src/store/authStore.ts`, and persisted cart state in `src/store/cartStore.ts`.
- 2026-05-23T19:06:28.812-03:00: Route-level pages were split with `React.lazy` in `src/App.tsx`, covering public catalog pages, customer checkout/order/profile flows, and admin dashboard/products/inventory/orders workspaces in `src/pages/`.
- 2026-05-24T11:39:21.890-03:00: Created `frontend/.env.production` with `VITE_API_URL=https://app-outdoors-api-dev.azurewebsites.net`. Vite automatically picks this up during `npm run build`, eliminating the need to manually set `$env:VITE_API_URL` before each production build. The file is intentionally committed to the repo (not in .gitignore) since it contains no secrets — just the public API base URL.
- 2026-05-24T15:02:56Z: Noted by Scribe — frontend SWA migration completed by Toru; confirmed `frontend/.env.production` points to `https://app-outdoors-api-dev.azurewebsites.net` for production builds.
