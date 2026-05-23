# Malta frontend theme decisions

- **Date:** 2026-05-23T19:06:28.812-03:00
- **Author:** Malta

## Decisions

1. The storefront uses Tailwind CSS with a shared oriental palette (`crimson`, `gold`, `jade`, `ink`, `parchment`, `copper`, `mist`) defined in `frontend/tailwind.config.js` so every page can reuse the same visual tokens.
2. Layout and surface styling live in `frontend/src/index.css` through reusable shells (`container-shell`, `ornate-card`, `panel-shell`, `field-input`) instead of ad-hoc page styling, keeping the magical bazaar tone consistent across catalog, checkout, and admin views.
3. Route pages rely on reusable UI building blocks in `frontend/src/components/ui/` and domain components in `frontend/src/components/products/` so customer and admin screens share the same visual language while staying responsive and accessible.
4. Auth and cart flows follow team security decisions: access token remains in memory via `frontend/src/store/authStore.ts`, refresh is cookie-based through `frontend/src/api/client.ts`, and the cart persists only in localStorage via `frontend/src/store/cartStore.ts`.
