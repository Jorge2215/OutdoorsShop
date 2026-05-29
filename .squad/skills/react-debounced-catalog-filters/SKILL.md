---
name: "react-debounced-catalog-filters"
description: "Extending filterable React catalog pages without breaking URL state or existing query flow"
domain: "frontend-catalog"
confidence: "high"
source: "earned"
---

## Context
Use this when a React + TypeScript catalog page already has URL-backed filters and API query helpers, and the goal is to add new shopper controls without rebuilding the page state model.

## Patterns
- Keep the page's existing `useSearchParams` pipeline and extend it with new query keys instead of introducing a parallel filter store.
- Split price-style filters into two layers: raw input text for the form control and debounced parsed values for API requests + URL sync.
- Reset client-side pagination when any filter or sort control changes so the shopper never lands on an empty later page after narrowing results.
- Keep default sort behavior implicit in the URL when possible, but always pass the active sort to the API client so the frontend behavior stays explicit.
- Centralize query serialization in the API client (`buildQuery`) and export shared param/sort types from that file so pages and other consumers stay aligned.

## Examples
- `frontend/src/pages/ProductsPage.tsx`
- `frontend/src/api/products.api.ts`
- URL example: `?category=1&search=tent&minPrice=20&maxPrice=150&sort=price_asc`

## Anti-Patterns
- Replacing an existing filter page with a new state model just to add one or two inputs.
- Sending partial numeric strings (`-`, `.`, empty) into the URL or API.
- Duplicating sort/query param literals across multiple components instead of sharing types/constants.
