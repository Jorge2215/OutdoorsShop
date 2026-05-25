# Cinnamon inbox — change password endpoint

- Date: 2026-05-25T19:08:32.516-03:00
- Owner: Cinnamon
- Area: backend auth API

## Decision

Add `PUT /api/v1/users/change-password` for authenticated users and handle the operation through `ICustomerService` / `CustomerService` while relying on ASP.NET Core Identity password APIs.

## Rationale

- The profile flow already lets authenticated users update personal data, but it lacked a secure self-service password change endpoint.
- The requested contract is `/api/v1/users/change-password`, so the backend now exposes that route directly.
- Using `CheckPasswordAsync` plus `ChangePasswordAsync` keeps password validation, hashing, and persistence inside ASP.NET Core Identity instead of custom code.

## Implementation notes

- Request body: `currentPassword`, `newPassword`, `confirmNewPassword`.
- `CurrentPassword` mismatch returns `400 Bad Request` with a clear message.
- Successful changes return `200 OK` with a success message.
- Swagger XML comments are enabled in `OutdoorsShop.Api` so the endpoint description appears in generated docs.
- Validation and regression were verified with `dotnet build .\\src\\OutdoorsShop.Api\\OutdoorsShop.Api.csproj` and `dotnet test .\\OutdoorsShop.slnx`.
