---
name: "live-api-route-verification"
description: "Verify whether a deployed API really matches local route mapping before blaming deploy drift."
domain: "api-operations"
confidence: "high"
source: "manual"
tools:
  - name: "powershell"
    description: "Probe live endpoints and run a local build"
    when: "When you need real HTTP status evidence and source-state confirmation"
  - name: "view"
    description: "Read Program.cs and controller route attributes"
    when: "When comparing live routes to local route mapping"
---

## Context
Use this when a deployed API might be stale, misrouted, or partially deployed. It is especially useful after App Service deploys, when Swagger is available, and when local source may also be in a broken intermediate state.

## Patterns
- Start with the live OpenAPI document if available; it gives the fastest authoritative list of deployed routes.
- Probe a small set of representative endpoints with the correct HTTP methods so you can distinguish `200`, `400`, `401`, `403`, `404`, and `405` instead of relying on guesses.
- Compare those live paths to `Program.cs` plus the relevant controller `[Route]` and `[Http*]` attributes.
- Run a local build after the route check; if live routes look right but the branch does not compile, the next action is source repair, not redeploy.

## Examples
- `GET /api/health` returning `200` confirms the app is up and the explicit health mapping is live.
- `POST /api/v1/auth/login` returning `400` for `{}` is evidence that the route exists and model validation is active.
- `GET /api/v1/auth/me` returning `401` without a token shows the route is present and authorization is enforcing access.
- `GET /api/v1/products` returning `200` with catalog JSON confirms the anonymous catalog surface is deployed.

## Anti-Patterns
- Do not treat a `401` or `400` as a missing route; those often prove the route exists.
- Do not assume stale deploy solely from one failing probe without checking Swagger and the expected HTTP verb.
- Do not redeploy immediately if the local branch has merge markers or build failures; fix source integrity first.
