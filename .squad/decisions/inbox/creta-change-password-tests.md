# Creta Finding — Change Password Tests (2026-05-25T19:08:32.516-03:00)

## What I added
- 6 backend integration contract tests for `PUT /api/v1/users/change-password`
- 4 `AuthController.ChangePassword()` unit tests following the existing xUnit + Moq controller pattern
- A bootstrap fix in `TestWebAppFactory` so SQLite creates Identity tables before startup seeding

## Result
- `dotnet test .\tests\OutdoorsShop.Api.Tests\OutdoorsShop.Api.Tests.csproj --no-restore`
- 62 tests passed
- 6 tests failed

## Notable finding
The current backend implementation lives at `PUT /api/v1/auth/change-password`, not the requested `PUT /api/v1/users/change-password` contract. The new integration tests intentionally target the requested route, and they currently fail with `404 Not Found` for that reason.

## Edge cases covered
- authenticated happy path
- wrong current password
- unauthenticated request
- confirm password mismatch
- new password shorter than 8 characters
- old password rejection after successful change
- second user unaffected by first user password change

## Recommendation
Have Cinnamon align the route to `PUT /api/v1/users/change-password` or explicitly update the team contract if `auth/change-password` is the intended public endpoint.
