# Live API read-only probe

## When to use
- You need to validate a deployed API without mutating data.
- You want a quick tester view of which routes are public, authenticated, or role-gated.
- You need to compare live behavior with Swagger/OpenAPI exposure.

## Pattern
1. Fetch `/swagger/v1/swagger.json` and list the public/auth/product routes you expect to exist.
2. Probe only read-safe requests:
   - public `GET` routes such as `/api/health`, `/swagger/index.html`, `/api/v1/products`
   - anonymous calls to authenticated routes such as `/api/v1/auth/me` or admin mutations to confirm `401`
   - anonymous calls to role-filtered reads such as `?includeInactive=true` to confirm `403`
3. Record status codes plus short body previews, not full payload dumps.
4. Flag any mismatch where live auth enforcement exists but Swagger omits security metadata.

## OutdoorsShop notes
- Current dev API base URL is `https://app-outdoors-api-dev.azurewebsites.net`.
- Useful anonymous probes: `/api/health`, `/swagger/index.html`, `/swagger/v1/swagger.json`, `/api/v1/products`, `/api/v1/products/1`, `/api/v1/auth/me`, `/api/v1/auth/logout`, `/api/v1/products?includeInactive=true`, `/api/v1/users/change-password`.
