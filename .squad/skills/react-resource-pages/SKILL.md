# React resource pages

## Captured
- **Date:** 2026-05-23T19:06:28.812-03:00
- **Source:** Malta frontend implementation for OutdoorsShop

## Pattern
Use this pattern when a React + TypeScript frontend needs full CRUD or dashboard-style pages quickly:

1. Put typed DTO mapping in `src/api/*.api.ts` and keep auth retry logic centralized in `src/api/client.ts`.
2. Keep route pages lean by composing `useAsyncData`, shared UI shells (`Card`, `Alert`, `Button`, `Modal`), and domain components (`ProductCard`, `CartItemRow`, `CategoryBadge`).
3. Use one page component per route, then split admin/customer areas with `React.lazy` in `App.tsx` and role-aware wrappers (`ProtectedRoute`, `AdminRoute`).
4. For editable admin tables, keep local draft state keyed by entity id, submit through typed API wrappers, and refresh the page dataset after each mutation.
5. For storefront consistency, define global theme tokens and reusable surface classes once in Tailwind config + `index.css`, then apply them everywhere instead of page-specific one-off styling.

## OutdoorsShop references
- `frontend/src/api/client.ts`
- `frontend/src/hooks/useAsyncData.ts`
- `frontend/src/components/ui/`
- `frontend/src/pages/admin/`
