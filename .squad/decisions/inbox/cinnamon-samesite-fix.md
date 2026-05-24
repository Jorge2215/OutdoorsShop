# Cinnamon Decision — 2026-05-24T15:11:02.555-03:00 — Fix auth refresh cookie + JWT display name

**By:** Cinnamon  
**Requested by:** Jorgito (via Creta's diagnosis)  
**Status:** Applied

## What changed

- Updated `src/OutdoorsShop.Api/Controllers/AuthController.cs` so refresh-token cookies are emitted with `SameSite=None` and `Secure=true`.
- Matched the logout cookie-clearing path to the same cross-site cookie policy so browsers overwrite the existing refresh cookie correctly.
- Updated `GenerateTokenAsync` to source the JWT `given_name` claim from `customer.Name` instead of `user.UserName`.

## Why

- The frontend runs on a different origin than `app-outdoors-api-dev`, so `SameSite=Strict` prevented the browser from sending the refresh cookie on `POST /api/v1/auth/refresh`, causing 401s about 15 minutes after login.
- `ApplicationUser.UserName` is the email address for registered customers, so the JWT was exposing the email in `given_name` instead of the display name stored on the customer record.

## Verification

- `dotnet build .\OutdoorsShop.slnx` succeeded after the change.
- Published the API for Linux, zipped it with `ZipFile.CreateFromDirectory`, uploaded `publish/api-dev.zip` to `stoutdoorsdev/webapp-releases/api-dev.zip`, and restarted `app-outdoors-api-dev`.
- Live smoke test against `https://app-outdoors-api-dev.azurewebsites.net`:
  - `POST /api/v1/auth/register` → `200`
  - `Set-Cookie: refreshToken=...; secure; samesite=none; httponly`
  - `POST /api/v1/auth/refresh` with the stored cookie → `200`
  - Decoded JWT `given_name` claim = `Cinnamon Display`

## Consequences

- Cross-origin refresh now works for the deployed frontend/API split.
- New access tokens expose the customer display name in `given_name`, which is the value the UI expects for greeting/profile usage.
