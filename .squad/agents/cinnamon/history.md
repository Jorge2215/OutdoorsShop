# history — History (summary)

- Full history archived to history-archive-20260524T145749Z.md

- Recent highlights:
- # Cinnamon â€” History
- 
- ## Core Context
- 
- - **Project:** Outdoors Shop
- - **Owner:** Jorgito
- - **Role:** Backend Developer
- - **Joined:** 2026-05-23
- - **Repo:** https://github.com/Jorge2215/OutdoorsShop.git (dev = development, main = production)
- - **Stack:** .NET 10 Web API (C#) | ASP.NET Core | EF Core | Azure SQL Database | Azure Functions (.NET isolated) | Azure Blob Storage | JWT auth
- - **Domain entities:** Products, Categories (Camping/Trekking/Cycling/Climbing), Customers, Orders, OrderItems, Inventory
- - **My scope:** .NET 10 Web API, EF Core + Azure SQL, Azure Functions (seasonal discounts/payment confirmation/stock updates), Azure Blob Storage (product images/receipts/exports), JWT auth backend, CSV/Excel report generation
- - **Team:** Toru (Architect), Malta (Frontend), Creta (Tester), Scribe (Docs), Ralph (Monitor)
- - **Purpose:** Proof of concept comparing GitHub Copilot + Squad vs traditional development
- 
- ## Learnings
- 
- ### 2026-05-24T15:11:02.555-03:00 â€” Auth refresh cookie cross-origin fix
- 
- - **Refresh cookie policy:** `src/OutdoorsShop.Api/Controllers/AuthController.cs` must use `SameSite=None` with `Secure=true` for both the refresh-token set cookie and the logout clear-cookie path; `SameSite=Strict` breaks cross-origin refresh when the frontend origin differs from the API origin.
- - **JWT display name claim:** `GenerateTokenAsync` should populate `given_name` from `customer.Name` instead of `user.UserName`, because registration stores the email in `UserName`.
- - **Deployment/verification:** Published `src/OutdoorsShop.Api/OutdoorsShop.Api.csproj` for Linux, zipped via `ZipFile.CreateFromDirectory`, uploaded to `stoutdoorsdev/webapp-releases/api-dev.zip`, restarted `app-outdoors-api-dev`, then verified `POST /api/v1/auth/register` and `POST /api/v1/auth/refresh` both returned `200` and the live `Set-Cookie` header now shows `samesite=none`.
- 
- ### 2026-05-23 â€” EF Core migration + entity key convention fix
- 
- - **Migration file location:** `src/OutdoorsShop.Infrastructure/Data/Migrations/20260523162304_InitialCreate.cs`

